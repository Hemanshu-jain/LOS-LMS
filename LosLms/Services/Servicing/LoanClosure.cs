using System.Text.Json;
using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>Outcome of closing a loan: Closed is true only when every step worked; Message says what to do otherwise.</summary>
public sealed record ClosureResult(bool Closed, string Message);

/// <summary>
/// Closing a loan (Admin only): cancel every debit that has not happened yet, cancel the mandate at Digio, then close the account.
/// A mandate left live after closure is a customer-harm and regulatory problem, so the account is NOT closed while a cancel failed:
/// the Admin retries (every step is safe to repeat).
/// </summary>
public sealed class LoanClosureService(IDbContextFactory<LosDbContext> dbFactory, DigioClient digio)
{
    /// <summary>
    /// Digio's registration_cancel reason codes, with the meanings the owner confirmed. C004 is left out until Digio confirms what it means;
    /// add it back here (and only here) then.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> Reasons = new Dictionary<string, string>
    {
        ["C001"] = "Customer asked to cancel",
        ["C002"] = "Lender cancelled (loan closed / foreclosed)",
        ["C003"] = "Bank account closed, frozen or inoperative",
    };

    public static readonly string[] ReasonCodes = Reasons.Keys.ToArray();

    /// <summary>What closing a loan normally is, so the dropdown starts there.</summary>
    public const string DefaultReason = "C002";

    private static string Why(DigioResult r) =>
        r.Status == DigioStatus.NotConfigured ? "Digio keys are not saved for this company." : (r.Error ?? "Digio gave no reason.").TrimEnd('.') + ".";

    public async Task<ClosureResult> CloseAsync(int loanAccountId, bool foreclosure, string reasonCode, string adminUserId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!db.IsElevatedUser)
        {
            return new(false, "Only an Admin can close a loan.");
        }

        if (!ReasonCodes.Contains(reasonCode))
        {
            return new(false, "Pick a cancellation reason.");
        }

        var account = await db.LoanAccounts.Include(a => a.Instalments).FirstOrDefaultAsync(a => a.Id == loanAccountId);
        if (account is not { Status: "Active" })
        {
            return new(false, "This loan is not active.");
        }

        if (!foreclosure && account.Instalments.Any(i => i.Status is not ("Paid" or "Waived")))
        {
            return new(false, "Not every instalment is paid. Record a foreclosure to close it early.");
        }

        // 1. Debits that have not happened: cancel them at Digio first, so nothing is taken after the close.
        var pending = await db.Presentations.Where(p => p.Status == "Created" || p.Status == "Sent" || p.Status == "InProgress")
            .Where(p => db.Instalments.Any(i => i.Id == p.InstalmentId && i.LoanAccountId == account.Id)).ToListAsync();
        foreach (var p in pending)
        {
            if (p.DigioTxnId is not null || p.DigioPlanId is not null)
            {
                var r = p.DigioTxnId is not null
                    ? await digio.SendAsync(new DigioCall(account.CompanyId, HttpMethod.Post, "/v3/client/nach_debit/transaction/action",
                        new { transaction_id = p.DigioTxnId, action = "cancel" }, Product: "NACH_DEBIT", Operation: "DEBIT_CANCEL", ApplicationId: account.ApplicationId, RequestedByUserId: adminUserId,
                        Free: "cancelling a debit not yet taken (UNSURE: no price in the quote)"))
                    : await digio.SendAsync(new DigioCall(account.CompanyId, HttpMethod.Post, "/v3/client/nach_debit/scheduled/action",
                        new { scheduled_plan_id = p.DigioPlanId, action = "cancel" }, Product: "NACH_DEBIT", Operation: "DEBIT_CANCEL", ApplicationId: account.ApplicationId, RequestedByUserId: adminUserId,
                        Free: "cancelling a debit plan not yet taken (UNSURE: no price in the quote)"));
                if (r.Status != DigioStatus.Ok)
                {
                    await db.SaveChangesAsync(); // keep the ones already cancelled
                    return new(false, "A pending debit could not be cancelled at Digio: " + Why(r) + " Nothing was closed. Try again.");
                }
            }

            p.Status = "Cancelled";
            p.UpdatedAt = DateTime.UtcNow;
        }

        // 2. The mandate.
        var mandate = account.MandateId is { } mid ? await db.EnachMandates.FirstOrDefaultAsync(m => m.Id == mid) : null;
        if (mandate is { DigioMandateId: not null, Umrn: not null } && mandate.DigioState == NachLifecycle.Registered)
        {
            var r = await digio.SendAsync(new DigioCall(account.CompanyId, HttpMethod.Post, "/v3/client/mandate/registration_cancel",
                new { digio_mandate_id = mandate.DigioMandateId, umrn = mandate.Umrn, reason = reasonCode }, ApiCode: DigioRates.MandateCancel, Product: "MANDATE", Operation: "MANDATE_CANCEL",
                ApplicationId: account.ApplicationId, RequestedByUserId: adminUserId));
            if (r.Status != DigioStatus.Ok)
            {
                await db.SaveChangesAsync();
                return new(false, "The mandate could not be cancelled at Digio: " + Why(r) + " The loan was NOT closed. Try again.");
            }
        }

        account.Status = foreclosure ? "Foreclosed" : "Closed";
        account.ClosedAt = DateTime.UtcNow;
        foreach (var t in await db.CollectionTasks.Where(t => t.LoanAccountId == account.Id && t.Status == "Open").ToListAsync())
        {
            t.Status = "Done";
            t.ResolvedAt = DateTime.UtcNow;
            t.ResolvedByUserId = adminUserId;
            t.ResolutionNote = "Loan closed.";
        }

        await db.SaveChangesAsync();
        return new(true, $"Loan {(foreclosure ? "foreclosed" : "closed")}; {pending.Count} pending debit(s) cancelled" + (mandate is null ? "." : " and the mandate cancel was sent to Digio."));
    }
}

/// <summary>
/// Checks one Digio settlement report (a CID, pasted by an Admin) against our presentations: every transaction in it should be a debit we
/// made and recorded as Succeeded, and the amounts should agree. Digio documents this API for "PA users" only; there is no list endpoint,
/// so it runs per CID. The result is saved and mismatches show on the collections dashboard.
/// </summary>
public sealed class SettlementReconciler(IDbContextFactory<LosDbContext> dbFactory, DigioClient digio)
{
    public async Task<(SettlementCheck? Check, string? Error)> CheckAsync(int companyId, string cid)
    {
        cid = cid.Trim();
        if (cid.Length is < 4 or > 80)
        {
            return (null, "Enter the settlement id (CID) from Digio's settlement email.");
        }

        var r = await digio.SendAsync(new DigioCall(companyId, HttpMethod.Get, $"/v3/client/nach_debit/requester_settlement/{Uri.EscapeDataString(cid)}",
            Free: "settlement report read (UNSURE: documented for Digio PA users only; no price in the quote)"));
        if (r.Status != DigioStatus.Ok || r.Data is not { } data)
        {
            return (null, r.HttpStatus is 403 or 404 ? "Digio has no settlement report for this id, or this report is not enabled for your account." : r.Error ?? "Digio could not be reached.");
        }

        // Shape per the spec: a list of settlements, each with total_amount and debit_txn_ids. A single object is accepted too.
        var items = data.ValueKind == JsonValueKind.Array ? data.EnumerateArray().ToList() : [data];
        var ids = new List<string>();
        decimal digioTotal = 0;
        foreach (var item in items.Where(i => i.ValueKind == JsonValueKind.Object))
        {
            if (item.TryGetProperty("total_amount", out var t) && t.TryGetDecimal(out var amount)) { digioTotal += amount; }
            if (item.TryGetProperty("debit_txn_ids", out var txns) && txns.ValueKind == JsonValueKind.Array)
            {
                ids.AddRange(txns.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!));
            }
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var ours = await db.Presentations.IgnoreQueryFilters().AsNoTracking().Where(p => p.CompanyId == companyId && p.DigioTxnId != null && ids.Contains(p.DigioTxnId)).ToListAsync();
        var known = ours.Select(p => p.DigioTxnId).ToHashSet();
        var unknown = ids.Where(i => !known.Contains(i)).ToList();
        var notPaid = ours.Where(p => p.Status != "Succeeded").ToList();
        var ourTotal = ours.Where(p => p.Status == "Succeeded").Sum(p => p.Amount);

        var problems = new List<string>();
        if (unknown.Count > 0) { problems.Add($"{unknown.Count} transaction(s) in Digio's report are not in our records: {string.Join(", ", unknown.Take(5))}"); }
        if (notPaid.Count > 0) { problems.Add($"{notPaid.Count} transaction(s) settled by Digio are not marked Succeeded here: {string.Join(", ", notPaid.Take(5).Select(p => p.DigioTxnId))}"); }
        if (Math.Abs(digioTotal - ourTotal) > 0.01m) { problems.Add($"Digio total {digioTotal:0.00} differs from our successful debits {ourTotal:0.00}"); }

        var check = new SettlementCheck
        {
            CompanyId = companyId, Cid = cid, CheckedAt = DateTime.UtcNow, DigioTotal = digioTotal, OurTotal = ourTotal, TxnCount = ids.Count,
            MismatchCount = problems.Count, Detail = problems.Count == 0 ? null : string.Join(" | ", problems) is { Length: > 1000 } long_ ? long_[..1000] : string.Join(" | ", problems),
        };
        db.SettlementChecks.Add(check);
        await db.SaveChangesAsync();
        return (check, null);
    }
}
