using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// Rejecting an application — the one write shared by every stage screen's sub-header and the
/// dashboard drawer, so the three steps never drift between callers.
/// </summary>
public static class RejectionService
{
    /// <summary>
    /// Logs the rejection, marks the application Rejected, and saves. <c>CurrentStage</c> is left
    /// exactly where it was — a rejection stops a file, it does not move it, so the log's
    /// <see cref="RejectionLog.StageAtRejection"/> is the only record of where it died.
    /// </summary>
    /// <remarks>
    /// The caller supplies <paramref name="currentStage"/> rather than this method re-reading it, so
    /// the stage recorded is exactly the one the officer saw when they rejected.
    /// </remarks>
    public static async Task RejectAsync(
        LosDbContext db, string applicationId, int currentStage, string reason)
    {
        db.RejectionLogs.Add(new RejectionLog
        {
            ApplicationId = applicationId,
            StageAtRejection = currentStage,
            Reason = reason.Trim(),
        });

        var application = await db.Applications.FirstAsync(a => a.Id == applicationId);
        application.Status = "Rejected";

        await db.SaveChangesAsync();
    }
}
