using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// How the sanctioned money actually leaves the building. One row per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// There is deliberately no stored disbursement *amount*. It is
/// <c>ApprovalDecision.SanctionedAmount</c> minus the non-waived Stage 7 charges, computed on every
/// render — storing it would let it drift the moment a charge is edited or waived.
///
/// The three file paths live here rather than in tables of their own: each is a single optional
/// document hanging off this one row, and a table per file would buy nothing.
/// <see cref="GeneralDocFilePath"/> in particular backs a tab whose purpose is unconfirmed — see
/// OPEN-QUESTIONS-FOR-ARUN.md.
/// </remarks>
public class Disbursement
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>Direct to dealer / Direct to customer / Direct to seller.</summary>
    [MaxLength(40)]
    public string Type { get; set; } = string.Empty;

    [MaxLength(150)]
    public string BeneficiaryName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string BeneficiaryAccount { get; set; } = string.Empty;

    /// <summary>NEFT / RTGS / UTR Transfer / Cheque.</summary>
    [MaxLength(30)]
    public string PaymentMode { get; set; } = string.Empty;

    public DateOnly? ValueDate { get; set; }

    [MaxLength(60)]
    public string? Utr { get; set; }

    /// <summary>Anchors the repayment schedule's due dates.</summary>
    public DateOnly? FirstEmiDate { get; set; }

    [MaxLength(400)]
    public string? MemoFilePath { get; set; }

    [MaxLength(400)]
    public string? InsuranceFilePath { get; set; }

    /// <summary>Backs the Document Upload tab, whose purpose the brief itself questions.</summary>
    [MaxLength(400)]
    public string? GeneralDocFilePath { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
