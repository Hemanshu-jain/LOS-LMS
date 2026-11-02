using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// One entry in the chase log against a checklist document.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// Append-only. Remarks are never edited and never deleted, including when the document is finally
/// collected — chasing a document is a sequence of contacts over time and that history is the
/// record of it. Once a document is Collected the log simply stops being displayed; the rows stay.
/// This deliberately differs from the reference spec's demo, which cleared the log on collect.
/// </remarks>
public class DocumentRemark
{
    public int Id { get; set; }

    public int DocumentId { get; set; }

    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    /// <summary>Set on insert. This is the date shown against each entry in the log.</summary>
    public DateTime CreatedAt { get; set; }

    public ChecklistDocument? Document { get; set; }
}
