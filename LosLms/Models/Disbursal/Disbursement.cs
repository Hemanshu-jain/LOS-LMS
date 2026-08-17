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

    // ---- Disbursement account & trade advance ----

    /// <summary>
    /// Which of the lender's own accounts the money leaves from. Plain text for now
    /// (e.g. "HDFC Bank — Operations A/C ****1234"); a real account master is out of scope.
    /// </summary>
    [MaxLength(200)]
    public string? DisburseFromAccount { get; set; }

    /// <summary>
    /// Advance already paid to the dealer/seller before disbursal. Subtracted from the net
    /// disbursement amount, which is itself computed on every render and never stored.
    /// </summary>
    public decimal? TradeAdvance { get; set; }

    // ---- EMI adjustment (Post Sanction) ----

    /// <summary>Officer's override for the first month's instalment; null = use the computed EMI.</summary>
    public decimal? FirstEmiOverride { get; set; }

    /// <summary>Round the regular instalments to this nearest rupee figure (10 or 100); null = no rounding.</summary>
    public decimal? EmiRoundedTo { get; set; }

    // ---- E-agreement, welcome letter, welcome SMS ----
    //
    // The two documents are generated for real. Dispatch to an e-sign provider and an SMS gateway are
    // honest stubs — the status columns only ever leave their default until a real provider is wired,
    // and specifically are never set to 'Signed'/'Sent' by anything in this build.

    [MaxLength(400)]
    public string? AgreementFilePath { get; set; }

    /// <summary>NotSent / Sent / Signed. Only ever 'NotSent' in this build — no e-sign provider.</summary>
    [MaxLength(20)]
    public string AgreementEsignStatus { get; set; } = "NotSent";

    [MaxLength(400)]
    public string? WelcomeLetterFilePath { get; set; }

    /// <summary>NotSent / Sent. Only ever 'NotSent' in this build — no SMS gateway.</summary>
    [MaxLength(20)]
    public string WelcomeSmsStatus { get; set; } = "NotSent";

    public DateTime? WelcomeSmsSentAt { get; set; }

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
