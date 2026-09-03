using LosLms.Models;

namespace LosLms.Services;

/// <summary>The identity a KYC provider would return for a PAN + Aadhaar pair.</summary>
public sealed record DigiKycData(
    string? FullName,
    DateOnly? DateOfBirth,
    string? Gender,
    string? FatherSpouseName,
    string? Address1,
    string? City,
    string? State,
    string? PinCode);

/// <summary>Outcome of a KYC verification attempt.</summary>
/// <param name="IsConfigured">False whenever no provider is wired — the only state reachable here.</param>
/// <param name="Data">The fetched identity, present only on a real success. Always null in this build.</param>
public sealed record DigiKycResult(bool IsConfigured, DigiKycData? Data)
{
    public static readonly DigiKycResult NotConfigured = new(false, null);
}

/// <summary>
/// PAN/Aadhaar identity verification: honest stub for the fetch, real write-back for the success path.
/// </summary>
/// <remarks>
/// No DigiKYC provider is configured, so <see cref="VerifyAsync"/> always returns
/// <see cref="DigiKycResult.NotConfigured"/> and Customer Details keeps showing the same
/// "API key not configured" notice it always has — nothing about the Verify buttons changes for the
/// user. The success path is built but dormant: <see cref="Apply"/> populates the party's Personal and
/// Contact fields from a result, so wiring a provider later is a one-call change — fetch, parse the
/// response into a <see cref="DigiKycData"/> at the marked boundary, and hand the result to
/// <see cref="Apply"/>. It never invents an identity.
/// </remarks>
public static class DigiKycService
{
    /// <summary>
    /// Attempts verification. Always reports "not configured" in this build; a real implementation
    /// would call the provider here and return a configured result on success.
    /// </summary>
    public static Task<DigiKycResult> VerifyAsync(string? pan, string? aadhaar)
    {
        // REAL PROVIDER: call the DigiKYC endpoint with pan/aadhaar, then parse its response into a
        // DigiKycData and return new DigiKycResult(IsConfigured: true, data). Until then, stay honest.
        return Task.FromResult(DigiKycResult.NotConfigured);
    }

    /// <summary>
    /// Copies a verified identity onto the party form. No-op unless the result is a real success, so
    /// calling it after every VerifyAsync is safe and changes nothing while no provider is configured.
    /// Only fills blanks the officer has not already typed, so a manual entry is never overwritten.
    /// </summary>
    public static void Apply(PartyForm party, DigiKycResult result)
    {
        if (!result.IsConfigured || result.Data is not { } data)
        {
            return;
        }

        party.FullName = Prefer(party.FullName, data.FullName);
        party.DateOfBirth ??= data.DateOfBirth;
        party.Gender = Prefer(party.Gender, data.Gender);
        party.FatherSpouseName = Prefer(party.FatherSpouseName, data.FatherSpouseName);
        party.Address1 = Prefer(party.Address1, data.Address1);
        party.City = Prefer(party.City, data.City);
        party.State = Prefer(party.State, data.State);
        party.PinCode = Prefer(party.PinCode, data.PinCode);
    }

    private static string? Prefer(string? existing, string? fetched) =>
        string.IsNullOrWhiteSpace(existing) ? fetched : existing;
}
