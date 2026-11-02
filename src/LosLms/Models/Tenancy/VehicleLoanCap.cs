using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// The most a company will lend against one make, model and model year — and, incidentally, the
/// catalog that Loan &amp; Security's vehicle dropdowns are built from.
/// </summary>
/// <remarks>
/// Make and model used to be one freeform text box, so every operator spelled the same lorry a
/// different way and no cap could be attached to any of it. One row here does both jobs: it names a
/// vehicle the company will finance, and it says how far.
///
/// The year is part of the identity, not a label. A four-year-old truck is not worth what a new one
/// is, so the same model carries a different ceiling per year — that is the whole reason the column
/// exists.
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

    /// <summary>
    /// Model year. Part of the row's identity — the same model in two years is two catalog entries
    /// with two ceilings. Selecting it on Loan &amp; Security is what fills the application's
    /// manufacturing year, so an officer cannot record a year the company does not finance.
    /// </summary>
    public int Year { get; set; }

    /// <summary>Maximum loan amount sanctionable against this make, model and year.</summary>
    public decimal MaxLoanAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Company? Company { get; set; }
}
