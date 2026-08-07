using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// The credit decision: who recommended it, who approved it, and on what terms. One row per
/// application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// <see cref="SanctionConfirmed"/> is the first half of a two-step gate. Confirming it on the
/// Sanction Confirmation tab is what enables the sub-header's Sanction button; nothing else does.
/// Send Back clears it deliberately — an application that goes back for rework must be re-confirmed
/// against whatever the evidence looks like when it returns.
///
/// Signatories are stored as plain names rather than foreign keys into <see cref="Officer"/>, which
/// the screen populates its dropdowns from. That mirrors the existing
/// <c>Applications.AssignedOfficer</c> inconsistency rather than fixing it — see
/// OPEN-QUESTIONS-FOR-ARUN.md.
/// </remarks>
public class ApprovalDecision
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string ApprovalNote { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? RecommenderName { get; set; }

    [MaxLength(60)]
    public string? RecommenderRole { get; set; }

    [MaxLength(120)]
    public string? ApproverName { get; set; }

    [MaxLength(60)]
    public string? ApproverRole { get; set; }

    [MaxLength(60)]
    public string? Authority { get; set; }

    public DateOnly? RecommenderDate { get; set; }
    public DateOnly? ApproverDate { get; set; }
    public DateOnly? ValidityDate { get; set; }

    public decimal? SanctionedAmount { get; set; }
    public decimal? SanctionedRoi { get; set; }
    public int? SanctionedTenure { get; set; }

    [MaxLength(4000)]
    public string? Conditions { get; set; }

    public bool SanctionConfirmed { get; set; }
    public DateTime? SanctionConfirmedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
