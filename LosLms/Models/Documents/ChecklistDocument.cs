using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// One checklist line: an application, a party, and one of the 13 document types.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — reconciled during the cross-screen review once all nine screens exist.
///
/// Named <c>ChecklistDocument</c> rather than <c>Document</c> even though the table is
/// <c>Documents</c>: a type called <c>Document</c> in this namespace would collide with QuestPDF's
/// <c>Document.Create</c> inside CamPdf.cs, which imports both namespaces.
///
/// For Pan, Aadhaar and Photo the file itself lives on the <see cref="Party"/> row
/// (PanScanPath / AadhaarScanPath / PhotoPath) and <see cref="FilePath"/> here stays null — those
/// three are read live from Parties so nothing is stored twice. Target date and remarks still hang
/// off this row for every type.
/// </remarks>
public class ChecklistDocument
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>Applicant / CoApplicant / Guarantor.</summary>
    [MaxLength(20)]
    public string PartyType { get; set; } = string.Empty;

    /// <summary>One of the 13 values in <c>DocumentChecklist.DocumentTypes</c>.</summary>
    [MaxLength(30)]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Unused for Pan / Aadhaar / Photo — see the remarks above.</summary>
    [MaxLength(400)]
    public string? FilePath { get; set; }

    public DateTime? UploadDate { get; set; }

    public DateOnly? TargetDate { get; set; }

    /// <summary>Only ever 90, only ever on Address, and only once Address is actually collected.</summary>
    public int? ValidityDays { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
