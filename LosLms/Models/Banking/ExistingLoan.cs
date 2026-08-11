using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// An obligation the borrower already carries elsewhere. Many rows per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// <see cref="Emi"/> is the only column that feeds the eligibility calculation: the sum across every
/// row is subtracted from the FOIR allowance before the new loan is sized. Everything else on the
/// row is underwriting context.
/// </remarks>
public class ExistingLoan
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>Applicant / CoApplicant / Guarantor.</summary>
    [MaxLength(20)]
    public string PartyType { get; set; } = "Applicant";

    [MaxLength(80)]
    public string Lender { get; set; } = string.Empty;

    [MaxLength(60)]
    public string LoanType { get; set; } = string.Empty;

    public decimal Sanctioned { get; set; }
    public decimal Pos { get; set; }
    public decimal Emi { get; set; }
    public decimal Roi { get; set; }

    public int MaxDpd { get; set; }
    public int Bounces { get; set; }

    /// <summary>Repayment track record: Regular / Irregular / Overdue.</summary>
    [MaxLength(20)]
    public string Rtr { get; set; } = "Regular";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
