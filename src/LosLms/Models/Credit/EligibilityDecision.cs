using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// The officer's written justification for lending below the requested amount. One row per
/// application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// <see cref="NoteWrittenAtDeviationPct"/> records the deviation that was on screen when the note
/// was last saved. Storing it is what lets the staleness warning survive a page reload: without it
/// the screen could only compare against in-session state, so reopening the application would show
/// a note written against -4% as though it had been written against the current -18%.
/// </remarks>
public class EligibilityDecision
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? ApproverNote { get; set; }

    public decimal? NoteWrittenAtDeviationPct { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
