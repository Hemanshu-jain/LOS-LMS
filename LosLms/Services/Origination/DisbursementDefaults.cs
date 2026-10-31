using LosLms.Models;

namespace LosLms.Services;

/// <summary>
/// Starting values for the Post Sanction Disbursement tab, so an officer confirms instead of typing: today as the value date, NEFT, the
/// borrower's own account when paying the customer directly, and the lender's account when only one is kept. Blanks only.
/// </summary>
public static class DisbursementDefaults
{
    public const string DirectToCustomer = "Direct to customer";
    public const string DefaultPaymentMode = "NEFT";
    public const int DefaultEmiDay = 5;

    public static void Apply(Disbursement d, string? applicantName, BankDetail? borrowerBank, DateOnly today, IReadOnlyList<string> lenderAccounts)
    {
        d.ValueDate ??= today;

        if (string.IsNullOrWhiteSpace(d.PaymentMode))
        {
            d.PaymentMode = DefaultPaymentMode;
        }

        if (d.Type == DirectToCustomer)
        {
            if (string.IsNullOrWhiteSpace(d.BeneficiaryName) && !string.IsNullOrWhiteSpace(applicantName))
            {
                d.BeneficiaryName = applicantName.Trim();
            }

            if (string.IsNullOrWhiteSpace(d.BeneficiaryAccount) && !string.IsNullOrWhiteSpace(borrowerBank?.AccountNumber))
            {
                d.BeneficiaryAccount = borrowerBank.AccountNumber.Trim();
            }
        }

        d.DisburseFromAccount = OnlyChoice.IfBlank(d.DisburseFromAccount, lenderAccounts);
    }

    /// <summary>The first date on or after <paramref name="from"/> that falls on <paramref name="day"/> (a 31 becomes the 30th in a 30-day month).</summary>
    public static DateOnly NextDayOfMonth(DateOnly from, int day)
    {
        var thisMonthDay = Math.Min(day, DateTime.DaysInMonth(from.Year, from.Month));
        var candidate = new DateOnly(from.Year, from.Month, thisMonthDay);
        if (candidate > from) { return candidate; }

        var next = from.AddMonths(1);
        return new DateOnly(next.Year, next.Month, Math.Min(day, DateTime.DaysInMonth(next.Year, next.Month)));
    }
}
