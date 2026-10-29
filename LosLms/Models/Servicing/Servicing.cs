using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// The minimal servicing ledger (docs/digio/03 section 1). Opened ONCE when funds are released, from the sanctioned values.
/// DPD, overdue and outstanding are computed from the instalments and receipts, never stored.
/// </summary>
public class LoanAccount
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    /// <summary>One loan account per application.</summary>
    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>The number customers and staff quote; today the application id (LN-2026-005002).</summary>
    [MaxLength(30)]
    public string LoanAccountNo { get; set; } = string.Empty;

    public decimal Principal { get; set; }

    public decimal Roi { get; set; }

    public int Tenure { get; set; }

    public DateOnly FirstEmiDate { get; set; }

    /// <summary>The day of month the EMI falls on.</summary>
    public int EmiDay { get; set; }

    /// <summary>Active, Closed, Foreclosed or WrittenOff.</summary>
    [MaxLength(15)]
    public string Status { get; set; } = "Active";

    /// <summary><see cref="EnachMandate.Id"/> of the EMI mandate (its Digio state is read live); null when none was set up at release.</summary>
    public int? MandateId { get; set; }

    public DateTime OpenedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public List<Instalment> Instalments { get; set; } = new();
}

/// <summary>One scheduled EMI: a snapshot of LoanMath.Schedule at disbursal (first-EMI override and rounding included), never recomputed.</summary>
public class Instalment
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public int LoanAccountId { get; set; }

    /// <summary>1-based instalment number.</summary>
    public int No { get; set; }

    public DateOnly DueDate { get; set; }

    public decimal Emi { get; set; }

    public decimal Principal { get; set; }

    public decimal Interest { get; set; }

    /// <summary>Due, Presented, Paid, Bounced, PartPaid or Waived. Paid only ever comes from a Receipt (ReceiptService).</summary>
    [MaxLength(10)]
    public string Status { get; set; } = "Due";

    public decimal PaidAmount { get; set; }

    public DateOnly? PaidOn { get; set; }

    public LoanAccount? LoanAccount { get; set; }
}

/// <summary>One debit attempt at Digio. <see cref="Id"/> is OUR id and is sent to Digio as unique_key (the idempotency key).</summary>
public class Presentation
{
    /// <summary>32-char Guid "N".</summary>
    [Key]
    [MaxLength(32)]
    public string Id { get; set; } = string.Empty;

    public int CompanyId { get; set; }

    public int InstalmentId { get; set; }

    /// <summary>1, 2... (re-presentations count up).</summary>
    public int Attempt { get; set; } = 1;

    [MaxLength(40)]
    public string? Umrn { get; set; }

    public decimal Amount { get; set; }

    public DateOnly SettlementDate { get; set; }

    /// <summary>Digio's debit plan id.</summary>
    [MaxLength(80)]
    public string? DigioPlanId { get; set; }

    [MaxLength(80)]
    public string? DigioTxnId { get; set; }

    /// <summary>Created, Sent, InProgress, Succeeded, Failed or Cancelled.</summary>
    [MaxLength(12)]
    public string Status { get; set; } = "Created";

    [MaxLength(40)]
    public string? FailureCode { get; set; }

    [MaxLength(300)]
    public string? FailureReason { get; set; }

    [MaxLength(40)]
    public string? Utr { get; set; }

    [MaxLength(12)]
    public string Environment { get; set; } = "Sandbox";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>Money received. The only thing that marks an instalment paid.</summary>
public class Receipt
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public int LoanAccountId { get; set; }

    public int? InstalmentId { get; set; }

    /// <summary>{CompanyCode}-RCPT-{yyyy}-{00001}; unique per company.</summary>
    [MaxLength(30)]
    public string ReceiptNo { get; set; } = string.Empty;

    /// <summary>NACH, UPI, Manual, Cash or Cheque.</summary>
    [MaxLength(10)]
    public string Source { get; set; } = "NACH";

    public decimal Amount { get; set; }

    public DateOnly ReceivedOn { get; set; }

    /// <summary>The bank's UTR / cheque number.</summary>
    [MaxLength(60)]
    public string? Reference { get; set; }

    [MaxLength(32)]
    public string? PresentationId { get; set; }

    /// <summary>Approved, or Pending until an Admin approves a manual cash/cheque receipt. Only Approved receipts count.</summary>
    [MaxLength(10)]
    public string Status { get; set; } = "Approved";

    [MaxLength(450)]
    public string? RecordedByUserId { get; set; }

    [MaxLength(450)]
    public string? ApprovedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>IFileStore key of the receipt PDF, once generated.</summary>
    [MaxLength(400)]
    public string? PdfPath { get; set; }
}

/// <summary>A charge on the loan (bounce charge, slice 3). Separate from the Stage 7 Charge table, which belongs to the application.</summary>
public class LoanCharge
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public int LoanAccountId { get; set; }

    public int? InstalmentId { get; set; }

    /// <summary>Bounce, Other.</summary>
    [MaxLength(15)]
    public string Kind { get; set; } = "Bounce";

    public decimal Amount { get; set; }

    public decimal Gst { get; set; }

    /// <summary>Open, Paid or Waived.</summary>
    [MaxLength(10)]
    public string Status { get; set; } = "Open";

    [MaxLength(200)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
}
