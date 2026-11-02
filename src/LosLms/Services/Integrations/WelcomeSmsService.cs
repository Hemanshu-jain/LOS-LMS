using System.Globalization;

namespace LosLms.Services;

/// <summary>Outcome of an SMS send attempt.</summary>
/// <param name="IsConfigured">False whenever no gateway is wired — the only state reachable here.</param>
public sealed record SmsSendResult(bool IsConfigured)
{
    public static readonly SmsSendResult NotConfigured = new(false);
}

/// <summary>
/// The welcome SMS: the message is composed for real; sending is an honest stub.
/// </summary>
/// <remarks>
/// <see cref="BuildMessage"/> produces the actual text the officer sees, merged from real loan data.
/// <see cref="SendAsync"/> has no gateway to hand it to, so it reports "not configured" and the
/// disbursement's <c>WelcomeSmsStatus</c> stays <c>NotSent</c> — nothing here ever fabricates a sent
/// status. Wiring a gateway later is a one-call change at the marked boundary.
/// </remarks>
public static class WelcomeSmsService
{
    public const string Unavailable = "SMS sending unavailable — provider not configured.";

    private static readonly CultureInfo IndianCulture = CultureInfo.GetCultureInfo("en-IN");

    /// <summary>Condensed SMS-length text with the same merge fields as the welcome letter.</summary>
    public static string BuildMessage(string companyName, decimal sanctionedAmount, decimal emi, int tenureMonths)
    {
        var company = string.IsNullOrWhiteSpace(companyName) ? "your lender" : companyName.Trim();
        return $"Welcome to {company}! Loan of {Money(sanctionedAmount)} approved. "
             + $"EMI {Money(emi)} x {tenureMonths} months. Thank you for choosing us.";
    }

    /// <summary>
    /// Attempts to send. Always "not configured" in this build; a real gateway would be called here.
    /// </summary>
    public static Task<SmsSendResult> SendAsync(string? toMobile, string message)
    {
        // REAL PROVIDER: POST { to: toMobile, body: message } to the SMS gateway and return
        // new SmsSendResult(IsConfigured: true) only on a genuine accepted response. Never before.
        return Task.FromResult(SmsSendResult.NotConfigured);
    }

    private static string Money(decimal amount) =>
        "Rs " + Math.Round(amount, MidpointRounding.AwayFromZero).ToString("N0", IndianCulture);
}
