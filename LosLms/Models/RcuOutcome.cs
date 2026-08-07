using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// The RCU verdict for one party on an application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// The party's name is deliberately not stored here; it is joined live from
/// <see cref="Party.FullName"/>, the same principle the Document Checklist uses, so a name
/// corrected in Stage 1 is never left stale in Stage 5.
/// </remarks>
public class RcuOutcome
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>Applicant / CoApplicant / Guarantor.</summary>
    [MaxLength(20)]
    public string PartyType { get; set; } = string.Empty;

    /// <summary>Pending / Recommended / NotRecommended. Drives the stage completion gate.</summary>
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public DateOnly? VerifiedOn { get; set; }

    public int? VerifiedByOfficerId { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
    public Officer? VerifiedBy { get; set; }
}
