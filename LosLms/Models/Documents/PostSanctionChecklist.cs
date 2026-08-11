using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// One branch pre-disbursement control. Many rows per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// **These rows are the release-funds gate.** Every row must read Cleared before money can move, so
/// adding a row tightens the control automatically and removing one loosens it. Seven are seeded
/// today; the content brief says nine exist. See the gate comment in PostSanction.razor and
/// OPEN-QUESTIONS-FOR-ARUN.md — the two missing items are the highest-priority open question in
/// this build.
/// </remarks>
public class PostSanctionChecklist
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Item { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Owner { get; set; } = string.Empty;

    /// <summary>Cleared / Pending.</summary>
    [MaxLength(20)]
    public string Flag { get; set; } = "Pending";

    public DateOnly? ClearedOn { get; set; }

    [MaxLength(300)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
