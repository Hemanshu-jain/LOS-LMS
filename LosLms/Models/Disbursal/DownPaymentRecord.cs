using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// The customer's own contribution, collected before disbursement. One row per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// Nothing reconciles <see cref="AmountReceived"/> against
/// <c>CamCostBreakdown.AppliedMargin</c> — the margin the CAM assumed the customer would put in.
/// The two are entered on different screens by different people and can disagree silently.
/// Recorded in OPEN-QUESTIONS-FOR-ARUN.md.
/// </remarks>
public class DownPaymentRecord
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    public decimal AmountReceived { get; set; }

    [MaxLength(60)]
    public string? ReceiptNo { get; set; }

    public DateOnly? ReceivedDate { get; set; }

    [MaxLength(400)]
    public string? ReceiptFilePath { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
