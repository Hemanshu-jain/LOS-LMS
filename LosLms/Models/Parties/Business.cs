using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// The borrowing firm behind the application. One row per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// Overlaps <c>Parties.CustomerCategory</c> from Stage 1, which already records Proprietorship /
/// Partnership firm / Private limited / HUF against the applicant. Nothing keeps the two in sync —
/// recorded in OPEN-QUESTIONS-FOR-ARUN.md.
/// </remarks>
public class Business
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    [MaxLength(150)]
    public string FirmName { get; set; } = string.Empty;

    [MaxLength(60)]
    public string Constitution { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Gstin { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Vintage { get; set; } = string.Empty;

    public DateOnly? IncorpDate { get; set; }

    public decimal Turnover { get; set; }

    /// <summary>How the business earns — routes, counterparties, seasonality.</summary>
    [MaxLength(4000)]
    public string Narrative { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
