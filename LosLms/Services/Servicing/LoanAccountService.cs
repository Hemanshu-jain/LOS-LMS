using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>What the loan page shows, all computed from the instalments (never stored).</summary>
public sealed record LoanStanding(int DaysPastDue, decimal OverdueAmount, decimal OutstandingPrincipal, decimal TotalPaid, Instalment? NextDue)
{
    /// <summary>
    /// DPD = days since the OLDEST instalment that is past due and not fully paid. Overdue = what is unpaid on those. Outstanding principal =
    /// principal of every instalment not Paid / Waived. Paid and Waived instalments never count as overdue.
    /// </summary>
    public static LoanStanding Compute(IEnumerable<Instalment> instalments, DateOnly today)
    {
        var all = instalments.OrderBy(i => i.No).ToList();
        var late = all.Where(i => i.DueDate < today && i.Status is not ("Paid" or "Waived")).ToList();
        var dpd = late.Count == 0 ? 0 : today.DayNumber - late.Min(i => i.DueDate).DayNumber;
        return new(dpd, late.Sum(i => i.Emi - i.PaidAmount),
            all.Where(i => i.Status is not ("Paid" or "Waived")).Sum(i => i.Principal),
            all.Sum(i => i.PaidAmount),
            all.FirstOrDefault(i => i.Status is not ("Paid" or "Waived")));
    }
}

/// <summary>Opens the loan account (docs/digio/03 section 1).</summary>
public static class LoanAccountService
{
    /// <summary>
    /// Adds the loan account and the instalment snapshot to <paramref name="db"/> (the caller saves, so it commits with Release funds).
    /// The snapshot is exactly what the Repayment tab shows: LoanMath.Schedule with the first-EMI override and rounding. Idempotent:
    /// an application that already has an account gets none added and null back. No schedule (terms missing) = no account.
    /// </summary>
    public static async Task<LoanAccount?> OpenAsync(
        LosDbContext db, Application application, decimal principal, decimal roi, int tenure, DateOnly? firstEmiDate,
        int? emiDay, decimal? firstEmiOverride, decimal? roundTo, DateTime? nowUtc = null)
    {
        if (firstEmiDate is not { } first || principal <= 0 || tenure <= 0
            || await db.LoanAccounts.IgnoreQueryFilters().AnyAsync(a => a.ApplicationId == application.Id))
        {
            return null;
        }

        var schedule = LoanMath.Schedule(principal, roi, tenure, first, firstEmiOverride, roundTo);
        if (schedule.Count == 0)
        {
            return null;
        }

        var mandateId = await db.EnachMandates.IgnoreQueryFilters().Where(m => m.ApplicationId == application.Id).Select(m => (int?)m.Id).FirstOrDefaultAsync();
        var account = new LoanAccount
        {
            CompanyId = application.CompanyId, ApplicationId = application.Id, LoanAccountNo = application.Id,
            Principal = principal, Roi = roi, Tenure = tenure, FirstEmiDate = first, EmiDay = emiDay is > 0 and <= 31 ? emiDay.Value : first.Day,
            MandateId = mandateId, OpenedAt = nowUtc ?? DateTime.UtcNow,
            Instalments = schedule.Select(r => new Instalment
            {
                CompanyId = application.CompanyId, No = r.Month, DueDate = r.DueDate ?? first.AddMonths(r.Month - 1),
                Emi = r.Emi, Principal = r.Principal, Interest = r.Interest,
            }).ToList(),
        };
        db.LoanAccounts.Add(account);
        return account;
    }
}
