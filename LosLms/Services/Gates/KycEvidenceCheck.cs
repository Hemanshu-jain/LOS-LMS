using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// Stage 1's second gate, after CIBIL: for every party, the Digio checks the company switched on must have a current, passing verdict.
/// PAN needs the PAN check, Aadhaar needs DigiLocker, AML needs screening. Nothing switched on, nothing required.
/// Same shape as <see cref="VehicleCapCheck"/>: an Admin is never blocked; staff raise a KycBypass request that is scoped to the exact gap.
/// </summary>
public static class KycEvidenceCheck
{
    public static string Fingerprint(Party p) => DigioKyc.Fingerprint(p.Pan?.Trim().ToUpperInvariant(), p.FullName?.Trim().ToUpperInvariant(), p.DateOfBirth?.ToString("yyyy-MM-dd"));

    public static string AmlFingerprint(Party p) => DigioKyc.Fingerprint(p.FullName?.Trim().ToUpperInvariant(), p.Pan?.Trim().ToUpperInvariant());

    /// <param name="production">The company's Digio keys are production keys: sandbox evidence then does not count.</param>
    public static async Task<GateResult> GateAsync(LosDbContext db, string applicationId, bool production)
    {
        if (db.IsElevatedUser)
        {
            return GateResult.Clear;
        }

        var gaps = await GapsAsync(db, applicationId, production);
        if (gaps.Count == 0)
        {
            return GateResult.Clear;
        }

        var (pending, approved, deniedNote) = await GateCheckService.LatestRequestStateAsync(db, applicationId, AdminRequest.KycBypass, SubjectKeyFor(gaps));
        return approved
            ? GateResult.Clear
            : new GateResult(false, "Blocked — KYC checks missing: " + string.Join("; ", gaps) + ". Request admin bypass to continue.", AdminRequest.KycBypass, pending, deniedNote);
    }

    /// <summary>Scopes an approval to this exact set of gaps; a new gap needs a new approval.</summary>
    public static string SubjectKeyFor(IReadOnlyList<string> gaps) => DigioKyc.Fingerprint(gaps.ToArray())[..16];

    public static async Task<List<string>> GapsAsync(LosDbContext db, string applicationId, bool production)
    {
        var companyId = await db.Applications.AsNoTracking().Where(a => a.Id == applicationId).Select(a => a.CompanyId).FirstOrDefaultAsync();
        var settings = await db.DigioSettings.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(s => s.CompanyId == companyId);
        var gaps = new List<string>();
        if (settings is null || !(settings.PanVerify || settings.DigiLocker || settings.Aml))
        {
            return gaps;
        }

        var parties = await db.Parties.AsNoTracking().Where(p => p.ApplicationId == applicationId).ToListAsync();
        var evidence = await db.VerificationEvidence.AsNoTracking().Where(e => e.ApplicationId == applicationId).ToListAsync();

        bool Passes(Party p, string subject, string? fingerprint)
        {
            var latest = evidence.Where(e => e.PartyId == p.Id && e.Subject == subject).OrderByDescending(e => e.Id).FirstOrDefault();
            return latest is { Verdict: "Verified" or "AcceptedMismatch" }
                && !latest.IsStaleFor(fingerprint)
                && !(production && latest.Environment == "Sandbox");
        }

        foreach (var p in parties)
        {
            var who = string.IsNullOrWhiteSpace(p.FullName) ? p.PartyType : p.FullName;
            if (settings.PanVerify && !Passes(p, "PAN", Fingerprint(p))) { gaps.Add($"PAN ({who})"); }
            if (settings.DigiLocker && !Passes(p, "AADHAAR", null)) { gaps.Add($"Aadhaar ({who})"); }
            if (settings.Aml && !Passes(p, "AML", AmlFingerprint(p))) { gaps.Add($"AML screening ({who})"); }
        }

        return gaps;
    }
}
