using System.Text.RegularExpressions;

namespace LosLms.Services;

/// <summary>What to do about a failed debit.</summary>
/// <param name="Action">Represent (try again on <see cref="EarliestOn"/>) or Escalate (a person must act; nothing is re-presented).</param>
public sealed record BounceDecision(string Action, DateOnly? EarliestOn, string Reason, string? Kind = null);

/// <summary>
/// The NPCI return/reject codes and Digio's own DG101-DG104 rules, as data (Digio docs, "NACH presentation error codes").
/// Pure: no database, no clock (the caller passes today). A code we do not know is NOT re-presented: a person looks at it.
/// </summary>
public static partial class BounceBook
{
    public const string Represent = "Represent", Escalate = "Escalate";

    /// <summary>code → (name, re-presentation allowed, how many presentations NPCI allows per month for it).</summary>
    private static readonly Dictionary<string, (string Name, bool Rpn, int Monthly)> Codes = new()
    {
        ["01"] = ("Account closed", false, 0), ["02"] = ("No such account", false, 0), ["04"] = ("Balance insufficient", true, 2),
        ["05"] = ("Not arranged for", true, 2), ["06"] = ("Payment stopped by drawer", true, 1), ["07"] = ("Payment stopped under court order", false, 0),
        ["08"] = ("Mandate not received", true, 1), ["11"] = ("Invalid IFSC/MICR", true, 2), ["12"] = ("Mismatch in mandate frequency", false, 0),
        ["13"] = ("Duplicate transaction", false, 0), ["14"] = ("Mandate expired", false, 0), ["15"] = ("Incorrect amount", true, 2),
        ["16"] = ("Customer name mismatch", true, 2), ["17"] = ("Returned as per customer request", true, 1), ["51"] = ("KYC documents pending", true, 1),
        ["52"] = ("Documents pending for account holder turning major", true, 1), ["53"] = ("Account inoperative", false, 0), ["54"] = ("Dormant account", false, 0),
        ["55"] = ("Zero balance account, first transaction condition", true, 1), ["56"] = ("Small account, first transaction at base branch", true, 1),
        ["57"] = ("Amount exceeds the bank's per-transaction limit", true, 2), ["58"] = ("Account reached the bank's debit limit", true, 2), ["59"] = ("Network failure (bank)", true, 2),
        ["60"] = ("Account holder expired", false, 0), ["61"] = ("Mandate cancelled", false, 0), ["68"] = ("Account blocked or frozen", true, 2),
        ["69"] = ("Customer insolvent", false, 0), ["70"] = ("Customer to refer to branch", true, 1), ["98"] = ("Bank extension case", false, 0),
        ["21"] = ("Invalid UMRN or inactive mandate", false, 0), ["22"] = ("Mandate not valid for debit", false, 0), ["23"] = ("Mismatch in mandate debtor account", true, 1),
        ["24"] = ("Mismatch in mandate debtor bank", true, 1), ["25"] = ("Mismatch in mandate currency", false, 0), ["26"] = ("Amount exceeds mandate maximum", true, 1),
        ["27"] = ("Mandate amount mismatch", true, 1), ["28"] = ("Date before mandate start", true, 1), ["29"] = ("Date after mandate end", false, 0),
        ["30"] = ("Mandate user number mismatch", true, 1), ["31"] = ("Duplicate reference number", false, 0), ["32"] = ("Invalid date", false, 0),
        ["33"] = ("Item unwound", false, 0), ["34"] = ("Invalid amount", false, 0), ["72"] = ("Item cancelled", false, 0), ["73"] = ("Settlement failed", false, 0),
        ["75"] = ("Cancelled by user", false, 0), ["92"] = ("Core error", false, 0), ["97"] = ("Bad batch corporate user number/name", true, 1),
    };

    [GeneratedRegex(@"\bDG\s?(10[1-4])\b", RegexOptions.IgnoreCase)]
    private static partial Regex DigioCode();

    [GeneratedRegex(@"(?<![0-9])([0-9]{1,2})(?![0-9])")]
    private static partial Regex NpciCode();

    /// <summary>Finds the code inside whatever string Digio sent ("04", "4", "NPCI 04 - Balance Insufficient", "DG103: …"). Null when there is none.</summary>
    public static string? Normalise(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (DigioCode().Match(raw) is { Success: true } dg)
        {
            return "DG" + dg.Groups[1].Value;
        }

        return NpciCode().Match(raw) is { Success: true } m ? m.Groups[1].Value.PadLeft(2, '0') : null;
    }

    /// <summary>Plain-words name for a code, or null when unknown.</summary>
    public static string? NameOf(string? raw) => Normalise(raw) switch
    {
        "DG101" => "Already debited for this period",
        "DG102" => "Debit limit breached",
        "DG103" => "Retry not allowed within 3 days of the last failure",
        "DG104" => "A debit is already open for this mandate",
        { } c when Codes.TryGetValue(c, out var r) => r.Name,
        _ => null,
    };

    /// <param name="attemptsMade">Presentations already made for this instalment, the failed one included.</param>
    /// <param name="lastFailureOn">The date the latest attempt failed (DG103 counts 3 days from it).</param>
    public static BounceDecision Decide(string? failureCode, int attemptsMade, int maxAttempts, DateOnly lastFailureOn, DateOnly today, int representAfterDays)
    {
        var code = Normalise(failureCode);
        var wait = Math.Max(3, representAfterDays); // Digio refuses a debit within 3 days of a failure (DG103), whatever the setting says
        var earliest = lastFailureOn.AddDays(wait);

        switch (code)
        {
            case null:
                return new(Escalate, null, "No return code from the bank. Check the transaction.", "NonRpnReturn");
            case "DG101":
                return new(Escalate, null, "Already debited for this mandate's period. Check whether the EMI was collected.", "NonRpnReturn");
            case "DG104":
                return new(Escalate, null, "A debit is already open for this mandate. Wait for it to finish, then check.", "NonRpnReturn");
            case "DG102":
                return new(Escalate, null, "Debit limit of the mandate reached.", "AttemptsExhausted");
            case "DG103":
                return new(Represent, earliest, "Digio allows another debit 3 days after the last failure.");
        }

        if (!Codes.TryGetValue(code, out var rule))
        {
            return new(Escalate, null, $"Unknown return code {code}. A person must look at it.", "NonRpnReturn");
        }

        if (!rule.Rpn)
        {
            return new(Escalate, null, $"{rule.Name} (code {code}): the bank does not allow presenting again.", "NonRpnReturn");
        }

        // Both our own limit and NPCI's monthly count for this code cap the attempts.
        if (attemptsMade >= Math.Min(maxAttempts, rule.Monthly))
        {
            return new(Escalate, null, $"{rule.Name} (code {code}): {attemptsMade} attempt(s) made, no more allowed.", "AttemptsExhausted");
        }

        return new(Represent, earliest, $"{rule.Name} (code {code}): present again on {earliest:d MMM}.");
    }
}
