using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// What the last Digio webhooks looked like, per company, for the Digio keys page. In memory: it shows "since this server
/// started" (Phase 1's inbox makes it durable).
/// </summary>
public sealed class DigioWebhookHealth
{
    private readonly ConcurrentDictionary<int, (DateTime? Received, DateTime? ProblemAt, string? Problem)> _state = new();

    public void Received(int companyId, DateTime? nowUtc = null) =>
        _state.AddOrUpdate(companyId, (nowUtc ?? DateTime.UtcNow, null, null), (_, s) => (nowUtc ?? DateTime.UtcNow, s.ProblemAt, s.Problem));

    public void Problem(int companyId, string what, DateTime? nowUtc = null) =>
        _state.AddOrUpdate(companyId, (null, nowUtc ?? DateTime.UtcNow, what), (_, s) => (s.Received, nowUtc ?? DateTime.UtcNow, what));

    public (DateTime? Received, DateTime? ProblemAt, string? Problem) For(int companyId) =>
        _state.TryGetValue(companyId, out var s) ? s : default;
}

/// <summary>Background confirmations in flight, so shutdown and tests can wait for them instead of guessing.</summary>
public sealed class DigioBackgroundTasks
{
    private readonly ConcurrentDictionary<Task, byte> _running = new();

    public void Track(Func<Task> work)
    {
        var task = Task.Run(work);
        _running[task] = 0;
        _ = task.ContinueWith(t => _running.TryRemove(t, out _), TaskScheduler.Default);
    }

    public Task WhenIdleAsync() => Task.WhenAll(_running.Keys);
}

/// <summary>
/// Acts on a VERIFIED Digio webhook. Only e-Sign events today. The only place an agreement becomes 'Signed' (all parties,
/// <c>agreement_status == "completed"</c>); the transition is one atomic statement so a redelivery or a concurrent delivery
/// changes nothing and meters the e-Sign credit exactly once.
/// </summary>
public sealed class DigioWebhookService(
    IDbContextFactory<LosDbContext> dbFactory, DigioClient digio, ApiBillingService billing, IFileStore files,
    IServiceScopeFactory scopes, DigioBackgroundTasks background, DigioStatusNotifier notifier, DigioEsignService esign,
    DigioNachService nach, DigioCollect collect, ILogger<DigioWebhookService> log)
{
    private const string Signed = "Signed";

    /// <summary>Throws only for a malformed body. True when the event is one we act on, false when it is ignored.</summary>
    public async Task<bool> HandleAsync(int companyId, string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var evt = Str(root, "event")?.ToLowerInvariant();
        if (DigioNachService.IsMandateEvent(evt))
        {
            // Never trust the webhook's own word for a mandate state: the confirming GET decides. A failed GET throws, so the inbox retries it.
            if (DigioNachService.MandateIdIn(root) is not { } mandateId)
            {
                return false;
            }

            return await nach.ConfirmAsync(companyId, mandateId) ? true : throw new InvalidOperationException("Digio mandate lookup failed");
        }

        if (DigioCollect.IsDebitEvent(evt))
        {
            return await collect.HandleEventAsync(companyId, root); // confirming GET decides; throws when Digio does not answer
        }

        if (!root.TryGetProperty("payload", out var payload) || !payload.TryGetProperty("document", out var document)
            || Str(document, "id") is not { Length: > 0 } docId)
        {
            return false;
        }

        switch (evt)
        {
            case "doc.signed":
                switch (Str(document, "agreement_status"))
                {
                    case "completed":
                        await CompleteAsync(companyId, docId);
                        break;
                    default:
                        // Not stated, or another signer is still to sign: ask Digio (not inside the webhook, it must answer fast) so the
                        // per-signer rows stay current; the document stays 'Sent' until every party has signed.
                        ConfirmInBackground(companyId, docId);
                        break;
                }

                break;
            case "doc.sign.failed":
                await FailAsync(companyId, docId, "Failed", Reason(document));
                ConfirmInBackground(companyId, docId);
                break;
            case "doc.sign.rejected":
                await FailAsync(companyId, docId, "Rejected", Reason(document));
                ConfirmInBackground(companyId, docId);
                break;
            default:
                return false;
        }

        return true;
    }

    /// <summary>Asks Digio for the document's real status and completes it if every party has signed.</summary>
    public async Task ConfirmAsync(int companyId, string docId)
    {
        var result = await digio.GetAsync(companyId, null, $"/v2/client/document/{Uri.EscapeDataString(docId)}", null, "reading a document's signing status (UNSURE: no price in the quote)");
        if (result.Status != DigioStatus.Ok)
        {
            return;
        }

        await esign.RefreshSignersAsync(companyId, docId, result.Data);
        if (result.Str("agreement_status") == "completed")
        {
            await CompleteAsync(companyId, docId);
        }
        else if (await ApplicationOfAsync(companyId, docId) is { } app)
        {
            await notifier.NotifyAsync(app);
        }
    }

    private void ConfirmInBackground(int companyId, string docId) => background.Track(async () =>
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<DigioWebhookService>().ConfirmAsync(companyId, docId);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Confirming Digio document {DocId} for company {CompanyId} failed", docId, companyId);
        }
    });

    private async Task CompleteAsync(int companyId, string docId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        // No signed-in user on a webhook: bypass the tenant filter and scope by the URL's company.
        var row = await db.Disbursements.IgnoreQueryFilters()
            .Where(d => d.EsignDocumentId == docId && d.Application!.CompanyId == companyId)
            .Select(d => new { d.Id, d.ApplicationId }).FirstOrDefaultAsync();
        if (row is null)
        {
            return;
        }

        // One statement decides who completes it. Redelivery or a parallel delivery gets 0 rows: no second credit.
        var won = await db.Disbursements.IgnoreQueryFilters()
            .Where(d => d.Id == row.Id && d.AgreementEsignStatus != Signed)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.AgreementEsignStatus, Signed).SetProperty(d => d.EsignFailureReason, (string?)null));
        if (won != 1)
        {
            return;
        }

        // A completed agreement means every signer has signed: the rows say so (and when), whatever order the events arrived in.
        var now = DateTime.UtcNow;
        await db.EsignSigners.IgnoreQueryFilters().Where(x => x.CompanyId == companyId && x.DocumentId == docId && x.Status != "signed")
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.Status, "signed").SetProperty(x => x.FailureReason, (string?)null).SetProperty(x => x.UpdatedAt, now).SetProperty(x => x.SignedAt, now));

        // A credit is consumed per Aadhaar signer (customer, co-applicant, guarantor; our lender stamp is not a Digio signer), when they sign.
        // Documents sent before signers were recorded have no rows: those still cost one.
        var signerIds = await db.EsignSigners.IgnoreQueryFilters().Where(x => x.CompanyId == companyId && x.DocumentId == docId).Select(x => x.Id.ToString()).ToListAsync();
        foreach (var signer in signerIds.Count == 0 ? [""] : signerIds)
        {
            await billing.RecordOnceAsync(companyId, DigioRates.ESignAadhaar, $"{docId}:{signer}", row.ApplicationId);
        }

        await RecordEvidenceAsync(db, companyId, row.ApplicationId, docId);
        await SaveSignedCopyAsync(companyId, row.Id, row.ApplicationId, docId);
        await notifier.NotifyAsync(row.ApplicationId);
    }

    private async Task<string?> ApplicationOfAsync(int companyId, string docId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Disbursements.IgnoreQueryFilters()
            .Where(d => d.EsignDocumentId == docId && d.Application!.CompanyId == companyId).Select(d => d.ApplicationId).FirstOrDefaultAsync();
    }

    // AGREEMENT_SIGNED evidence (02 section 8.1): sandbox evidence is tagged so a production company's gate ignores it.
    private static async Task RecordEvidenceAsync(LosDbContext db, int companyId, string applicationId, string docId)
    {
        var request = await db.DigioRequests.IgnoreQueryFilters().Where(r => r.CompanyId == companyId && r.DigioId == docId)
            .Select(r => new { r.Id, r.Environment, r.InputFingerprint }).FirstOrDefaultAsync();
        db.VerificationEvidence.Add(new VerificationEvidence
        {
            CompanyId = companyId, ApplicationId = applicationId, Subject = "AGREEMENT_SIGNED", Verdict = "Verified",
            Summary = "All parties signed the loan agreement.", DigioRequestId = request?.Id, Environment = request?.Environment ?? "Sandbox",
            InputFingerprint = request?.InputFingerprint, CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private async Task FailAsync(int companyId, string docId, string status, string? reason)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = await db.Disbursements.IgnoreQueryFilters()
            .Where(d => d.EsignDocumentId == docId && d.Application!.CompanyId == companyId)
            .Select(d => new { d.Id, d.ApplicationId }).FirstOrDefaultAsync();
        if (id is null)
        {
            return;
        }

        // Never un-signs; a repeat of the same outcome changes nothing.
        var changed = await db.Disbursements.IgnoreQueryFilters()
            .Where(d => d.Id == id.Id && d.AgreementEsignStatus != Signed && d.AgreementEsignStatus != status)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.AgreementEsignStatus, status).SetProperty(d => d.EsignFailureReason, reason));
        if (changed == 1)
        {
            await notifier.NotifyAsync(id.ApplicationId);
        }
    }

    /// <summary>
    /// Downloads the signed PDF and keeps it with the file. A failure is logged and leaves the path empty (the Post Sanction
    /// page then offers "Fetch signed copy"); it never undoes the Signed status.
    /// </summary>
    public async Task<bool> FetchSignedCopyAsync(int companyId, string applicationId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.Disbursements.IgnoreQueryFilters()
            .Where(d => d.ApplicationId == applicationId && d.Application!.CompanyId == companyId && d.AgreementEsignStatus == Signed)
            .Select(d => new { d.Id, d.EsignDocumentId, d.SignedAgreementFilePath }).FirstOrDefaultAsync();
        if (row?.EsignDocumentId is not { } docId)
        {
            return false;
        }

        return row.SignedAgreementFilePath is not null || await SaveSignedCopyAsync(companyId, row.Id, applicationId, docId);
    }

    private async Task<bool> SaveSignedCopyAsync(int companyId, int disbursementId, string applicationId, string docId)
    {
        try
        {
            var download = await digio.DownloadAsync(
                companyId, DigioRates.EsignDownload, $"/v2/client/document/download?document_id={Uri.EscapeDataString(docId)}", applicationId);
            if (download.Bytes is not { } bytes || !FileSignature.Matches(".pdf", bytes))
            {
                log.LogWarning("No signed PDF for Digio document {DocId} (company {CompanyId}): {Status}", docId, companyId, download.Status);
                return false;
            }

            var key = FileKey.Normalize(await files.SaveUploadAsync(applicationId, "postsanction", $"signed-{docId}.pdf", bytes));
            await using var db = await dbFactory.CreateDbContextAsync();
            await db.Disbursements.IgnoreQueryFilters().Where(d => d.Id == disbursementId)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.SignedAgreementFilePath, key));
            return true;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Saving the signed PDF for Digio document {DocId} (company {CompanyId}) failed", docId, companyId);
            return false;
        }
    }

    // "ESP-123: UIDAI Server Down", or for a rejection the signer checks that failed: "Checks failed: AADHAAR, YOB".
    private static string? Reason(JsonElement document)
    {
        var parts = new List<string>();
        if (Str(document, "error_code") is { } code) { parts.Add(code); }
        if (Str(document, "error_message") is { } message) { parts.Add(message); }
        var text = string.Join(": ", parts);

        if (document.TryGetProperty("validation_results", out var results) && results.ValueKind == JsonValueKind.Object
            && results.TryGetProperty("ruleResults", out var rules) && rules.ValueKind == JsonValueKind.Array)
        {
            var failed = rules.EnumerateArray()
                .SelectMany(r => r.TryGetProperty("conditions", out var c) && c.ValueKind == JsonValueKind.Array ? c.EnumerateArray() : [])
                .Where(c => Str(c, "result") == "FAIL").Select(c => Str(c, "field")).Where(f => f is not null).Distinct().ToList();
            if (failed.Count > 0)
            {
                text = (text.Length > 0 ? text + ". " : "") + "Signer checks failed: " + string.Join(", ", failed);
            }
        }

        return text.Length == 0 ? null : text.Length > 300 ? text[..300] : text;
    }

    private static string? Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}

/// <summary>
/// POST /webhooks/digio/{companyId}. Digio's delivery queue is head-of-line per account and webhooks are disabled after 24 h of
/// failures, so this ALWAYS answers 200: an unknown company, an oversize or unreadable body, a bad checksum and any processing
/// error are logged (ids only, never the body) and ignored. Only a body with a valid checksum is processed.
/// </summary>
public static class DigioWebhookEndpoint
{
    public const int MaxBodyBytes = 1_000_000;

    public static void UseDigioWebhook(this WebApplication app) => app.Use(async (context, next) =>
    {
        if (!HttpMethods.IsPost(context.Request.Method)
            || !context.Request.Path.StartsWithSegments("/webhooks/digio", out var rest))
        {
            await next();
            return;
        }

        await HandleAsync(context, rest.Value?.Trim('/'));
    });

    public static async Task HandleAsync(HttpContext context, string? companyIdText)
    {
        var log = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("DigioWebhook");
        var health = context.RequestServices.GetRequiredService<DigioWebhookHealth>();
        var companyId = 0;
        try
        {
            if (!int.TryParse(companyIdText, out companyId))
            {
                log.LogWarning("Digio webhook for an unreadable company id ignored (from {Ip})", ClientIp.Of(context));
            }
            else if (await ReadBoundedAsync(context.Request.Body) is not { } body)
            {
                log.LogWarning("Digio webhook for company {CompanyId} ignored: body over {Max} bytes", companyId, MaxBodyBytes);
                health.Problem(companyId, "A webhook body was too large and was ignored.");
            }
            else
            {
                var secret = context.RequestServices.GetRequiredService<DigioClient>().Credentials(companyId)?.WebhookSecret;
                if (string.IsNullOrEmpty(secret)
                    || !DigioClient.ChecksumMatches(secret, body, context.Request.Headers["X-Digio-Checksum"]))
                {
                    log.LogWarning("Digio webhook for company {CompanyId} ignored: signature missing or wrong (from {Ip})", companyId, ClientIp.Of(context));
                    health.Problem(companyId, "Signature mismatch: check the webhook secret on this page.");
                }
                else
                {
                    health.Received(companyId);
                    if (await context.RequestServices.GetRequiredService<DigioInbox>().StoreAsync(companyId, body, ClientIp.Of(context)) == DigioStored.Unreadable)
                    {
                        health.Problem(companyId, "A webhook could not be processed (unreadable body); see the Digio inbox.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Never a non-200 to Digio: record it and move on.
            log.LogError(ex, "Digio webhook for company {CompanyId} could not be processed", companyId);
            health.Problem(companyId, "A webhook could not be processed (" + ex.GetType().Name + "); see the server log.");
        }

        if (!context.Response.HasStarted)
        {
            // A body on purpose: an empty reply would be re-executed by the status-code pages as a POST to the error route.
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync("ok");
        }
    }

    /// <summary>The body as text, or null when it is bigger than the cap: enforced on the bytes read, so chunked requests can't skip it.</summary>
    internal static async Task<string?> ReadBoundedAsync(Stream stream)
    {
        var buffer = new byte[MaxBodyBytes + 1];
        var read = 0;
        int n;
        while (read < buffer.Length && (n = await stream.ReadAsync(buffer.AsMemory(read))) > 0)
        {
            read += n;
        }

        return read > MaxBodyBytes ? null : Encoding.UTF8.GetString(buffer, 0, read);
    }
}
