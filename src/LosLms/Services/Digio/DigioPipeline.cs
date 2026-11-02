using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// Tells every page looking at an application that a Digio status on it changed (AdminRequestNotifier's pattern: the page
/// re-reads from the database, nothing travels in the event). Single-server only, like AdminRequestNotifier; a page MUST
/// unsubscribe on dispose.
/// </summary>
public sealed class DigioStatusNotifier
{
    public event Func<string, Task>? Changed;

    public async Task NotifyAsync(string applicationId)
    {
        if (Changed is null)
        {
            return;
        }

        foreach (var handler in Changed.GetInvocationList().Cast<Func<string, Task>>())
        {
            try { await handler(applicationId); }
            catch (Exception) { /* a circuit that has gone away mid-notification is expected */ }
        }
    }
}

public enum DigioStored { Stored, Duplicate, Unreadable }

/// <summary>
/// The webhook's whole job: keep the verified body and answer. (CompanyId, DigioEventId) is unique, so Digio's at-least-once
/// redelivery is a no-op. Processing happens later in <see cref="DigioEventProcessor"/>.
/// </summary>
public sealed class DigioInbox(IDbContextFactory<LosDbContext> dbFactory, PasswordVault vault, DigioEventProcessor processor)
{
    public async Task<DigioStored> StoreAsync(int companyId, string body, string? sourceIp)
    {
        var row = new DigioWebhookInbox
        {
            CompanyId = companyId, ReceivedAt = DateTime.UtcNow, SourceIp = sourceIp is { Length: > 45 } ? sourceIp[..45] : sourceIp,
            PayloadJson = vault.Seal(body),
        };
        var readable = Fill(row, body);

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.DigioWebhookInbox.IgnoreQueryFilters().AnyAsync(i => i.CompanyId == companyId && i.DigioEventId == row.DigioEventId))
        {
            return DigioStored.Duplicate;
        }

        db.DigioWebhookInbox.Add(row);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // A parallel delivery of the same event won the unique index.
            return DigioStored.Duplicate;
        }

        processor.Wake();
        return readable ? DigioStored.Stored : DigioStored.Unreadable;
    }

    // Reads the envelope. Unparseable → kept as Failed with no retries (a retry can't make it parseable).
    private static bool Fill(DigioWebhookInbox row, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            string? Str(JsonElement e, string n) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

            row.Event = Cut(Str(root, "event") ?? "unknown", 60);
            row.DigioEventId = Cut(Str(root, "id") ?? Hash(body), 80);
            if (root.TryGetProperty("entities", out var entities) && entities.ValueKind == JsonValueKind.Array && entities.GetArrayLength() > 0
                && entities[0].GetString() is { } type)
            {
                row.EntityType = Cut(type, 30);
                if (root.TryGetProperty("payload", out var payload) && payload.TryGetProperty(type, out var entity) && Str(entity, "id") is { } id)
                {
                    row.EntityId = Cut(id, 80);
                }
            }

            return true;
        }
        catch (JsonException)
        {
            row.Event = "unreadable";
            row.DigioEventId = Hash(body);
            row.Status = "Failed";
            row.Attempts = DigioEventProcessor.MaxAttempts;
            row.LastError = "The body is not valid JSON.";
            return false;
        }
    }

    private static string Hash(string body) => "x" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)))[..32].ToLowerInvariant();

    private static string Cut(string s, int max) => s.Length > max ? s[..max] : s;
}

/// <summary>
/// Works the inbox: claims due rows, hands the e-Sign events to <see cref="DigioWebhookService"/>, and records the outcome.
/// Never throws back at a request. A failure backs off 1, 5, 15, 60 minutes (then hourly) up to <see cref="MaxAttempts"/>.
/// Runs at boot too, so anything received while the app was down is picked up (startup catch-up).
/// </summary>
public sealed class DigioEventProcessor(IServiceScopeFactory scopes, DigioWebhookHealth health, ILogger<DigioEventProcessor> log) : BackgroundService
{
    public const int MaxAttempts = 8;
    private static readonly TimeSpan[] Backoff = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(60)];
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan Sweep = TimeSpan.FromMinutes(1);

    private readonly Channel<bool> _wake = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public void Wake() => _wake.Writer.TryWrite(true);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync();
                using var wait = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                wait.CancelAfter(Sweep);
                await _wake.Reader.ReadAsync(wait.Token);
            }
            catch (OperationCanceledException) { /* sweep timer or shutdown */ }
            catch (Exception ex)
            {
                log.LogError(ex, "Digio event processor loop failed; continuing");
                await Task.Delay(Sweep, stoppingToken);
            }
        }
    }

    /// <summary>Processes every due row once; returns how many it finished with. Public so tests (and a manual retry) can drive it.</summary>
    public async Task<int> ProcessPendingAsync(DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        await using var scope = scopes.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LosDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var due = await db.DigioWebhookInbox.IgnoreQueryFilters().AsNoTracking()
            .Where(i => (i.Status == "Pending" || i.Status == "Failed") && i.Attempts < MaxAttempts && (i.NextAttemptAt == null || i.NextAttemptAt <= now))
            .OrderBy(i => i.Id).Take(100).Select(i => new { i.Id, i.CompanyId, i.Attempts }).ToListAsync();

        var done = 0;
        foreach (var item in due)
        {
            // Claim: only one worker bumps Attempts from the value it saw; the lease keeps a crashed claim from sticking.
            var claimed = await db.DigioWebhookInbox.IgnoreQueryFilters()
                .Where(i => i.Id == item.Id && i.Attempts == item.Attempts)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Attempts, item.Attempts + 1).SetProperty(i => i.NextAttemptAt, now + Lease));
            if (claimed == 1 && await ProcessOneAsync(scope.ServiceProvider, item.Id, item.CompanyId, item.Attempts + 1, now))
            {
                done++;
            }
        }

        return done;
    }

    private async Task<bool> ProcessOneAsync(IServiceProvider sp, long id, int companyId, int attempt, DateTime now)
    {
        var db = await sp.GetRequiredService<IDbContextFactory<LosDbContext>>().CreateDbContextAsync();
        await using var _ = db;
        try
        {
            var sealedBody = await db.DigioWebhookInbox.IgnoreQueryFilters().Where(i => i.Id == id).Select(i => i.PayloadJson).FirstAsync();
            var body = sp.GetRequiredService<PasswordVault>().Unseal(sealedBody);
            var handled = await sp.GetRequiredService<DigioWebhookService>().HandleAsync(companyId, body);
            await db.DigioWebhookInbox.IgnoreQueryFilters().Where(i => i.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, handled ? "Processed" : "Ignored")
                    .SetProperty(i => i.ProcessedAt, DateTime.UtcNow).SetProperty(i => i.NextAttemptAt, (DateTime?)null).SetProperty(i => i.LastError, (string?)null));
            return true;
        }
        catch (Exception ex)
        {
            // Type only: an exception message could carry request content.
            var error = ex.GetType().Name;
            log.LogWarning("Digio inbox row {InboxId} (company {CompanyId}) failed on attempt {Attempt}: {Error}", id, companyId, attempt, error);
            health.Problem(companyId, "A webhook could not be processed (" + error + "); it will be retried.");
            var next = now + Backoff[Math.Min(attempt - 1, Backoff.Length - 1)];
            await db.DigioWebhookInbox.IgnoreQueryFilters().Where(i => i.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, "Failed").SetProperty(i => i.NextAttemptAt, next).SetProperty(i => i.LastError, error));
            return false;
        }
    }
}

/// <summary>
/// Catches what a webhook never delivered (desktop without a tunnel, a sleeping Render instance, a thin sandbox payload): every
/// 5 minutes it asks Digio about each agreement still 'Sent' and applies the answer through the same path as a webhook. Backs off
/// per document 5 min → 15 → 60 → 4 h (in memory: a restart is itself a catch-up). Other products plug in as their phases land.
/// </summary>
public sealed class DigioReconciler(IServiceScopeFactory scopes, ILogger<DigioReconciler> log) : BackgroundService
{
    private static readonly TimeSpan[] Backoff = [TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(60), TimeSpan.FromHours(4)];
    private const int PerRun = 50;

    private readonly ConcurrentDictionary<string, (int Polls, DateTime Next)> _state = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
            do
            {
                try { await ReconcileAsync(); }
                catch (Exception ex) when (ex is not OperationCanceledException) { log.LogError(ex, "Digio reconcile failed; will retry"); }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) { /* shutdown */ }
    }

    /// <summary>Confirms each 'Sent' agreement that is due; returns how many it asked Digio about.</summary>
    public async Task<int> ReconcileAsync(DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        await using var scope = scopes.CreateAsyncScope();
        await using var db = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<LosDbContext>>().CreateDbContextAsync();
        var sent = await db.Disbursements.IgnoreQueryFilters().AsNoTracking()
            .Where(d => d.AgreementEsignStatus == "Sent" && d.EsignDocumentId != null)
            .Select(d => new { d.Application!.CompanyId, DocId = d.EsignDocumentId! }).ToListAsync();

        var webhook = scope.ServiceProvider.GetRequiredService<DigioWebhookService>();
        var asked = 0;
        foreach (var d in sent.Where(d => !_state.TryGetValue(d.DocId, out var s) || s.Next <= now).Take(PerRun))
        {
            var polls = _state.TryGetValue(d.DocId, out var prev) ? prev.Polls : 0;
            _state[d.DocId] = (polls + 1, now + Backoff[Math.Min(polls, Backoff.Length - 1)]);
            await webhook.ConfirmAsync(d.CompanyId, d.DocId);
            asked++;
        }

        // DigiLocker requests the customer has not finished: same backoff, keyed by our request id. (The KYC webhook payload shape was not captured, so polling is the path.)
        var waiting = await db.DigioRequests.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.Operation == "DIGILOCKER_AADHAAR_PAN" && r.Status == "WaitingForCustomer")
            .Select(r => new { r.CompanyId, r.Id }).ToListAsync();
        foreach (var r in waiting.Where(r => !_state.TryGetValue(r.Id, out var s) || s.Next <= now).Take(Math.Max(0, PerRun - asked)))
        {
            var polls = _state.TryGetValue(r.Id, out var prev) ? prev.Polls : 0;
            _state[r.Id] = (polls + 1, now + Backoff[Math.Min(polls, Backoff.Length - 1)]);
            await scope.ServiceProvider.GetRequiredService<DigioKyc>().CompleteDigiLockerAsync(r.CompanyId, r.Id);
            asked++;
        }


        // Mandates not yet final (physical / eSign ones take days): same backoff, the confirming GET moves them forward.
        var nach = scope.ServiceProvider.GetRequiredService<DigioNachService>();
        var open = await nach.OpenMandatesAsync();
        foreach (var m in open.Where(m => !_state.TryGetValue(m.MandateId, out var s) || s.Next <= now).Take(Math.Max(0, PerRun - asked)))
        {
            var polls = _state.TryGetValue(m.MandateId, out var prev) ? prev.Polls : 0;
            _state[m.MandateId] = (polls + 1, now + Backoff[Math.Min(polls, Backoff.Length - 1)]);
            await nach.ConfirmAsync(m.CompanyId, m.MandateId);
            asked++;
        }

        // Debits Digio has not finished with: the same backoff; the confirming GET records the result and the receipt.
        var collect = scope.ServiceProvider.GetRequiredService<DigioCollect>();
        var debits = await collect.OpenAsync();
        foreach (var d in debits.Where(d => !_state.TryGetValue(d.PresentationId, out var s) || s.Next <= now).Take(PerRun))
        {
            var polls = _state.TryGetValue(d.PresentationId, out var prev) ? prev.Polls : 0;
            _state[d.PresentationId] = (polls + 1, now + Backoff[Math.Min(polls, Backoff.Length - 1)]);
            await collect.ConfirmAsync(d.CompanyId, d.PresentationId);
            asked++;
        }

        // Forget documents, DigiLocker requests, mandates and debits that are no longer open.
        var live = sent.Select(d => d.DocId).Concat(waiting.Select(r => r.Id)).Concat(open.Select(m => m.MandateId)).Concat(debits.Select(d => d.PresentationId)).ToHashSet();
        foreach (var key in _state.Keys.Where(k => !live.Contains(k)))
        {
            _state.TryRemove(key, out _);
        }

        return asked;
    }
}

/// <summary>The /digio/return page's hint: confirm that agreement (or DigiLocker request) with Digio now instead of at the next poll.</summary>
public sealed class DigioReconcileTrigger(IServiceScopeFactory scopes) : IDigioReconcileTrigger
{
    public async Task RequestReconcileAsync(string requestId)
    {
        await using var scope = scopes.CreateAsyncScope();
        await using var db = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<LosDbContext>>().CreateDbContextAsync();
        var req = await db.DigioRequests.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.Id == requestId && r.DigioId != null && (r.Product == "ESIGN" || r.Operation == "DIGILOCKER_AADHAAR_PAN"))
            .Select(r => new { r.CompanyId, r.DigioId, r.Operation }).FirstOrDefaultAsync();
        if (req?.Operation == "DIGILOCKER_AADHAAR_PAN")
        {
            await scope.ServiceProvider.GetRequiredService<DigioKyc>().CompleteDigiLockerAsync(req.CompanyId, requestId);
        }
        else if (req is not null)
        {
            await scope.ServiceProvider.GetRequiredService<DigioWebhookService>().ConfirmAsync(req.CompanyId, req.DigioId!);
        }
    }
}
