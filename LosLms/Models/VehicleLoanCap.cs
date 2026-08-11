using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// The most a company will lend against one vehicle make and model — and, incidentally, the catalog
/// that Loan &amp; Security's make and model dropdowns are built from.
/// </summary>
/// <remarks>
/// Make and model used to be one freeform text box, so every operator spelled the same lorry a
/// different way and no cap could be attached to any of it. One row here does both jobs: it names a
/// vehicle the company will finance, and it says how far.
///
/// Company-scoped by global query filter, so a catalog is never visible or usable across tenants.
///
/// The cap is recorded but not yet enforced — enforcement on Loan &amp; Security's completion, with an
/// admin bypass, is the next phase. A row missing for a vehicle therefore means "not in the catalog",
/// not "unlimited".
/// </remarks>
public class VehicleLoanCap
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    [MaxLength(80)]
    public string Make { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Model { get; set; } = string.Empty;

    /// <summary>Maximum loan amount sanctionable against this make and model.</summary>
    public decimal MaxLoanAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Company? Company { get; set; }
}
