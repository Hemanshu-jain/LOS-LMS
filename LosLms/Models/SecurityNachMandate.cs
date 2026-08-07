using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// The backup NACH mandate held against the guarantor. One row per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// <see cref="MandateHolder"/> is a plain name rather than a foreign key into <see cref="Party"/>,
/// so nothing checks that the named holder is actually this application's guarantor — the same
/// loose coupling already noted for <c>Applications.AssignedOfficer</c>.
/// </remarks>
public class SecurityNachMandate
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Umrn { get; set; }

    /// <summary>Pending / Registered / Rejected.</summary>
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [MaxLength(150)]
    public string? MandateHolder { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
