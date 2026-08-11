using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// The RCU verification case raised with a vendor. One row per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
/// </remarks>
public class RcuInitiation
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>
    /// 'Screened' or 'Sampled'.
    /// </summary>
    /// <remarks>
    /// OPEN QUESTION: recorded and displayed, but has no behavioural effect anywhere — it does not
    /// touch the outcome table, the completion gate or any routing. If Sampled cases are meant to
    /// behave differently (skip the gate, route to another reviewer), that is a real decision still
    /// needed rather than something to infer from a toggle label.
    /// </remarks>
    [MaxLength(20)]
    public string Mode { get; set; } = "Screened";

    [MaxLength(100)] public string? Branch { get; set; }
    [MaxLength(120)] public string? Vendor { get; set; }

    public DateOnly? InitiationDate { get; set; }
    public DateOnly? CompletionDate { get; set; }

    /// <summary>Committed turnaround in days. InitiationDate + Tat is the expected completion.</summary>
    public int Tat { get; set; }

    /// <summary>Format 'RCU-{year}-{5-digit sequence}'. Generated on creation, editable after.</summary>
    [MaxLength(30)]
    public string? CaseRef { get; set; }

    // Only meaningful when the derived overall status is NotRecommended. All three must be set for
    // the override to count — see the gate in ReportsRcu.razor.
    public bool OverrideActive { get; set; }

    [MaxLength(500)]
    public string? OverrideReason { get; set; }

    /// <summary>Foreign key into <see cref="ApplicationUser"/> — the officer who approved the override.</summary>
    public string? OverrideApproverOfficerId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
    public ApplicationUser? OverrideApprover { get; set; }
}
