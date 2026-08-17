namespace LosLms.Services;

/// <summary>One month of a reducing-balance repayment schedule.</summary>
/// <param name="DueDate">
/// Null unless the schedule was built from a first-due date. Trailing and optional so the callers
/// that only need the arithmetic — the CAM PDF and the Stage 3 modal — are unaffected.
/// </param>
public sealed record AmortizationRow(
    int Month,
    decimal OpeningBalance,
    decimal Emi,
    decimal Principal,
    decimal Interest,
    decimal ClosingBalance,
    DateOnly? DueDate = null);

/// <summary>
/// Reducing-balance loan arithmetic, shared by the CAM tab, the repayment schedule and the PDF.
/// </summary>
/// <remarks>
/// Loan &amp; Security (Stage 2) still carries its own private copy of the EMI formula. Consolidating
/// it would mean a second edit to a file this brief says to touch only for navigation, so the
/// duplication is left in place and recorded as a follow-up.
/// </remarks>
public static class LoanMath
{
    /// <summary>
    /// Monthly instalment on a reducing balance. Returns zero when any term is missing, which also
    /// avoids dividing by (factor - 1) when the rate is zero.
    /// </summary>
    public static decimal Emi(decimal principal, decimal annualRatePct, int months)
    {
        if (principal <= 0 || annualRatePct <= 0 || months <= 0)
        {
            return 0m;
        }

        var monthlyRate = (double)annualRatePct / 12d / 100d;
        var factor = Math.Pow(1 + monthlyRate, months);

        if (factor <= 1d)
        {
            return 0m;
        }

        return (decimal)((double)principal * monthlyRate * factor / (factor - 1d));
    }

    /// <summary>
    /// The largest principal an instalment can service — <see cref="Emi"/> solved backward.
    /// Used by Eligibility to turn a FOIR headroom figure into a lendable amount.
    /// </summary>
    /// <remarks>
    /// Guards mirror <see cref="Emi"/> exactly, so the pair round-trips: feeding an EMI produced by
    /// <see cref="Emi"/> back through this returns the original principal.
    /// </remarks>
    public static decimal InverseEmi(decimal emi, decimal annualRatePct, int months)
    {
        if (emi <= 0 || annualRatePct <= 0 || months <= 0)
        {
            return 0m;
        }

        var monthlyRate = (double)annualRatePct / 12d / 100d;
        var factor = Math.Pow(1 + monthlyRate, months);

        if (factor <= 1d)
        {
            return 0m;
        }

        return (decimal)((double)emi * (factor - 1d) / (monthlyRate * factor));
    }

    /// <summary>
    /// Month-by-month amortization. Depends only on the loan terms, never on CAM recalculation.
    /// </summary>
    /// <param name="firstEmiOverride">
    /// When set, month 1's instalment is this figure instead of the computed EMI.
    /// </param>
    /// <param name="roundToNearest">
    /// When set, months 2..N-1 use the computed EMI rounded to this nearest rupee figure (e.g. 10 or 100).
    /// </param>
    /// <remarks>
    /// With BOTH parameters null the result is byte-identical to the original uniform schedule: the
    /// closing balance of the final row lands near zero (a few paise of EMI-rounding drift), exactly as
    /// the CAM PDF and the Stage 3 modal have always shown. Those two callers pass neither and are
    /// unaffected.
    ///
    /// With either set, the schedule is adjusted and the final instalment becomes a reconciling
    /// payment — whatever principal plus that month's interest remains — so the loan closes to exactly
    /// zero and all the override/rounding drift is absorbed in the last month, never left dangling.
    /// </remarks>
    public static List<AmortizationRow> Schedule(
        decimal principal,
        decimal annualRatePct,
        int months,
        decimal? firstEmiOverride = null,
        decimal? roundToNearest = null)
    {
        // No adjustment requested → the original loop, unchanged. This is the path CamPdf and the
        // Bank & Financial repayment modal take, so their output cannot move.
        if (firstEmiOverride is null && roundToNearest is null)
        {
            return ScheduleUniform(principal, annualRatePct, months);
        }

        return ScheduleAdjusted(principal, annualRatePct, months, firstEmiOverride, roundToNearest);
    }

    /// <summary>The original uniform-EMI schedule, kept verbatim so null/null callers never shift.</summary>
    private static List<AmortizationRow> ScheduleUniform(decimal principal, decimal annualRatePct, int months)
    {
        var rows = new List<AmortizationRow>();

        var emi = Emi(principal, annualRatePct, months);
        if (emi <= 0)
        {
            return rows;
        }

        var monthlyRate = annualRatePct / 12m / 100m;
        var balance = principal;

        for (var month = 1; month <= months; month++)
        {
            var opening = balance;
            var interest = opening * monthlyRate;
            var principalComponent = emi - interest;
            balance = opening - principalComponent;

            rows.Add(new AmortizationRow(month, opening, emi, principalComponent, interest, balance));
        }

        return rows;
    }

    /// <summary>
    /// Schedule with a first-EMI override and/or rounded regular instalments, and a reconciling final
    /// instalment that closes the loan to exactly zero.
    /// </summary>
    private static List<AmortizationRow> ScheduleAdjusted(
        decimal principal, decimal annualRatePct, int months, decimal? firstEmiOverride, decimal? roundToNearest)
    {
        var rows = new List<AmortizationRow>();

        var standardEmi = Emi(principal, annualRatePct, months);
        if (standardEmi <= 0)
        {
            return rows;
        }

        var monthlyRate = annualRatePct / 12m / 100m;
        var balance = principal;

        for (var month = 1; month <= months; month++)
        {
            var opening = balance;
            var interest = opening * monthlyRate;

            decimal emi;
            if (month == months)
            {
                // Final instalment reconciles: pay off all remaining principal plus this month's
                // interest, so the closing balance is exactly zero however earlier months were adjusted.
                emi = opening + interest;
            }
            else if (month == 1 && firstEmiOverride is { } over)
            {
                emi = over;
            }
            else if (roundToNearest is { } step && step > 0)
            {
                emi = Math.Round(standardEmi / step, MidpointRounding.AwayFromZero) * step;
            }
            else
            {
                emi = standardEmi;
            }

            var principalComponent = emi - interest;
            balance = opening - principalComponent;

            rows.Add(new AmortizationRow(month, opening, emi, principalComponent, interest, balance));
        }

        return rows;
    }

    /// <summary>
    /// The schedule with a due date on every instalment, the first on <paramref name="firstDueDate"/>
    /// and each later one a calendar month after it. Forwards the adjustment parameters unchanged.
    /// </summary>
    /// <remarks>
    /// A thin wrapper over the arithmetic above rather than a second implementation, so what Post
    /// Sanction shows cannot drift from the CAM PDF and the Stage 3 modal.
    ///
    /// <c>AddMonths</c> clamps a short month rather than rolling over — a 31st becomes the 30th in
    /// September, not the 1st of October. That matches how lenders actually set due dates.
    /// </remarks>
    public static List<AmortizationRow> Schedule(
        decimal principal,
        decimal annualRatePct,
        int months,
        DateOnly firstDueDate,
        decimal? firstEmiOverride = null,
        decimal? roundToNearest = null)
    {
        var rows = Schedule(principal, annualRatePct, months, firstEmiOverride, roundToNearest);

        return rows
            .Select(row => row with { DueDate = firstDueDate.AddMonths(row.Month - 1) })
            .ToList();
    }
}
