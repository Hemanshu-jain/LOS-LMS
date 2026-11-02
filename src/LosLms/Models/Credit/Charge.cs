using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// A fee recovered from the borrower. Many rows per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// <see cref="Locked"/> is true only for the Processing fee row, whose amount is pulled from
/// <c>Applications.ProcessingFee</c> (Stage 2) and is read-only here — one source of truth rather
/// than a second figure that can drift.
///
/// Waiving is a first-class state rather than "type zero": a waived row keeps its original amount
/// on record, carries a reason, and contributes nothing to the totals.
/// </remarks>
public class Charge
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Head { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Basis { get; set; } = string.Empty;

    [MaxLength(40)]
    public string DeductedFrom { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public decimal Gst { get; set; }

    /// <summary>True only for the Processing fee row, which is pulled and not editable.</summary>
    public bool Locked { get; set; }

    public bool Waived { get; set; }

    [MaxLength(300)]
    public string? WaiveReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
