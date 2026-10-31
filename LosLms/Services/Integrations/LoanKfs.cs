using LosLms.Services;

namespace LosLms.Services;

/// <summary>
/// The numbers of the RBI Key Fact Statement, worked out from what the application holds: the repayment schedule, the charges and the
/// disbursement. Nothing here is estimated or invented; a figure the app does not hold is simply not produced.
/// </summary>
public sealed record LoanKfs(
    decimal Sanctioned,
    decimal NetDisbursed,
    decimal TotalFees,
    decimal TotalInterest,
    decimal TotalRepayable,
    int Instalments,
    decimal? AprPct)
{
    /// <param name="schedule">The repayment schedule as agreed (EMI and interest per instalment, with due dates).</param>
    /// <param name="sanctioned">Loan amount sanctioned.</param>
    /// <param name="netDisbursed">Amount actually paid out: sanctioned less fees and any advance already paid.</param>
    /// <param name="totalFees">Processing fee and every other non-waived charge including GST.</param>
    /// <param name="disbursedOn">Value date of the disbursement; the APR clock starts here.</param>
    public static LoanKfs Compute(IReadOnlyList<AmortizationRow> schedule, decimal sanctioned, decimal netDisbursed, decimal totalFees, DateOnly? disbursedOn)
    {
        var interest = schedule.Sum(r => r.Interest);
        var repayable = schedule.Sum(r => r.Emi);
        return new LoanKfs(sanctioned, netDisbursed, totalFees, Math.Round(interest, 2), Math.Round(repayable, 2), schedule.Count,
            Apr(schedule, netDisbursed, disbursedOn));
    }

    /// <summary>
    /// Annual percentage rate: the effective annual rate (XIRR, actual days over 365) at which the instalments repay the net amount the
    /// borrower actually received. It therefore includes the processing fee and the other charges deducted up front. Null when there is
    /// no dated schedule or no net amount to measure against.
    /// </summary>
    public static decimal? Apr(IReadOnlyList<AmortizationRow> schedule, decimal netDisbursed, DateOnly? disbursedOn)
    {
        if (netDisbursed <= 0 || schedule.Count == 0 || schedule.Any(r => r.DueDate is null))
        {
            return null;
        }

        var start = disbursedOn ?? schedule[0].DueDate!.Value.AddMonths(-1);
        double Npv(double rate) => schedule.Sum(r =>
            (double)r.Emi / Math.Pow(1 + rate, (r.DueDate!.Value.DayNumber - start.DayNumber) / 365.0)) - (double)netDisbursed;

        double lo = 0, hi = 5;
        if (Npv(lo) < 0)
        {
            return null; // repays less than it lent: no meaningful rate
        }

        for (var i = 0; i < 200; i++)
        {
            var mid = (lo + hi) / 2;
            if (Npv(mid) > 0) { lo = mid; } else { hi = mid; }
        }

        return Math.Round((decimal)((lo + hi) / 2) * 100m, 2);
    }
}
