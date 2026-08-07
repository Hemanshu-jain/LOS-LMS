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
    /// <remarks>
    /// The closing balance of the final row lands near zero rather than exactly zero — EMI is a
    /// rounded figure, so a few paise of drift accumulate. A real disbursement schedule normally
    /// absorbs that into the last instalment; not doing so here because the brief specifies the
    /// formula exactly and inventing a balloon adjustment would misrepresent it.
    /// </remarks>
    public static List<AmortizationRow> Schedule(decimal principal, decimal annualRatePct, int months)
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
    /// The same schedule with a due date on every instalment, the first falling on
    /// <paramref name="firstDueDate"/> and each later one a calendar month after it.
    /// </summary>
    /// <remarks>
    /// Deliberately a thin wrapper rather than a second implementation: it delegates to the
    /// three-argument <see cref="Schedule(decimal, decimal, int)"/> above and only stamps dates onto
    /// the result, so the arithmetic the CAM PDF and the Stage 3 modal depend on cannot drift away
    /// from what Post Sanction displays.
    ///
    /// <c>AddMonths</c> clamps a short month rather than rolling over — a 31st becomes the 30th in
    /// September, not the 1st of October. That matches how lenders actually set due dates.
    /// </remarks>
    public static List<AmortizationRow> Schedule(
        decimal principal, decimal annualRatePct, int months, DateOnly firstDueDate)
    {
        var rows = Schedule(principal, annualRatePct, months);

        return rows
            .Select(row => row with { DueDate = firstDueDate.AddMonths(row.Month - 1) })
            .ToList();
    }
}
