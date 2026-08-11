using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// Rejecting an application — now a two-step decision rather than one officer's click.
/// </summary>
/// <remarks>
/// Staff used to reject directly and irreversibly from any stage screen or the dashboard drawer. They
/// now raise a request; only an Admin approving it actually stops the file, and the rejection log is
/// written at that moment rather than at submission — so the log records the decision, not the ask.
///
/// <see cref="ApplyAsync"/> is the old behaviour, kept intact but with exactly one caller: the Admin
/// Inbox's approve action.
/// </remarks>
public static class RejectionService
{
    /// <summary>
    /// Raises a rejection request. The application's status is untouched until an Admin decides.
    /// Returns false when one is already pending.
    /// </summary>
    public static Task<bool> RequestAsync(
        LosDbContext db,
        AdminRequestNotifier notifier,
        string applicationId,
        string? requestedByUserId,
        string reason) =>
        GateCheckService.RaiseRequestAsync(
            db, notifier, applicationId, AdminRequest.Reject, requestedByUserId, reason);

    /// <summary>
    /// Logs the rejection, marks the application Rejected, and saves. <c>CurrentStage</c> is left
    /// exactly where it was — a rejection stops a file, it does not move it, so the log's
    /// <see cref="RejectionLog.StageAtRejection"/> is the only record of where it died.
    /// </summary>
    /// <remarks>
    /// Called only when an Admin approves a Reject request. The stage is read here rather than passed
    /// in, because by now the officer who asked is long gone and the file's own current stage is the
    /// only truthful answer.
    /// </remarks>
    public static async Task ApplyAsync(LosDbContext db, string applicationId, string reason)
    {
        var application = await db.Applications.FirstAsync(a => a.Id == applicationId);

        db.RejectionLogs.Add(new RejectionLog
        {
            ApplicationId = applicationId,
            StageAtRejection = application.CurrentStage,
            Reason = reason.Trim(),
        });

        application.Status = "Rejected";

        await db.SaveChangesAsync();
    }
}
