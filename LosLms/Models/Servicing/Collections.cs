using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>Work for a collections officer: a bounce that cannot be re-presented, or a mandate that broke. One open task per (instalment, kind).</summary>
public class CollectionTask
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    public int LoanAccountId { get; set; }

    public int? InstalmentId { get; set; }

    /// <summary>NonRpnReturn, AttemptsExhausted or BrokenMandate.</summary>
    [MaxLength(20)]
    public string Kind { get; set; } = string.Empty;

    /// <summary>The application's assigned officer when the task was raised.</summary>
    [MaxLength(450)]
    public string? AssignedOfficerId { get; set; }

    [MaxLength(300)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Open or Done.</summary>
    [MaxLength(8)]
    public string Status { get; set; } = "Open";

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    [MaxLength(450)]
    public string? ResolvedByUserId { get; set; }

    [MaxLength(300)]
    public string? ResolutionNote { get; set; }
}

/// <summary>One settlement report (a Digio CID) checked against our presentations. Mismatches show on the collections dashboard.</summary>
public class SettlementCheck
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    [MaxLength(80)]
    public string Cid { get; set; } = string.Empty;

    public DateTime CheckedAt { get; set; }

    public decimal DigioTotal { get; set; }

    public decimal OurTotal { get; set; }

    public int TxnCount { get; set; }

    public int MismatchCount { get; set; }

    /// <summary>Plain words, no PII: which transaction ids / amounts differ.</summary>
    [MaxLength(1000)]
    public string? Detail { get; set; }
}
