using LosLms.Models;

namespace LosLms.Services;

/// <summary>What a Business-KYC / register lookup returned for a firm's identifiers.</summary>
public sealed record BusinessKycData(
    string? LegalName,
    string? Status,
    DateOnly? IncorporationDate,
    string? RegisteredAddress);

/// <summary>A company / register verification result: <see cref="IsConfigured"/> is true only when real data came back.</summary>
public sealed record BusinessKycResult(bool IsConfigured, BusinessKycData? Data);

/// <summary>
/// Write-back for a verified company record. The lookup itself is <see cref="DigioAsset.VerifyBusinessAsync"/>.
/// </summary>
public static class BusinessKycService
{
    /// <summary>
    /// Copies a verified company record onto the firm. No-op unless the result carries data. Only fills blanks, so a manual entry
    /// (or the Company-profile pre-fill) is never overwritten.
    /// </summary>
    public static void Apply(Business business, BusinessKycResult result)
    {
        if (!result.IsConfigured || result.Data is not { } data)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(business.FirmName) && !string.IsNullOrWhiteSpace(data.LegalName))
        {
            business.FirmName = data.LegalName!;
        }

        business.IncorpDate ??= data.IncorporationDate;
    }
}
