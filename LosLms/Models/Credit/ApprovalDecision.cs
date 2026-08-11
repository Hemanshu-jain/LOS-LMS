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
/// Signatories are real foreign keys into <see cref="ApplicationUser"/>. They used to be plain name
/// strings, which meant a sanction could name a signatory who did not exist and a renamed officer
/// silently detached from every decision they had signed. The role columns stay as text: they record
/// the authority the signatory held at the time of signing, which must not move when the person's
/// current role does.
/// </remarks>
public class ApprovalDecision
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string ApprovalNote { get; set; } = string.Empty;

    public string? RecommenderUserId { get; set; }

    [MaxLength(60)]
    public string? RecommenderRole { get; set; }

    public string? ApproverUserId { get; set; }

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
    public ApplicationUser? Recommender { get; set; }
    public ApplicationUser? Approver { get; set; }
}
