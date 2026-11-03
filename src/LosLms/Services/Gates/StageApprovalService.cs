using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// The Admin approval between Stage 2 (Loan &amp; Security) and Stage 3 (Verification). An application whose Stages 1 and 2 are done is
/// sent to an Admin automatically and waits at Stage 2; approval is what moves it to Stage 3. "Approved to proceed" is simply
/// <c>CurrentStage &gt;= 3</c>, so every application already past Stage 2 when this was introduced is treated as approved, and nothing
/// else needs migrating.
/// </summary>
public static class StageApprovalService
{
    public const int VerificationStage = 3;

    public const string WaitingMessage = "Waiting for Admin approval. Checks with outside services run after an Admin approves this application.";

    public static async Task<bool> IsApprovedAsync(LosDbContext db, string applicationId) =>
        await db.Applications.IgnoreQueryFilters().AsNoTracking().Where(a => a.Id == applicationId).Select(a => a.CurrentStage).FirstOrDefaultAsync() >= VerificationStage;

    public enum Outcome { Approved, Pending, AlreadyWaiting }

    /// <summary>
    /// Stage 2 is complete. An Admin completing it needs no one else (the approval is recorded as given by them); anyone else raises the
    /// request and the file stays at Stage 2. Never moves an application backwards.
    /// </summary>
    public static async Task<Outcome> SubmitAsync(LosDbContext db, AdminRequestNotifier notifier, string applicationId, string? userId, string? reason)
    {
        var application = await db.Applications.FirstAsync(a => a.Id == applicationId);
        if (application.CurrentStage >= VerificationStage)
        {
            return Outcome.Approved;
        }

        if (db.IsElevatedUser)
        {
            application.CurrentStage = VerificationStage;
            db.AdminRequests.Add(new AdminRequest
            {
                ApplicationId = applicationId, RequestType = AdminRequest.ProceedApproval, Status = AdminRequest.Approved,
                RequestedByUserId = userId, ReviewedByUserId = userId, RequestedAt = DateTime.UtcNow, ReviewedAt = DateTime.UtcNow,
                RequestReason = reason, ReviewNote = "Stage 2 completed by an Admin.",
            });
            await db.SaveChangesAsync();
            return Outcome.Approved;
        }

        return await GateCheckService.RaiseRequestAsync(db, notifier, applicationId, AdminRequest.ProceedApproval, userId, reason)
            ? Outcome.Pending
            : Outcome.AlreadyWaiting;
    }

    /// <summary>
    /// Re-sends or re-raises the approval request after denial or while pending.
    /// If a request is already pending, nudges it to the top of the Admin Inbox.
    /// If the request was denied, raises a new pending request marked as re-triggered (see <see cref="PendingApprovals.RetriggerCountsAsync"/>).
    /// </summary>
    public static async Task<Outcome> ResendAsync(LosDbContext db, AdminRequestNotifier notifier, string applicationId, string? userId, string? reason = null)
    {
        var application = await db.Applications.FirstAsync(a => a.Id == applicationId);
        if (application.CurrentStage >= VerificationStage)
        {
            return Outcome.Approved;
        }

        var isPending = await db.AdminRequests.AnyAsync(r => r.ApplicationId == applicationId
            && r.RequestType == AdminRequest.ProceedApproval
            && r.Status == AdminRequest.Pending);

        if (isPending)
        {
            await GateCheckService.ResendRequestAsync(db, notifier, applicationId, AdminRequest.ProceedApproval, userId);
            return Outcome.Pending;
        }

        var fullReason = string.IsNullOrWhiteSpace(reason)
            ? "[Re-triggered] Stages 1 and 2 re-submitted for approval."
            : (reason.StartsWith("[Re-triggered]", StringComparison.OrdinalIgnoreCase) ? reason : $"[Re-triggered] {reason}");

        return await GateCheckService.RaiseRequestAsync(db, notifier, applicationId, AdminRequest.ProceedApproval, userId, fullReason)
            ? Outcome.Pending
            : Outcome.AlreadyWaiting;
    }

    /// <summary>An Admin approved the request: the application moves to Verification.</summary>
    public static async Task ApplyApprovalAsync(LosDbContext db, string applicationId)
    {
        var application = await db.Applications.FirstAsync(a => a.Id == applicationId);
        application.CurrentStage = Math.Max(application.CurrentStage, VerificationStage);
        await db.SaveChangesAsync();
    }

    /// <summary>(waiting, denial note) for the banner on Stage 2.</summary>
    public static async Task<(bool Waiting, string? DeniedNote)> StateAsync(LosDbContext db, string applicationId)
    {
        var (pending, _, denied) = await GateCheckService.LatestRequestStateAsync(db, applicationId, AdminRequest.ProceedApproval);
        return (pending, denied);
    }
}
