using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// Whether an application may advance, and if not, why and what can be done about it.
/// </summary>
/// <param name="CanAdvance">False blocks the stage's completion action outright.</param>
/// <param name="BlockedReason">Shown to the officer. Null when nothing is blocking.</param>
/// <param name="RequestType">Which admin request would unblock it, or null if none applies.</param>
/// <param name="RequestPending">A request of that type is already awaiting an Admin — do not offer to raise another.</param>
/// <param name="DeniedNote">The Admin's note on the most recent denial, so the officer learns why.</param>
public sealed record GateResult(
    bool CanAdvance,
    string? BlockedReason,
    string? RequestType,
    bool RequestPending,
    string? DeniedNote)
{
    public static readonly GateResult Clear = new(true, null, null, false, null);
}

/// <summary>
/// The one implementation of "can this application move on", called by every stage screen.
/// </summary>
/// <remarks>
/// Written once on purpose. Eight copies of a gate is eight chances for one screen to disagree with
/// the other seven about whether a file is blocked — and the one that disagrees is the one that lets
/// an unchecked application through.
///
/// Enforced in two places for two different reasons: <c>StageSubHeader</c> uses it to disable the
/// button and explain why, which is UX; each completion handler calls it again before doing any work,
/// which is the actual control. A disabled button is a hint, not a guarantee.
/// </remarks>
public static class GateCheckService
{
    public const string Blocked = "Blocked";
    public const string Passed = "Passed";
    public const string Bypassed = "Bypassed";

    public const string CibilBlockedMessage =
        "Blocked — CIBIL check required. Request admin bypass to continue.";

    /// <summary>
    /// The CIBIL gate, which applies to every stage from Customer Details through Release funds.
    /// </summary>
    public static async Task<GateResult> CanAdvanceAsync(LosDbContext db, string applicationId)
    {
        // An Admin (or SuperAdmin) has complete authority: no stage gate blocks them, and they never
        // raise a request for another Admin to approve. One guard here clears every server-side
        // completion handler and the sub-header at once, because they all route through this method.
        if (db.IsElevatedUser)
        {
            return GateResult.Clear;
        }

        var application = await db.Applications.AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => new { a.CibilGateStatus })
            .FirstOrDefaultAsync();

        if (application is null)
        {
            return GateResult.Clear;
        }

        if (application.CibilGateStatus is Passed or Bypassed)
        {
            return GateResult.Clear;
        }

        var (pending, _, deniedNote) =
            await LatestRequestStateAsync(db, applicationId, AdminRequest.CibilBypass);

        return new GateResult(
            CanAdvance: false,
            BlockedReason: CibilBlockedMessage,
            RequestType: AdminRequest.CibilBypass,
            RequestPending: pending,
            DeniedNote: deniedNote);
    }

    /// <summary>
    /// Recomputes the application's gate from its parties and saves it.
    /// </summary>
    /// <remarks>
    /// Passes only when EVERY party on the file has a bureau score inside the company's accepted
    /// range. One party still unchecked keeps the whole application blocked — a partial pass is not a
    /// pass, because the unchecked party is exactly the one nobody has looked at.
    ///
    /// A gate already Bypassed is left alone: an Admin has made a decision, and a later party edit
    /// must not silently undo it.
    /// </remarks>
    public static async Task RecomputeCibilGateAsync(LosDbContext db, string applicationId)
    {
        var application = await db.Applications.FirstOrDefaultAsync(a => a.Id == applicationId);
        if (application is null || application.CibilGateStatus == Bypassed)
        {
            return;
        }

        var policy = await CompanyPolicyReader.ForApplicationAsync(db, applicationId);

        var parties = await db.Parties.AsNoTracking()
            .Where(p => p.ApplicationId == applicationId)
            .Select(p => new { p.CibilStatus, p.CibilScore })
            .ToListAsync();

        // No parties at all is not a pass — there is nothing to have checked.
        var passes = parties.Count > 0
            && parties.All(p =>
                p.CibilStatus == "Passed"
                && p.CibilScore is { } score
                && score >= policy.CibilMinScore
                && score <= policy.CibilMaxScore);

        var next = passes ? Passed : Blocked;

        if (application.CibilGateStatus != next)
        {
            application.CibilGateStatus = next;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Most recent request of a type: whether one is pending, whether it was approved, and the note
    /// on the last denial.
    /// </summary>
    /// <param name="subjectKey">
    /// When given, only requests raised for that exact subject count. This is what stops an approved
    /// vehicle-cap bypass from silently carrying over to a different vehicle swapped in afterwards.
    /// </param>
    internal static async Task<(bool Pending, bool Approved, string? DeniedNote)> LatestRequestStateAsync(
        LosDbContext db, string applicationId, string requestType, string? subjectKey = null)
    {
        var query = db.AdminRequests.AsNoTracking()
            .Where(r => r.ApplicationId == applicationId && r.RequestType == requestType);

        if (subjectKey is not null)
        {
            query = query.Where(r => r.SubjectKey == subjectKey);
        }

        var latest = await query
            .OrderByDescending(r => r.Id)
            .Select(r => new { r.Status, r.ReviewNote })
            .FirstOrDefaultAsync();

        if (latest is null)
        {
            return (false, false, null);
        }

        return (
            latest.Status == AdminRequest.Pending,
            latest.Status == AdminRequest.Approved,
            latest.Status == AdminRequest.Denied ? latest.ReviewNote : null);
    }

    /// <summary>
    /// Raises a request, unless an identical one is already waiting. Returns false when it was a
    /// duplicate, so the caller can say so rather than silently doing nothing.
    /// </summary>
    public static async Task<bool> RaiseRequestAsync(
        LosDbContext db,
        AdminRequestNotifier notifier,
        string applicationId,
        string requestType,
        string? requestedByUserId,
        string? reason,
        string? subjectKey = null)
    {
        var alreadyPending = await db.AdminRequests
            .AnyAsync(r => r.ApplicationId == applicationId
                           && r.RequestType == requestType
                           && r.Status == AdminRequest.Pending);

        if (alreadyPending)
        {
            return false;
        }

        db.AdminRequests.Add(new AdminRequest
        {
            ApplicationId = applicationId,
            RequestType = requestType,
            Status = AdminRequest.Pending,
            RequestedByUserId = requestedByUserId,
            RequestedAt = DateTime.UtcNow,
            RequestReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            SubjectKey = subjectKey,
        });

        await db.SaveChangesAsync();

        // Company comes from the application, which is how the Inbox is scoped too.
        var companyId = await db.Applications.AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => a.CompanyId)
            .FirstOrDefaultAsync();

        await notifier.NotifyAsync(companyId);
        return true;
    }
}
