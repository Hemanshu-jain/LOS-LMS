using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>Where one check stands. <see cref="Locked"/> = done and no re-check allowed (the button is disabled); <see cref="GrantOpen"/> = an Admin allowed one more run.</summary>
public sealed record LockStatus(bool HasResult, DateTime? VerifiedOn, bool GrantOpen, string? Verdict = null)
{
    public bool Locked => HasResult && !GrantOpen;

    /// <summary>"Verified on 29 Oct 2026" when locked (or "Checked on ..." when the verdict was a mismatch: it was not verified), else null (the button's disabled reason).</summary>
    public string? Reason => Locked
        ? (Verdict is "Mismatch" or "Failed" or "AcceptedMismatch" ? "Checked on " : "Verified on ") + (VerifiedOn is { } d ? d.ToLocalTime().ToString("d MMM yyyy") : "an earlier date")
        : null;

    public static readonly LockStatus Open = new(false, null, false);
}

/// <summary>
/// The owner's rule: no accidental API over-use. Once a check has a result on file (the newest evidence for that subject is Verified, Mismatch
/// or AcceptedMismatch) it cannot be run again until an Admin allows ONE re-check with a reason. Every verify method asks
/// <see cref="TryBeginAsync(LosDbContext,int,string,int?,string)"/> before it sends anything to Digio; the buttons only mirror <see cref="StatusAsync"/>.
/// </summary>
public static class VerificationLock
{
    public const string Refusal = "Already verified. Ask an Admin to allow a re-check.";

    private static readonly string[] Counts = ["Verified", "Mismatch", "AcceptedMismatch"];

    public static async Task<LockStatus> StatusAsync(LosDbContext db, int companyId, string applicationId, int? partyId, string subject)
    {
        var evidence = await db.VerificationEvidence.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.ApplicationId == applicationId && e.PartyId == partyId && e.Subject == subject && Counts.Contains(e.Verdict))
            .OrderByDescending(e => e.Id).Select(e => new { e.CreatedAt, e.Verdict }).FirstOrDefaultAsync();
        DateTime? when = evidence?.CreatedAt;
        var verdict = evidence?.Verdict;

        // The penny drop predates evidence: an existing Matched / NotMatched on the bank row counts as done.
        if (evidence is null && subject == "BANK_ACCOUNT")
        {
            var bank = await db.BankDetails.IgnoreQueryFilters().AsNoTracking()
                .Where(b => b.ApplicationId == applicationId && (b.PennyDropStatus == "Matched" || b.PennyDropStatus == "NotMatched"))
                .Select(b => new { b.PennyDropCheckedAt, b.PennyDropStatus }).FirstOrDefaultAsync();
            if (bank is not null)
            {
                when = bank.PennyDropCheckedAt ?? DateTime.UtcNow;
                verdict = bank.PennyDropStatus == "Matched" ? "Verified" : "Mismatch";
            }
        }

        if (when is null)
        {
            return LockStatus.Open;
        }

        var open = await db.VerificationRechecks.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(g => g.CompanyId == companyId && g.ApplicationId == applicationId && g.PartyId == partyId && g.Subject == subject && g.UsedAt == null);
        return new(true, when, open, verdict);
    }

    /// <summary>
    /// True when the run may go ahead: nothing on file yet, or an Admin's grant was used up right now (atomically, so two clicks cannot share
    /// one grant). Call it after the cheap input checks and any "same inputs, answer already on file" shortcut, immediately before the Digio call.
    /// </summary>
    public static async Task<bool> TryBeginAsync(LosDbContext db, int companyId, string applicationId, int? partyId, string subject)
    {
        if (!(await StatusAsync(db, companyId, applicationId, partyId, subject)).HasResult)
        {
            return true;
        }

        var grant = await db.VerificationRechecks.IgnoreQueryFilters().AsNoTracking()
            .Where(g => g.CompanyId == companyId && g.ApplicationId == applicationId && g.PartyId == partyId && g.Subject == subject && g.UsedAt == null)
            .OrderBy(g => g.Id).Select(g => (long?)g.Id).FirstOrDefaultAsync();
        return grant is { } id
            && await db.VerificationRechecks.IgnoreQueryFilters().Where(g => g.Id == id && g.UsedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(g => g.UsedAt, DateTime.UtcNow)) == 1;
    }

    public static async Task<bool> TryBeginAsync(IDbContextFactory<LosDbContext> factory, int companyId, string applicationId, int? partyId, string subject)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await TryBeginAsync(db, companyId, applicationId, partyId, subject);
    }

    /// <summary>
    /// An Admin / SuperAdmin allows one re-check, with a reason. Returns null when granted, else why not. Only a check that is actually
    /// locked can be granted, and only one open grant at a time.
    /// </summary>
    public static async Task<string?> GrantAsync(LosDbContext db, int companyId, string applicationId, int? partyId, string subject, string? reason, string adminUserId, string? adminName)
    {
        if (!db.IsElevatedUser)
        {
            return "Only an Admin can allow a re-check.";
        }

        var why = reason?.Trim() ?? "";
        if (why.Length < 5)
        {
            return "Give a reason (at least a few words).";
        }

        var status = await StatusAsync(db, companyId, applicationId, partyId, subject);
        if (!status.HasResult)
        {
            return "Nothing has been verified yet, so there is nothing to re-check.";
        }

        if (status.GrantOpen)
        {
            return "A re-check is already allowed.";
        }

        db.VerificationRechecks.Add(new VerificationRecheck
        {
            CompanyId = companyId, ApplicationId = applicationId, PartyId = partyId, Subject = subject, Reason = why[..Math.Min(300, why.Length)],
            GrantedByUserId = adminUserId, GrantedByName = adminName is { Length: > 100 } ? adminName[..100] : adminName, GrantedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return null;
    }
}
