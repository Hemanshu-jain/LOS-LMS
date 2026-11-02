using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// Priority-sector and co-lending classification for an application. One row per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// <see cref="PslSub"/> and <see cref="PrioritySectorAmount"/> are nullable because they only carry
/// meaning while <see cref="Psl"/> is the "Yes" value; the screen hides both fields otherwise. They
/// are deliberately not cleared when PSL flips back to No — an officer who toggles by accident
/// should not silently lose what they typed.
/// </remarks>
public class Classification
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Psl { get; set; } = PslNo;

    [MaxLength(40)]
    public string? PslSub { get; set; }

    public decimal RiskSharing { get; set; }

    [MaxLength(60)]
    public string CoLendingPartner { get; set; } = string.Empty;

    [MaxLength(60)]
    public string EndUse { get; set; } = string.Empty;

    public decimal? PrioritySectorAmount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }

    public const string PslYes = "Yes - Priority Sector";
    public const string PslNo = "No - Non-Priority Sector";
}
