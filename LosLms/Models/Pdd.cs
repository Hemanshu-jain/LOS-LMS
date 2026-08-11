using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// A post-disbursement document — something owed to the file after the money has already gone out.
/// Many rows per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// Unlike <see cref="PostSanctionChecklist"/>, these do **not** gate anything: an application can be
/// disbursed with every PDD still Open, which is the whole point of the category. Nothing chases
/// them either — <see cref="ExpectedDate"/> can pass with no warning anywhere in the system.
/// Recorded in OPEN-QUESTIONS-FOR-ARUN.md.
///
/// <see cref="WaivedByOfficerId"/> is a real foreign key rather than a typed name, so a waiver
/// always points at an officer who exists — the same audit discipline used for
/// <see cref="RcuInitiation.OverrideApproverOfficerId"/>.
/// </remarks>
public class Pdd
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Item { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Responsible { get; set; } = string.Empty;

    public DateOnly? ExpectedDate { get; set; }

    /// <summary>Open / Received / Waived.</summary>
    [MaxLength(20)]
    public string Status { get; set; } = "Open";

    /// <summary>Foreign key into <see cref="ApplicationUser"/> — the officer who waived the item.</summary>
    public string? WaivedByOfficerId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser? WaivedBy { get; set; }

    public Application? Application { get; set; }
}
