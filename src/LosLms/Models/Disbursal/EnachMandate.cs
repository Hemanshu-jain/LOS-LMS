using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// The e-NACH mandate that will collect the EMI. One row per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// <see cref="Status"/> is recorded and displayed but gates nothing: funds can be released with a
/// Pending mandate, because the release gate reads the checklist, not this row. The checklist does
/// carry an "E-Nach registration" item, so the control exists — it is just a person ticking a box
/// rather than this field. Recorded in OPEN-QUESTIONS-FOR-ARUN.md.
/// </remarks>
public class EnachMandate : INachMandate
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>Unique Mandate Reference Number, issued by the sponsor bank.</summary>
    [MaxLength(40)]
    public string? Umrn { get; set; }

    /// <summary>Pending / Registered / Rejected.</summary>
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public DateOnly? DebitDate { get; set; }

    [MaxLength(120)]
    public string? LinkedAccount { get; set; }

    // ---- Registration flow ----
    // The account the mandate debits. Bank, branch and address are auto-fetched from the live IFSC
    // directory; the officer confirms the account number by entering it twice.
    [MaxLength(18)] public string? AccountNumber { get; set; }
    [MaxLength(11)] public string? Ifsc { get; set; }
    [MaxLength(120)] public string? BankName { get; set; }
    [MaxLength(150)] public string? BankBranch { get; set; }

    /// <summary>
    /// One of: NotRun, Unavailable, Matched, NotMatched. Only NotRun and Unavailable are reachable —
    /// no account-holder verification provider is configured, and nothing may fake a Matched. The
    /// other two exist so a real provider can populate them later without a migration. Same discipline
    /// as <see cref="BankDetail.PennyDropStatus"/>.
    /// </summary>
    [MaxLength(20)]
    public string NameMatchStatus { get; set; } = "NotRun";

    public DateTime? NameMatchCheckedAt { get; set; }

    /// <summary>
    /// The officer's explicit acceptance of responsibility for forwarding the case. Required before a
    /// mandate can be submitted by either route, and the reason it is stored rather than merely
    /// enforced in the UI: it is an audit record of who accepted what.
    /// </summary>
    public bool ConfirmationAccepted { get; set; }

    /// <summary>Digital / Physical. Null until the officer chooses.</summary>
    [MaxLength(20)] public string? MandateType { get; set; }

    /// <summary>NetBanking / DebitCard. Only meaningful for a Digital mandate.</summary>
    [MaxLength(20)] public string? DigitalMode { get; set; }

    // ---- Digio DigiCollect (06-NACH.md section 2). Status above stays Pending/Registered/Rejected; the detail lives here. ----

    /// <summary>Digio's mandate id (ENA...).</summary>
    [MaxLength(40)] public string? DigioMandateId { get; set; }

    /// <summary>API / ESIGN / PHYSICAL.</summary>
    [MaxLength(20)] public string? AuthMode { get; set; }

    [MaxLength(40)] public string? DestinationBankId { get; set; }

    /// <summary>Our forward-only state: Requested, AuthSuccess, InBankProcessing, Registered, AuthFailed, Rejected, Expired, Cancelled.</summary>
    [MaxLength(30)] public string? DigioState { get; set; }

    /// <summary>Digio's raw bank_details.state.</summary>
    [MaxLength(30)] public string? BankState { get; set; }

    [MaxLength(300)] public string? RejectReason { get; set; }

    [MaxLength(32)] public string? DigioRequestId { get; set; }

    public decimal? CollectionAmount { get; set; }
    public decimal? MaximumAmount { get; set; }
    public DateTime? RegisteredAt { get; set; }

    /// <summary>IFileStore key of the Digio-generated physical form.</summary>
    [MaxLength(400)] public string? SignedFormPath { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
