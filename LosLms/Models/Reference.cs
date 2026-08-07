using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// A personal reference supplied for an application. Many rows per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// The table is named <c>References</c>, which is a reserved word in MySQL. Pomelo quotes every
/// identifier with backticks so EF queries are safe, but any hand-written SQL against this table
/// must quote it as <c>`References`</c>.
///
/// Rows carry no identity in the UI, so a save deletes every row for the application and re-inserts
/// the current list rather than trying to diff them.
/// </remarks>
public class Reference
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    [MaxLength(200)] public string? Name { get; set; }
    [MaxLength(100)] public string? Relationship { get; set; }
    [MaxLength(10)] public string? Mobile { get; set; }
    [MaxLength(400)] public string? Address { get; set; }
    [MaxLength(60)] public string? KnownSince { get; set; }

    public DateTime CreatedAt { get; set; }

    public Application? Application { get; set; }
}
