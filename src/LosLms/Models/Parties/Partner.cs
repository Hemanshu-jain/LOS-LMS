using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// A partner or director of the borrowing firm. Many rows per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// Separate from <see cref="Party"/>, which models the applicant / co-applicant / guarantor on the
/// loan. A partner here is an owner of the firm and is not necessarily a borrower.
/// </remarks>
public class Partner
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Role { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Pan { get; set; } = string.Empty;

    [MaxLength(15)]
    public string Contact { get; set; } = string.Empty;

    public decimal Shareholding { get; set; }

    public DateOnly? Dob { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
