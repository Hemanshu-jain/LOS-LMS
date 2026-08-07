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
public class EnachMandate
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

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
