using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// One record of an application being rejected. Many rows per application, insert-only.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// An application is rejected once in practice, but the table supports history regardless — the same
/// append-only shape as <see cref="SendBackLog"/>. <see cref="StageAtRejection"/> captures where the
/// file was when it was killed, because <c>Applications.CurrentStage</c> is deliberately left
/// untouched by a rejection: rejecting does not move the file, it stops it.
///
/// No UpdatedAt — a rejection is a point in time, never rewritten.
/// </remarks>
public class RejectionLog
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>The <c>CurrentStage</c> value at the moment of rejection.</summary>
    public int StageAtRejection { get; set; }

    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    public DateTime RejectedAt { get; set; }

    public Application? Application { get; set; }
}
