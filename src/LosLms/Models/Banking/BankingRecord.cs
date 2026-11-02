using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// A reviewed bank account backing the officer's judgement. Many rows per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// Deliberately feeds nothing. Per the brief, banking conduct is context for the officer, not an
/// input to the eligible amount — the Eligibility screen says so on the tab itself. If that ever
/// changes, the only code that needs to know is the eligibility calculation.
/// </remarks>
public class BankingRecord
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>Applicant / CoApplicant / Guarantor.</summary>
    [MaxLength(20)]
    public string PartyType { get; set; } = "Applicant";

    [MaxLength(80)]
    public string Bank { get; set; } = string.Empty;

    public int Months { get; set; }
    public decimal AvgBalance { get; set; }
    public int Bounces { get; set; }
    public decimal InwardPct { get; set; }
    public decimal OutwardPct { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
