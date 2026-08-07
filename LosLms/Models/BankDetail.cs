using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// The bank account an application's EMIs will be collected from. One row per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — reconciled during the cross-screen review once all nine screens exist.
///
/// One account per application, i.e. the applicant's. Co-applicant and guarantor accounts are not
/// modelled; that assumption is recorded in OPEN-QUESTIONS-FOR-ARUN.md.
/// </remarks>
public class BankDetail
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>Resolved from the IFSC prefix where possible, otherwise picked manually.</summary>
    [MaxLength(120)]
    public string? BankName { get; set; }

    [MaxLength(18)] public string? AccountNumber { get; set; }
    [MaxLength(11)] public string? Ifsc { get; set; }
    [MaxLength(20)] public string? AccountType { get; set; }
    [MaxLength(200)] public string? AccountHolderName { get; set; }
    [MaxLength(20)] public string? Vintage { get; set; }

    /// <summary>
    /// One of: NotRun, Unavailable, Matched, NotMatched.
    /// </summary>
    /// <remarks>
    /// Only <c>NotRun</c> and <c>Unavailable</c> are reachable in this build. No provider is
    /// configured, so nothing is actually verified and no code path may set Matched or NotMatched —
    /// those exist only so a real provider can populate them later without a migration.
    /// </remarks>
    [MaxLength(20)]
    public string PennyDropStatus { get; set; } = "NotRun";

    public DateTime? PennyDropCheckedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
