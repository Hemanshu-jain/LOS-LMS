using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <param name="Created">Loan accounts made now.</param>
/// <param name="AlreadyHad">Disbursed applications that already had one (left exactly as they are).</param>
/// <param name="Skipped">Disbursed applications that could not be opened, with why (no sanctioned terms or no EMI date).</param>
public sealed record BackfillResult(int Created, int AlreadyHad, IReadOnlyList<string> Skipped);

/// <summary>
/// Loans disbursed before the servicing ledger existed have no loan account. This opens one for each, from what was recorded at disbursal
/// (sanctioned amount / rate / tenure, first EMI date, override, rounding, EMI day), through the same
/// <see cref="LoanAccountService.OpenAsync"/> as Release funds. Idempotent: an application with an account is never touched, so it can be run
/// again at any time. The instalments start as Due: what was collected before the ledger is not known to the system, so nothing is marked paid
/// (receipts are the only path to Paid); the past ones show as overdue until the Admin records or approves what was actually received.
/// </summary>
public static class LoanBackfill
{
    public static async Task<BackfillResult> RunAsync(LosDbContext db, DateTime? nowUtc = null)
    {
        var disbursed = await db.Applications.IgnoreQueryFilters().Where(a => a.Disbursed).OrderBy(a => a.Id).ToListAsync();
        var have = (await db.LoanAccounts.IgnoreQueryFilters().Select(a => a.ApplicationId).ToListAsync()).ToHashSet();
        var created = 0;
        var skipped = new List<string>();
        foreach (var app in disbursed.Where(a => !have.Contains(a.Id)))
        {
            var decision = await db.ApprovalDecisions.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(d => d.ApplicationId == app.Id);
            var disbursement = await db.Disbursements.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(d => d.ApplicationId == app.Id);
            var first = disbursement?.FirstEmiDate ?? app.FirstEmiDate;
            if (decision is not { SanctionedAmount: > 0, SanctionedTenure: > 0 })
            {
                skipped.Add($"{app.Id}: no sanctioned terms");
                continue;
            }

            if (first is null)
            {
                skipped.Add($"{app.Id}: no first EMI date");
                continue;
            }

            var account = await LoanAccountService.OpenAsync(db, app, decision.SanctionedAmount ?? 0m, decision.SanctionedRoi ?? 0m, decision.SanctionedTenure ?? 0, first,
                disbursement?.EmiDayOfMonth ?? app.EmiDayOfMonth, disbursement?.FirstEmiOverride, disbursement?.EmiRoundedTo, nowUtc);
            if (account is null)
            {
                skipped.Add($"{app.Id}: no schedule could be built");
                continue;
            }

            created++;
        }

        await db.SaveChangesAsync();
        return new(created, disbursed.Count(a => have.Contains(a.Id)), skipped);
    }
}
