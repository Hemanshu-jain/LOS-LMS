using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// Release funds needs the agreement Signed (and not changed since) and the EMI mandate Registered, for a company that has
/// those Digio products switched on (DECISIONS #8). Sandbox results never satisfy a production company. An approved
/// <see cref="AdminRequest.DigioReleaseBypass"/> clears it. It does NOT look at the caller's role: an Admin may release anyway, but the
/// page then logs that override (<see cref="LogOverrideAsync"/>), so "Admin bypass, logged" holds.
/// </summary>
public static class DigioReleaseGate
{
    public static async Task<GateResult> CheckAsync(LosDbContext db, string applicationId, bool companyProduction)
    {
        var companyId = await db.Applications.AsNoTracking().Where(a => a.Id == applicationId).Select(a => a.CompanyId).FirstOrDefaultAsync();
        var settings = await db.DigioSettings.AsNoTracking().FirstOrDefaultAsync(s => s.CompanyId == companyId);
        if (settings is null || !(settings.Esign || settings.Enach))
        {
            return GateResult.Clear;
        }

        var problems = new List<string>();
        if (settings.Esign)
        {
            var d = await db.Disbursements.AsNoTracking().Where(x => x.ApplicationId == applicationId)
                .Select(x => new { x.AgreementEsignStatus, x.AgreementHash, x.AgreementFingerprint, x.EsignRequestId }).FirstOrDefaultAsync();
            if (d?.AgreementEsignStatus != "Signed")
            {
                problems.Add("the loan agreement is not signed by every party");
            }
            else if (d.AgreementHash is not null && d.AgreementHash != d.AgreementFingerprint)
            {
                problems.Add("the agreement was changed after it was signed");
            }
            else if (companyProduction && await IsSandboxAsync(db, d.EsignRequestId))
            {
                problems.Add("the agreement was signed in Digio's test environment");
            }
        }

        if (settings.Enach)
        {
            var m = await db.EnachMandates.AsNoTracking().Where(x => x.ApplicationId == applicationId)
                .Select(x => new { x.DigioState, x.DigioRequestId }).FirstOrDefaultAsync();
            if (m?.DigioState != NachLifecycle.Registered)
            {
                problems.Add("the e-NACH mandate is not registered");
            }
            else if (companyProduction && await IsSandboxAsync(db, m.DigioRequestId))
            {
                problems.Add("the e-NACH mandate was registered in Digio's test environment");
            }
        }

        if (problems.Count == 0)
        {
            return GateResult.Clear;
        }

        var (pending, approved, denied) = await GateCheckService.LatestRequestStateAsync(db, applicationId, AdminRequest.DigioReleaseBypass);
        return approved
            ? GateResult.Clear
            : new GateResult(false, "Blocked: " + string.Join("; ", problems) + ".", AdminRequest.DigioReleaseBypass, pending, denied);
    }

    /// <summary>Records an Admin releasing past this gate: an already-approved request, so the Inbox history shows who did it and why.</summary>
    public static async Task LogOverrideAsync(LosDbContext db, string applicationId, string? userId, string reason)
    {
        db.AdminRequests.Add(new AdminRequest
        {
            ApplicationId = applicationId, RequestType = AdminRequest.DigioReleaseBypass, Status = AdminRequest.Approved,
            RequestedByUserId = userId, ReviewedByUserId = userId, RequestedAt = DateTime.UtcNow, ReviewedAt = DateTime.UtcNow,
            RequestReason = "Admin released funds past the Digio gate. " + reason, ReviewNote = "Admin override at release.",
        });
        await db.SaveChangesAsync();
    }

    private static async Task<bool> IsSandboxAsync(LosDbContext db, string? requestId) =>
        requestId is not null && await db.DigioRequests.AsNoTracking().AnyAsync(r => r.Id == requestId && r.Environment == "Sandbox");
}
