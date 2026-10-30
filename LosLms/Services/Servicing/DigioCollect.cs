using System.Text.Json;
using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// The debit presentation state machine (docs/digio/03 section 2). States only move forward: Created, Sent, InProgress, then one of
/// Succeeded / Failed / NotPresented / Cancelled, which are final. Digio's GET answer is the source of truth, so a replayed or
/// out-of-order webhook can never undo a result.
/// </summary>
public static class CollectLifecycle
{
    public const string Created = "Created", Sent = "Sent", InProgress = "InProgress", Succeeded = "Succeeded", Failed = "Failed",
        NotPresented = "NotPresented", Cancelled = "Cancelled";

    public static bool IsFinal(string? s) => s is Succeeded or Failed or NotPresented or Cancelled;

    private static int Rank(string? s) => s switch { Created => 0, Sent => 1, InProgress => 2, _ => 3 };

    /// <summary>Digio's transaction <c>details.status</c> to ours; null when the status says nothing new.</summary>
    public static string? Map(string? txnStatus) => txnStatus?.Trim().ToUpperInvariant() switch
    {
        "PAYMENT_SUCCESS" => Succeeded,
        "PAYMENT_FAILED" => Failed,
        "SKIPPED" or "REJECT_SPO_BANK" or "DIGIO_TECHNICAL_ERROR" => NotPresented,   // never reached the customer's bank: fix and present again
        "CANCELLED" => Cancelled,
        "NEW" or "READY_TO_SEND" or "SUBMITTED_TO_BANK" or "ACCEPTED_SPO_BANK" or "PAYMENT_DELAYED" or "PAYMENT_AUTHENTICATED" => InProgress,
        _ => null,
    };

    public static string Next(string current, string? incoming) =>
        incoming is null || IsFinal(current) ? current : IsFinal(incoming) || Rank(incoming) > Rank(current) ? incoming : current;
}

/// <summary>DigiCollect e-NACH debits: register one one-time presentation per instalment, confirm it, record the outcome.</summary>
public sealed class DigioCollect(
    IDbContextFactory<LosDbContext> dbFactory, DigioClient digio, DigioStatusNotifier notifier, IBounceHandler bounces) : IPresentationStarter
{
    private static DateOnly TodayIst(DateTime? nowUtc = null) => DateOnly.FromDateTime((nowUtc ?? DateTime.UtcNow).AddHours(5.5));

    /// <summary>
    /// Creates a presentation for every due instalment whose date falls in [today+1, today+lead] (IST), for each company with the Debits
    /// product on. Idempotent: an instalment that already has a presentation gets none (re-presentation after a failure is the bounce
    /// policy, slice 3). Presentations that Digio never accepted (still Created, no plan id) are sent again with the same unique_key.
    /// </summary>
    public async Task<int> PresentDueAsync(DateTime? nowUtc = null)
    {
        var today = TodayIst(nowUtc);
        await using var db = await dbFactory.CreateDbContextAsync();
        var companies = await db.DigioSettings.IgnoreQueryFilters().AsNoTracking().Where(s => s.Debits && s.Enach).ToListAsync();
        var created = 0;
        foreach (var s in companies)
        {
            if (digio.Credentials(s.CompanyId) is not { } creds)
            {
                continue;
            }

            var lead = Math.Max(1, s.DebitLeadDays);
            var from = today.AddDays(1);
            var to = today.AddDays(lead);
            var due = await db.Instalments.IgnoreQueryFilters().Include(i => i.LoanAccount)
                .Where(i => i.CompanyId == s.CompanyId && i.Status == "Due" && i.DueDate >= from && i.DueDate <= to
                    && i.LoanAccount!.Status == "Active" && i.LoanAccount.MandateId != null)
                .OrderBy(i => i.DueDate).ThenBy(i => i.No).ToListAsync();
            foreach (var i in due)
            {
                if (await db.Presentations.IgnoreQueryFilters().AnyAsync(p => p.InstalmentId == i.Id))
                {
                    continue;
                }

                var mandate = await db.EnachMandates.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(m => m.Id == i.LoanAccount!.MandateId);
                if (mandate is not { DigioState: NachLifecycle.Registered, Umrn: { Length: > 0 } umrn })
                {
                    continue; // debits only against a Registered UMRN
                }

                var p = new Presentation
                {
                    Id = Guid.NewGuid().ToString("N"), CompanyId = s.CompanyId, InstalmentId = i.Id, Attempt = 1, Umrn = umrn,
                    Amount = i.Emi - i.PaidAmount, SettlementDate = i.DueDate, Status = CollectLifecycle.Created,
                    Environment = DigioEndpoints.Environment(creds.Production), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
                };
                db.Presentations.Add(p);
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    db.Entry(p).State = EntityState.Detached; // a parallel run won the (instalment, attempt) index
                    continue;
                }

                created++;
                await RegisterAsync(s.CompanyId, p.Id);
            }
        }

        // Created but never accepted (Digio unreachable at the time): send again, same unique_key.
        var cutoff = (nowUtc ?? DateTime.UtcNow).AddMinutes(-30);
        var stale = await db.Presentations.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.Status == CollectLifecycle.Created && p.DigioPlanId == null && p.CreatedAt < cutoff)
            .Select(p => new { p.CompanyId, p.Id }).ToListAsync();
        foreach (var p in stale)
        {
            await RegisterAsync(p.CompanyId, p.Id);
        }

        return created;
    }

    /// <summary>Re-presentation (slice 3): the next attempt for a bounced instalment, due no earlier than tomorrow (the cut-off is T-1). True when it was created.</summary>
    public async Task<bool> StartAsync(int instalmentId, int attempt)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var i = await db.Instalments.IgnoreQueryFilters().Include(x => x.LoanAccount).FirstOrDefaultAsync(x => x.Id == instalmentId);
        if (i is not { Status: "Bounced", LoanAccount: { Status: "Active", MandateId: not null } account } || digio.Credentials(i.CompanyId) is not { } creds)
        {
            return false;
        }

        var mandate = await db.EnachMandates.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(m => m.Id == account.MandateId);
        if (mandate is not { DigioState: NachLifecycle.Registered, Umrn: { Length: > 0 } umrn })
        {
            return false;
        }

        var p = new Presentation
        {
            Id = Guid.NewGuid().ToString("N"), CompanyId = i.CompanyId, InstalmentId = i.Id, Attempt = attempt, Umrn = umrn, Amount = i.Emi - i.PaidAmount,
            SettlementDate = TodayIst().AddDays(1), Status = CollectLifecycle.Created, Environment = DigioEndpoints.Environment(creds.Production),
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        db.Presentations.Add(p);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return false; // a parallel run already made this attempt
        }

        await RegisterAsync(i.CompanyId, p.Id);
        return true;
    }

    /// <summary>
    /// POST /v3/client/nach_debit/scheduled/register with unique_key = our presentation id. Marks the instalment Presented on acceptance.
    /// </summary>
    public async Task<(bool Ok, string? Error)> RegisterAsync(int companyId, string presentationId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var p = await db.Presentations.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == presentationId && x.CompanyId == companyId);
        if (p is null || p.Status != CollectLifecycle.Created)
        {
            return (false, "Nothing to send.");
        }

        var settings = await db.DigioSettings.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(s => s.CompanyId == companyId);
        var creds = digio.Credentials(companyId);
        var account = creds is null || settings is null ? null : creds.Production ? settings.CorporateAccountProduction : settings.CorporateAccountSandbox;
        if (creds is null || settings is not { Debits: true } || string.IsNullOrWhiteSpace(account))
        {
            return (false, $"Debits are not set up for {DigioEndpoints.Environment(creds?.Production ?? false)} (switch, keys and collection account).");
        }

        var instalment = await db.Instalments.IgnoreQueryFilters().Include(i => i.LoanAccount).FirstAsync(i => i.Id == p.InstalmentId);
        var mandate = await db.EnachMandates.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(m => m.Id == instalment.LoanAccount!.MandateId);
        var customer = await db.Parties.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.ApplicationId == instalment.LoanAccount!.ApplicationId && x.PartyType == "Applicant").Select(x => x.FullName).FirstOrDefaultAsync();
        var configId = creds.Production ? settings.EnachConfigIdProduction : settings.EnachConfigIdSandbox;

        // ponytail: settlement_date format (yyyy-MM-dd), unique_key idempotency and destination_account_type (SAVINGS) are unconfirmed until
        // the Phase 0 sandbox debit sample exists (03 section 2).
        var body = new Dictionary<string, object?>
        {
            ["umrn"] = p.Umrn, ["amount_in_paise"] = (long)Math.Round(p.Amount * 100m), ["settlement_date"] = p.SettlementDate.ToString("yyyy-MM-dd"),
            ["frequency"] = "onetime", ["corporate_account_number"] = account.Trim(), ["corporate_config_id"] = string.IsNullOrWhiteSpace(configId) ? null : configId.Trim(),
            ["customer_name"] = customer, ["customer_account_number"] = mandate?.AccountNumber, ["destination_account_type"] = "savings",
            ["narration"] = $"EMI {instalment.No} {instalment.LoanAccount!.LoanAccountNo}", ["unique_key"] = p.Id,
        };
        var result = await digio.SendAsync(new DigioCall(companyId, HttpMethod.Post, "/v3/client/nach_debit/scheduled/register", body,
            DigioRates.MandateDebit, "DEBIT", "ENACH_DEBIT", instalment.LoanAccount.ApplicationId, null, p.Id, null, null, SuccessStatus: "Processing"));
        if (result.Status != DigioStatus.Ok || result.Str("id") is not { } planId)
        {
            p.FailureReason = Cut(result.Status == DigioStatus.NotConfigured ? "Digio is not configured." : result.Error ?? "Digio did not accept the debit.", 300);
            p.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return (false, p.FailureReason);
        }

        p.DigioPlanId = planId;
        p.Status = CollectLifecycle.Sent;
        p.FailureReason = null;
        p.UpdatedAt = DateTime.UtcNow;
        if (instalment.Status is "Due" or "Bounced")
        {
            instalment.Status = "Presented";
        }

        await db.SaveChangesAsync();
        await notifier.NotifyAsync(instalment.LoanAccount.ApplicationId);
        return (true, null);
    }

    /// <summary>
    /// GET /v3/client/nach_debit/scheduled/by_reference_id/{our id} (the plan, with its last transaction) is the source of truth.
    /// Applies it through the forward-only machine; the receipt is made here and only here for a success. True when Digio answered.
    /// </summary>
    public async Task<bool> ConfirmAsync(int companyId, string presentationId)
    {
        var r = await digio.GetAsync(companyId, null, $"/v3/client/nach_debit/scheduled/by_reference_id/{Uri.EscapeDataString(presentationId)}", null, "reading a debit's status; the debit itself is billed at presentation (MANDATE_DEBIT)");
        if (r.Status != DigioStatus.Ok || r.Data is not { } plan)
        {
            return false;
        }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var p = await db.Presentations.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == presentationId && x.CompanyId == companyId);
            if (p is null)
            {
                return true;
            }

            var application = await ApplyAsync(db, p, plan);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                continue; // a parallel confirm made the receipt first (unique PresentationId) or took the number: re-read and redo
            }

            if (application is not null)
            {
                await notifier.NotifyAsync(application);
                if (p.Status == CollectLifecycle.Failed)
                {
                    await bounces.OnFailedAsync(p.Id); // bounce charge, then re-present later or hand to an officer (slice 3)
                }
            }

            return true;
        }

        return true;
    }

    /// <summary>Applies a plan answer to the presentation and its instalment (no save). Returns the application id when something changed.</summary>
    public static async Task<string?> ApplyAsync(LosDbContext db, Presentation p, JsonElement plan, DateTime? nowUtc = null)
    {
        var txn = Obj(plan, "last_transaction");
        var details = Obj(txn, "details");
        var incoming = CollectLifecycle.Map(Str(details, "status"));
        var before = (p.Status, p.DigioTxnId, p.DigioPlanId);
        p.DigioPlanId ??= Str(plan, "id");
        p.DigioTxnId = Str(txn, "id") ?? p.DigioTxnId;

        var next = CollectLifecycle.Next(p.Status, incoming);
        if (next == p.Status)
        {
            if (before == (p.Status, p.DigioTxnId, p.DigioPlanId))
            {
                return null;
            }

            p.UpdatedAt = nowUtc ?? DateTime.UtcNow;
            return null;
        }

        p.Status = next;
        p.UpdatedAt = nowUtc ?? DateTime.UtcNow;
        p.Utr = Cut(Str(details, "utr"), 40) ?? p.Utr;
        var instalment = await db.Instalments.IgnoreQueryFilters().Include(i => i.LoanAccount).FirstAsync(i => i.Id == p.InstalmentId);
        switch (next)
        {
            case CollectLifecycle.Succeeded:
                var amount = details.ValueKind == JsonValueKind.Object && details.TryGetProperty("amount", out var a) && a.ValueKind == JsonValueKind.Number ? a.GetDecimal() : p.Amount;
                await ReceiptService.RecordNachAsync(db, p, p.Utr, amount, TodayIst(nowUtc), nowUtc);
                break;
            case CollectLifecycle.Failed:
                p.FailureCode = Cut(Str(details, "failure_code"), 40);
                p.FailureReason = Cut(Str(details, "failure_reason"), 300);
                instalment.Status = "Bounced";
                break;
            case CollectLifecycle.NotPresented or CollectLifecycle.Cancelled:
                p.FailureCode = Cut(Str(details, "failure_code") ?? Str(details, "status"), 40);
                p.FailureReason = Cut(Str(details, "failure_reason"), 300);
                if (instalment.Status == "Presented")
                {
                    instalment.Status = "Due"; // not debited: the instalment is still owed and can be presented again
                }

                break;
            default:
                if (instalment.Status == "Due")
                {
                    instalment.Status = "Presented";
                }

                break;
        }

        return instalment.LoanAccount!.ApplicationId;
    }

    // ---- webhook ----

    public static bool IsDebitEvent(string? evt) => evt is not null && evt.StartsWith("nach.debit.", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// A NACH.DEBIT.* webhook only says "look at this": the event name and payload shape do not decide anything (no sample captured yet).
    /// Finds our presentation by any NSP / NPT id in the payload and runs the confirming GET. An event we cannot place is left to the
    /// reconciler. Throws when Digio does not answer, so the inbox retries.
    /// </summary>
    public async Task<bool> HandleEventAsync(int companyId, JsonElement root)
    {
        var ids = IdsIn(root);
        if (ids.Count == 0)
        {
            return false;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var p = await db.Presentations.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.CompanyId == companyId && (ids.Contains(x.Id) || (x.DigioPlanId != null && ids.Contains(x.DigioPlanId)) || (x.DigioTxnId != null && ids.Contains(x.DigioTxnId))))
            .Select(x => x.Id).FirstOrDefaultAsync();
        if (p is null)
        {
            return false;
        }

        return await ConfirmAsync(companyId, p) ? true : throw new InvalidOperationException("Digio debit lookup failed");
    }

    private static HashSet<string> IdsIn(JsonElement root)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        if (root.TryGetProperty("payload", out var payload) && payload.ValueKind == JsonValueKind.Object)
        {
            foreach (var o in payload.EnumerateObject())
            {
                if (o.Value.ValueKind == JsonValueKind.Object && Str(o.Value, "id") is { } id)
                {
                    ids.Add(id);
                }

                if (o.Value.ValueKind == JsonValueKind.Object && Str(o.Value, "unique_key") is { } key)
                {
                    ids.Add(key);
                }
            }
        }

        return ids;
    }

    /// <summary>Presentations Digio has not finished with, for the reconciler.</summary>
    public async Task<List<(int CompanyId, string PresentationId)>> OpenAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var open = await db.Presentations.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.Status == CollectLifecycle.Sent || p.Status == CollectLifecycle.InProgress)
            .Select(p => new { p.CompanyId, p.Id }).ToListAsync();
        return open.Select(p => (p.CompanyId, p.Id)).ToList();
    }

    private static JsonElement Obj(JsonElement e, string n) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(n, out var v) ? v : default;

    private static string? Str(JsonElement e, string n) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string? Cut(string? s, int max) => s is { Length: > 0 } ? (s.Length > max ? s[..max] : s) : null;
}
