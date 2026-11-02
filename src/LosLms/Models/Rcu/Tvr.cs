using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// Tele-verification record — the call placed to confirm the applicant's details. One row per
/// application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// <see cref="Status"/> is captured and displayed with risk colouring, but nothing acts on it: a
/// "Negative - Discrepancy" TVR does not block the sanction. Only the presence of the fields feeds
/// the completion gate. Recorded in OPEN-QUESTIONS-FOR-ARUN.md.
/// </remarks>
public class Tvr
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Agent { get; set; } = string.Empty;

    [MaxLength(120)]
    public string PersonContacted { get; set; } = string.Empty;

    [MaxLength(60)]
    public string Relationship { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(60)]
    public string RecordingRef { get; set; } = string.Empty;

    public DateTime? CallDateTime { get; set; }

    [MaxLength(4000)]
    public string Summary { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
