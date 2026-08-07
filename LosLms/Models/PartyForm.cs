namespace LosLms.Models;

/// <summary>
/// Editable in-memory state for one party tab on the Customer Details screen.
/// </summary>
/// <remarks>
/// A mutable class rather than a record because Razor's <c>@bind</c> needs settable properties.
///
/// Three instances are held at once — one per party type — so switching tabs only changes which
/// one is rendered. Nothing is copied between them, and all three are written to the database in a
/// single save.
/// </remarks>
public sealed class PartyForm
{
    /// <summary>Primary key of the backing Parties row. Zero until the first save.</summary>
    public int Id { get; set; }

    public string PartyType { get; set; } = string.Empty;

    // ---- Personal (8 fields — all required for stage completion) ----
    public string? FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string? FatherSpouseName { get; set; }
    public string? CustomerCategory { get; set; }
    public string? Nationality { get; set; }
    public string? MotherTongue { get; set; }

    // ---- Identity ----
    public string? Pan { get; set; }
    public string? Aadhaar { get; set; }

    // ---- Contact (10 fields — all required for stage completion) ----
    public string? Mobile { get; set; }
    public string? AltMobile { get; set; }
    public string? Email { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PinCode { get; set; }
    public string? ResidenceType { get; set; }
    public int? YearsAtAddress { get; set; }

    // ---- Employment (never gates stage completion) ----
    public string? EmploymentType { get; set; }
    public string? EmployerName { get; set; }
    public string? Designation { get; set; }
    public decimal? MonthlyIncome { get; set; }
    public int? YearsInJob { get; set; }
    public string? OfficeAddress { get; set; }

    // ---- Persisted upload paths ----
    public string? PhotoPath { get; set; }
    public string? PanScanPath { get; set; }
    public string? AadhaarScanPath { get; set; }

    /// <summary>Data URL of the just-selected photo. Never persisted — preview only.</summary>
    public string? PhotoPreview { get; set; }

    // ---- Server-owned state ----
    public string DedupeStatus { get; set; } = "NotRun";
    public bool PanVerified { get; set; }
    public bool AadhaarVerified { get; set; }
    public bool MobileVerified { get; set; }

    public bool IsPersonalComplete =>
        !string.IsNullOrWhiteSpace(FullName)
        && DateOfBirth.HasValue
        && !string.IsNullOrWhiteSpace(Gender)
        && !string.IsNullOrWhiteSpace(MaritalStatus)
        && !string.IsNullOrWhiteSpace(FatherSpouseName)
        && !string.IsNullOrWhiteSpace(CustomerCategory)
        && !string.IsNullOrWhiteSpace(Nationality)
        && !string.IsNullOrWhiteSpace(MotherTongue);

    public bool IsContactComplete =>
        !string.IsNullOrWhiteSpace(Mobile)
        && !string.IsNullOrWhiteSpace(AltMobile)
        && !string.IsNullOrWhiteSpace(Email)
        && !string.IsNullOrWhiteSpace(Address1)
        && !string.IsNullOrWhiteSpace(Address2)
        && !string.IsNullOrWhiteSpace(City)
        && !string.IsNullOrWhiteSpace(State)
        && !string.IsNullOrWhiteSpace(PinCode)
        && !string.IsNullOrWhiteSpace(ResidenceType)
        && YearsAtAddress.HasValue;

    public bool IsEmploymentComplete =>
        !string.IsNullOrWhiteSpace(EmploymentType)
        && !string.IsNullOrWhiteSpace(EmployerName)
        && !string.IsNullOrWhiteSpace(Designation)
        && MonthlyIncome.HasValue
        && YearsInJob.HasValue
        && !string.IsNullOrWhiteSpace(OfficeAddress);

    /// <summary>
    /// True once the officer has entered anything at all. A party with nothing entered is treated
    /// as not part of this application and never blocks stage completion.
    /// </summary>
    public bool HasAnyData =>
        IsAnySet(FullName, Gender, MaritalStatus, FatherSpouseName, CustomerCategory, Nationality,
            MotherTongue, Pan, Aadhaar, Mobile, AltMobile, Email, Address1, Address2, City, State,
            PinCode, ResidenceType, EmploymentType, EmployerName, Designation, OfficeAddress,
            PhotoPath, PanScanPath, AadhaarScanPath)
        || DateOfBirth.HasValue
        || YearsAtAddress.HasValue
        || MonthlyIncome.HasValue
        || YearsInJob.HasValue;

    private static bool IsAnySet(params string?[] values) =>
        values.Any(v => !string.IsNullOrWhiteSpace(v));
}
