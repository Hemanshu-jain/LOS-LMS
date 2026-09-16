using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// Commits the credit sanction — the one place it happens, whether an Admin sanctions directly on the
/// Approvals screen or approves an officer's Sanction-approval request from the file.
/// </summary>
/// <remarks>
/// The recommender is the officer who worked the file (its assigned/initiating officer); the approver is
/// the Admin sanctioning it. Keeping this in one method means the direct path and the request-approval
/// path can never record the decision differently.
/// </remarks>
public static class SanctionService
{
    /// <summary>
    /// Records recommender + approver, confirms the decision, and moves the application to Sanctioned.
    /// The caller owns <c>SaveChangesAsync</c> so the sanction and any surrounding change commit together.
    /// </summary>
    /// <param name="approverUserId">The Admin performing (or approving) the sanction.</param>
    public static async Task ApplyAsync(LosDbContext db, string applicationId, string? approverUserId)
    {
        var application = await db.Applications.FirstOrDefaultAsync(a => a.Id == applicationId);
        if (application is null)
        {
            return;
        }

        var decision = await db.ApprovalDecisions.FirstOrDefaultAsync(d => d.ApplicationId == applicationId);
        if (decision is null)
        {
            decision = new ApprovalDecision { ApplicationId = applicationId, CreatedAt = DateTime.UtcNow };
            db.ApprovalDecisions.Add(decision);
        }

        var today = DateOnly.FromDateTime(DateTime.Today);

        // Recommender = the initiating officer. Only stamped if not already set, so an explicit
        // recommendation is never overwritten.
        decision.RecommenderUserId ??= application.AssignedOfficerId;
        decision.RecommenderDate ??= today;

        decision.ApproverUserId = approverUserId;
        decision.ApproverDate = today;
        decision.SanctionConfirmed = true;
        decision.SanctionConfirmedAt = DateTime.UtcNow;
        decision.UpdatedAt = DateTime.UtcNow;

        // Never move an application backwards.
        application.CurrentStage = Math.Max(application.CurrentStage, 8);
        application.Status = "Sanctioned";
        application.UpdatedAt = DateTime.UtcNow;
    }
}
