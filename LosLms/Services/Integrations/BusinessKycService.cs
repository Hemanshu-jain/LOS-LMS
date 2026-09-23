using LosLms.Models;

namespace LosLms.Services;

/// <summary>What a Business-KYC / ROC lookup would return for a firm's identifiers.</summary>
public sealed record BusinessKycData(
    string? LegalName,
    string? Status,
    DateOnly? IncorporationDate,
    string? RegisteredAddress);

/// <summary>Outcome of a company / ROC verification attempt.</summary>
/// <param name="IsConfigured">False whenever no provider is wired — the only state reachable here.</param>
/// <param name="Data">The fetched company record, present only on a real success. Always null in this build.</param>
public sealed record BusinessKycResult(bool IsConfigured, BusinessKycData? Data)
{
    public static readonly BusinessKycResult NotConfigured = new(false, null);
}

/// <summary>
/// Company / ROC verification (item #12): honest stub for the lookup, real write-back for the success
/// path — the same shape as <see cref="DigiKycService"/>.
/// </summary>
/// <remarks>
/// No Business-KYC provider is configured, so <see cref="VerifyAsync"/> always returns
/// <see cref="BusinessKycResult.NotConfigured"/> and the Approvals firm details stay whatever the
/// officer (or the Company profile pre-fill, #14) supplied. When Digio Business KYC is onboarded, make
/// the real call at the marked boundary — CIN / DIN / GSTIN against MCA/ROC — parse the response into a
/// <see cref="BusinessKycData"/>, and hand it to <see cref="Apply"/>. It never invents a company.
/// </remarks>
public static class BusinessKycService
{
    /// <summary>
    /// Attempts company/ROC verification from whatever identifiers the firm has (GSTIN, and CIN once the
    /// model carries one). Always "not configured" in this build.
    /// </summary>
    public static Task<BusinessKycResult> VerifyAsync(string? gstin, string? cin = null)
    {
        // REAL PROVIDER: call Digio Business KYC (Registrar of Companies — CIN/DIN/GSTIN) here, then
        // parse the response into a BusinessKycData and return new BusinessKycResult(true, data).
        return Task.FromResult(BusinessKycResult.NotConfigured);
    }

    /// <summary>
    /// Copies a verified company record onto the firm. No-op unless the result is a real success, so
    /// calling it after every VerifyAsync is safe and changes nothing while no provider is configured.
    /// Only fills blanks, so a manual entry (or the Company-profile pre-fill) is never overwritten.
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
