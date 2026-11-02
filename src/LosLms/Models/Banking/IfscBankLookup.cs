using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// Maps the first four characters of an IFSC code to the issuing bank.
/// </summary>
/// <remarks>
/// PARTIAL LIST — twelve common prefixes, not the RBI-published dataset. A real IFSC covers every
/// bank and every branch in India and changes as banks merge, so v2 replaces this seed with the
/// official list (or a lookup service).
///
/// ponytail: twelve fixed rows would be a Dictionary in code, not a table. It is a table because
/// the v2 direction is thousands of rows that must be updatable without a redeploy — the future
/// shape justifies it, the current twelve do not.
/// </remarks>
public class IfscBankLookup
{
    /// <summary>Exactly four characters, uppercase, e.g. "HDFC".</summary>
    [MaxLength(4)]
    public string IfscPrefix { get; set; } = string.Empty;

    [MaxLength(120)]
    public string BankName { get; set; } = string.Empty;
}
