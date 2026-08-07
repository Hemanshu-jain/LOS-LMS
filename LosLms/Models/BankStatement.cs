using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// One uploaded bank statement period. Many rows per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// Statements are additive: a save inserts only rows that have no Id yet, so an unrelated save can
/// never disturb a file that is already on record. This is the opposite of how References behave on
/// Stage 2, where replace-all is correct because those rows carry no identity.
/// </remarks>
public class BankStatement
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>Free text as typed by the officer, e.g. "Apr 2026".</summary>
    [MaxLength(60)]
    public string? Period { get; set; }

    /// <summary>Server-generated path, stored outside wwwroot — statements are PII.</summary>
    [MaxLength(400)]
    public string? FilePath { get; set; }

    public DateTime UploadedOn { get; set; }

    /// <summary>
    /// Always <c>NotConfigured</c> in this build — no statement parser exists. The column is wide
    /// enough to hold real outcomes later without a migration.
    /// </summary>
    [MaxLength(30)]
    public string ParsedStatus { get; set; } = "NotConfigured";

    public DateTime CreatedAt { get; set; }

    public Application? Application { get; set; }
}
