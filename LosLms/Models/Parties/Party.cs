using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// KYC record for one party on an application. One row per party type per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// Each party type gets its own row deliberately. The visual spec draws all three tabs from one
/// shared field set because it is a mockup; building it that way for real would silently overwrite
/// one party's KYC with another's.
/// </remarks>
public class Party
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>One of: Applicant, CoApplicant, Guarantor.</summary>
    [MaxLength(20)]
    public string PartyType { get; set; } = string.Empty;

    // ---- Personal ----
    [MaxLength(200)] public string? FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    [MaxLength(20)] public string? Gender { get; set; }
    [MaxLength(20)] public string? MaritalStatus { get; set; }
    [MaxLength(200)] public string? FatherSpouseName { get; set; }
    [MaxLength(60)] public string? CustomerCategory { get; set; }
    [MaxLength(40)] public string? Nationality { get; set; }
    [MaxLength(40)] public string? MotherTongue { get; set; }

    // ---- Identity ----
    [MaxLength(10)] public string? Pan { get; set; }
    [MaxLength(12)] public string? Aadhaar { get; set; }

    // These stay false in this build: no verification provider is configured. They are only ever
    // set true by a real verification result, never optimistically by the UI.
    public bool PanVerified { get; set; }
    public bool AadhaarVerified { get; set; }
    public bool MobileVerified { get; set; }

    // Server-generated paths, stored outside wwwroot — these are PII scans.
    [MaxLength(400)] public string? PhotoPath { get; set; }
    [MaxLength(400)] public string? PanScanPath { get; set; }
    [MaxLength(400)] public string? AadhaarScanPath { get; set; }

    // ---- Contact ----
    [MaxLength(10)] public string? Mobile { get; set; }
    [MaxLength(10)] public string? AltMobile { get; set; }
    [MaxLength(200)] public string? Email { get; set; }
    [MaxLength(200)] public string? Address1 { get; set; }
    [MaxLength(200)] public string? Address2 { get; set; }
    [MaxLength(100)] public string? City { get; set; }
    [MaxLength(60)] public string? State { get; set; }
    [MaxLength(6)] public string? PinCode { get; set; }
    [MaxLength(40)] public string? ResidenceType { get; set; }
    public int? YearsAtAddress { get; set; }

    // ---- Employment ----
    [MaxLength(40)] public string? EmploymentType { get; set; }
    [MaxLength(200)] public string? EmployerName { get; set; }
    [MaxLength(200)] public string? Designation { get; set; }
    [MaxLength(400)] public string? OfficeAddress { get; set; }
    public decimal? MonthlyIncome { get; set; }
    public int? YearsInJob { get; set; }

    /// <summary>One of: NotRun, Pass, Fail. Set by the dedupe check that runs on every save.</summary>
    [MaxLength(20)]
    public string DedupeStatus { get; set; } = "NotRun";

    // ---- Bureau ----

    /// <summary>
    /// NotChecked / Unavailable / Passed / Failed.
    /// </summary>
    /// <remarks>
    /// Only NotChecked and Unavailable are reachable in this build. No bureau is configured, so no
    /// code path may set Passed — a fabricated credit decision is the single worst thing this system
    /// could invent. Passed and Failed exist so a real provider can populate them later without a
    /// migration. Same discipline as <see cref="BankDetail.PennyDropStatus"/>.
    /// </remarks>
    [MaxLength(20)]
    public string CibilStatus { get; set; } = "NotChecked";

    /// <summary>Null until a real bureau returns one. Never invented.</summary>
    public int? CibilScore { get; set; }

    public DateTime? CibilCheckedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
