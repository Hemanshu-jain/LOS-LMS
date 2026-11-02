using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// One round of the vendor's RCU report. Many rows per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// Versioning is just <see cref="SequenceNumber"/>: the highest is the current report and everything
/// below it is history. Re-submitting inserts a new row, which makes the previous one historical
/// automatically — there is no archive flag to keep in sync.
///
/// A re-submission starts with no file, so a row with a null <see cref="FilePath"/> legitimately
/// means "awaiting the vendor's upload" rather than a broken record.
/// </remarks>
public class RcuReport
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>1, 2, 3… in order of creation. The highest is the current report.</summary>
    public int SequenceNumber { get; set; }

    [MaxLength(400)]
    public string? FilePath { get; set; }

    public DateTime? UploadedAt { get; set; }

    [MaxLength(300)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public Application? Application { get; set; }
}
