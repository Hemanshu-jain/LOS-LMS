using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// A loan record carried over from the client's previous system by bulk import. Deliberately kept
/// separate from <see cref="Application"/>: these are historical/reference rows — already-live loans —
/// that never enter the 8-stage origination flow. They exist so the client's back-book is searchable
/// in one place alongside new business.
/// </summary>
/// <remarks>Company-scoped by the same global query filter as every other tenant-owned table.</remarks>
public class MigratedLoan
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    /// <summary>The account/loan number from the old system — the client's own identifier for the loan.</summary>
    [MaxLength(60)]
    public string LoanAccountNo { get; set; } = string.Empty;

    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? Pan { get; set; }

    [MaxLength(15)]
    public string? Mobile { get; set; }

    [MaxLength(120)]
    public string? Product { get; set; }

    public decimal? SanctionedAmount { get; set; }

    public decimal? OutstandingAmount { get; set; }

    /// <summary>Free text from the old system (e.g. Live, Closed, NPA) — not tied to the app's own statuses.</summary>
    [MaxLength(40)]
    public string? Status { get; set; }

    public DateOnly? DisbursedOn { get; set; }

    [MaxLength(100)]
    public string? Branch { get; set; }

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    public Company? Company { get; set; }
}
