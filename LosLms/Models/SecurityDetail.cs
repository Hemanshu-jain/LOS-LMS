using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// The asset securing an application. One row per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — reconciled during the cross-screen review once all nine screens exist.
///
/// Vehicle and property columns live side by side on one row and only one set is active at a time,
/// governed by <see cref="AssetType"/>. <see cref="AssessedValue"/> is deliberately shared between
/// the two, per the brief, which means a vehicle valuation and a property valuation cannot both be
/// stored for the same application — the second overwrites the first.
/// </remarks>
public class SecurityDetail
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>'Vehicle' or 'Property'.</summary>
    [MaxLength(20)]
    public string AssetType { get; set; } = "Vehicle";

    // ---- Vehicle ----
    [MaxLength(200)] public string? MakeModel { get; set; }
    [MaxLength(4)] public string? MfgYear { get; set; }
    [MaxLength(30)] public string? RegNo { get; set; }
    [MaxLength(40)] public string? ChassisNo { get; set; }
    [MaxLength(40)] public string? EngineNo { get; set; }
    [MaxLength(60)] public string? InvoiceNo { get; set; }
    public DateOnly? InvoiceDate { get; set; }
    public decimal? InvoiceValue { get; set; }
    [MaxLength(100)] public string? InsuranceProvider { get; set; }
    [MaxLength(60)] public string? PolicyNo { get; set; }
    public DateOnly? PolicyExpiry { get; set; }

    // ---- Property ----
    [MaxLength(60)] public string? PropertyType { get; set; }
    [MaxLength(400)] public string? PropertyAddress { get; set; }
    public decimal? Area { get; set; }
    [MaxLength(40)] public string? OwnershipType { get; set; }
    [MaxLength(80)] public string? SaleDeedNo { get; set; }
    [MaxLength(80)] public string? ValuationRefNo { get; set; }
    [MaxLength(80)] public string? EncumbranceRef { get; set; }

    /// <summary>Shared by both asset types — only one is active at a time.</summary>
    public decimal? AssessedValue { get; set; }

    // Server-generated paths, stored outside wwwroot.
    [MaxLength(400)] public string? InvoiceFilePath { get; set; }
    [MaxLength(400)] public string? InsuranceFilePath { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
