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

    /// <summary>
    /// The structured rejection category for bureau reporting, chosen from a standard list at rejection
    /// time (e.g. "Low bureau score", "High existing obligations"). Nullable because historical rows
    /// pre-date it and a free-text <see cref="Reason"/> is always present. The concrete CIBIL reason
    /// codes replace this list once the client supplies them — see <see cref="RejectionCategories"/>.
    /// </summary>
    [MaxLength(60)]
    public string? RejectionCategory { get; set; }

    public DateTime RejectedAt { get; set; }

    public Application? Application { get; set; }
}

/// <summary>
/// The standard rejection categories offered at rejection time and used in the CIBIL export. A
/// starter list until the client supplies the exact bureau reason codes; swapping it changes only what
/// new rejections offer — existing rows keep the string they recorded.
/// </summary>
public static class RejectionCategories
{
    public static readonly string[] All =
    {
        "Low bureau score",
        "Adverse credit history (write-off / settlement / overdue)",
        "High existing obligations (FOIR)",
        "Insufficient income / eligibility",
        "Insufficient or adverse documentation",
        "Collateral / LTV shortfall",
        "RCU / field verification negative",
        "Policy norms not met",
        "Other",
    };
}
