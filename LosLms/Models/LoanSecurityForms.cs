namespace LosLms.Models;

/// <summary>
/// Editable security-asset state for the Loan &amp; Security screen.
/// </summary>
/// <remarks>
/// Deliberately holds the vehicle block <em>and</em> the property block at once. The asset-type
/// toggle only changes which set is rendered, so an officer can switch back and forth without
/// losing anything they typed. Mutable class rather than a record because <c>@bind</c> needs
/// settable properties.
/// </remarks>
public sealed class SecurityForm
{
    public int Id { get; set; }

    /// <summary>'Vehicle' or 'Property'.</summary>
    public string AssetType { get; set; } = "Vehicle";

    // ---- Vehicle (12 fields) ----
    public string? MakeModel { get; set; }
    public string? MfgYear { get; set; }
    public string? RegNo { get; set; }
    public string? ChassisNo { get; set; }
    public string? EngineNo { get; set; }
    public string? InvoiceNo { get; set; }
    public DateOnly? InvoiceDate { get; set; }
    public decimal? InvoiceValue { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? PolicyNo { get; set; }
    public DateOnly? PolicyExpiry { get; set; }

    // ---- Property (8 fields) ----
    public string? PropertyType { get; set; }
    public string? PropertyAddress { get; set; }
    public decimal? Area { get; set; }
    public string? OwnershipType { get; set; }
    public string? SaleDeedNo { get; set; }
    public string? ValuationRefNo { get; set; }
    public string? EncumbranceRef { get; set; }

    /// <summary>Shared by both asset types — the schema stores only one.</summary>
    public decimal? AssessedValue { get; set; }

    public string? InvoiceFilePath { get; set; }
    public string? InsuranceFilePath { get; set; }

    public bool HasInvoiceFile => !string.IsNullOrWhiteSpace(InvoiceFilePath);

    public bool HasInsuranceFile => !string.IsNullOrWhiteSpace(InsuranceFilePath);

    private bool IsVehicleComplete =>
        Filled(MakeModel) && Filled(MfgYear) && Filled(RegNo) && Filled(ChassisNo)
        && Filled(EngineNo) && Filled(InvoiceNo) && InvoiceDate.HasValue && Filled(InvoiceValue)
        && Filled(InsuranceProvider) && Filled(PolicyNo) && PolicyExpiry.HasValue
        && Filled(AssessedValue);

    private bool IsPropertyComplete =>
        Filled(PropertyType) && Filled(PropertyAddress) && Filled(Area) && Filled(OwnershipType)
        && Filled(SaleDeedNo) && Filled(ValuationRefNo) && Filled(AssessedValue)
        && Filled(EncumbranceRef);

    /// <summary>Complete = the active asset type's fields are filled and both files are attached.</summary>
    public bool IsComplete =>
        (AssetType == "Vehicle" ? IsVehicleComplete : IsPropertyComplete)
        && HasInvoiceFile
        && HasInsuranceFile;

    private static bool Filled(string? value) => !string.IsNullOrWhiteSpace(value);

    // A zero amount is meaningless on this screen, so zero counts as "not yet entered" rather than
    // as a filled value. Matches the reference spec's isFilled heuristic.
    private static bool Filled(decimal? value) => value.HasValue && value.Value > 0;
}

/// <summary>One row of the reference table.</summary>
/// <remarks>
/// Rows have no stable identity in the UI, which is why a save replaces every reference row for the
/// application rather than attempting to diff them.
/// </remarks>
public sealed class ReferenceForm
{
    public string? Name { get; set; }
    public string? Relationship { get; set; }
    public string? Mobile { get; set; }
    public string? Address { get; set; }
    public string? KnownSince { get; set; }

    /// <summary>A reference counts towards the required two once it has a name and a mobile.</summary>
    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Mobile);

    public bool HasAnyData =>
        !string.IsNullOrWhiteSpace(Name)
        || !string.IsNullOrWhiteSpace(Relationship)
        || !string.IsNullOrWhiteSpace(Mobile)
        || !string.IsNullOrWhiteSpace(Address)
        || !string.IsNullOrWhiteSpace(KnownSince);
}
