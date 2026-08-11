using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// The backup NACH mandate held against the guarantor. One row per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// <see cref="MandateHolder"/> is a plain name rather than a foreign key into <see cref="Party"/>,
/// so nothing checks that the named holder is actually this application's guarantor — the same
/// loose coupling already noted for <c>Applications.AssignedOfficer</c>.
/// </remarks>
public class SecurityNachMandate : INachMandate
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Umrn { get; set; }

    /// <summary>Pending / Registered / Rejected.</summary>
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [MaxLength(150)]
    public string? MandateHolder { get; set; }

    // ---- Registration flow ----
    // Identical to EnachMandate's, deliberately duplicated rather than shared through a base type or
    // an interface: the two are separate one-to-one tables with separate lifecycles, and one abstract
    // parent for two rows would buy nothing but indirection. The account here belongs to the
    // guarantor rather than the applicant; nothing else differs.
    [MaxLength(18)] public string? AccountNumber { get; set; }
    [MaxLength(11)] public string? Ifsc { get; set; }
    [MaxLength(120)] public string? BankName { get; set; }
    [MaxLength(150)] public string? BankBranch { get; set; }

    /// <summary>NotRun / Unavailable only. See <see cref="EnachMandate.NameMatchStatus"/>.</summary>
    [MaxLength(20)]
    public string NameMatchStatus { get; set; } = "NotRun";

    public DateTime? NameMatchCheckedAt { get; set; }

    public bool ConfirmationAccepted { get; set; }

    /// <summary>Digital / Physical.</summary>
    [MaxLength(20)] public string? MandateType { get; set; }

    /// <summary>NetBanking / DebitCard.</summary>
    [MaxLength(20)] public string? DigitalMode { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
