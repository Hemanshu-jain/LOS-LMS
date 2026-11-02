using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// One request to analyse a party's bank statements. Many rows per application — an audit trail of
/// every time an officer asked for an automated read.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// Scaffold for a provider that is not configured in this build, following the same honesty rule as
/// PAN/Aadhaar OCR, penny-drop and e-NACH: <see cref="Status"/> is only ever written
/// <see cref="NotConfigured"/>, and <see cref="RawResultJson"/> stays null. Both exist so a real
/// provider's response can be recorded later without a migration. The write-back that maps a result
/// onto Viability and ExistingLoans lives in <see cref="Services.BankStatementAnalysisService"/> and
/// is never reached with real data here.
/// </remarks>
public class BankStatementAnalysis
{
    public const string NotConfigured = "NotConfigured";

    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>Applicant / CoApplicant / Guarantor — whose statements were submitted.</summary>
    [MaxLength(20)]
    public string PartyType { get; set; } = "Applicant";

    /// <summary>Only ever <see cref="NotConfigured"/> in this build. Never invented as a success.</summary>
    [MaxLength(20)]
    public string Status { get; set; } = NotConfigured;

    public DateTime RequestedAt { get; set; }

    /// <summary>Reserved for the real provider's raw response once wired. Never populated here.</summary>
    public string? RawResultJson { get; set; }

    public Application? Application { get; set; }
}
