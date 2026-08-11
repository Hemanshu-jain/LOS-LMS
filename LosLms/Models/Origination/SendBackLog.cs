using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// One record of an application being sent backwards to an earlier stage. Many rows per
/// application, insert-only.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// This is the only backward transition in the system, so it is the only trace that an application
/// ever moved down rather than up. <c>Applications.CurrentStage</c> records where a file is, never
/// where it has been — without these rows a round trip through rework would be invisible.
///
/// Append-only, like <see cref="DocumentRemark"/>: there is no UpdatedAt because a row is never
/// rewritten.
/// </remarks>
public class SendBackLog
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    public int FromStage { get; set; }
    public int ToStage { get; set; }

    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Application? Application { get; set; }
}
