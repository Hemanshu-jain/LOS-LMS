using System.Text.RegularExpressions;
using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// What the lists and the inbox say about requests that are waiting for an Admin: which stage each one belongs to (the red bubble on the
/// applications list) and how many times it has been re-triggered (the "Re-triggered" tag).
/// </summary>
public static class PendingApprovals
{
    private static readonly Regex Resent = new("Re-sent ", RegexOptions.Compiled);

    /// <summary>The 1-based stage on which an Admin approval of this request type is asked for.</summary>
    public static int StageOf(string requestType, int currentStage) => requestType switch
    {
        AdminRequest.CibilBypass => 1,
        AdminRequest.ProceedApproval or AdminRequest.MakeModelCapBypass or AdminRequest.MakeModelCapChange => 2,
        AdminRequest.KycBypass => 3,
        AdminRequest.RcuReview => 5,
        AdminRequest.SanctionApproval => 7,
        AdminRequest.DigioReleaseBypass or AdminRequest.DisbursementApproval => 8,
        _ => Math.Clamp(currentStage, 1, LoanStages.Count),
    };

    /// <summary>With several requests waiting on different stages the bubble shows the earliest one: that is where the file is held up first.</summary>
    public static int StageOf(IEnumerable<string> requestTypes, int currentStage) =>
        requestTypes.Select(t => StageOf(t, currentStage)).DefaultIfEmpty(Math.Clamp(currentStage, 1, LoanStages.Count)).Min();

    /// <summary>How many times a still-pending request has been nudged ("Re-sent …" notes added to its reason).</summary>
    public static int ResendNotes(string? reason) => string.IsNullOrEmpty(reason) ? 0 : Resent.Matches(reason).Count;

    /// <summary>
    /// For each pending request, how many times it has been re-triggered: every earlier request of the same kind on the same application that an
    /// Admin denied (this one was raised again after it), plus every "Re-sent" nudge on this one. Requests never re-triggered are left out.
    /// </summary>
    public static async Task<Dictionary<int, int>> RetriggerCountsAsync(LosDbContext db, IReadOnlyCollection<AdminRequest> pending)
    {
        var result = new Dictionary<int, int>();
        if (pending.Count == 0) { return result; }

        var appIds = pending.Select(r => r.ApplicationId).Distinct().ToList();
        var denied = await db.AdminRequests.AsNoTracking()
            .Where(r => appIds.Contains(r.ApplicationId) && r.Status == AdminRequest.Denied)
            .Select(r => new { r.Id, r.ApplicationId, r.RequestType })
            .ToListAsync();

        foreach (var request in pending)
        {
            var count = denied.Count(d => d.ApplicationId == request.ApplicationId && d.RequestType == request.RequestType && d.Id < request.Id)
                        + ResendNotes(request.RequestReason);
            if (count > 0) { result[request.Id] = count; }
        }

        return result;
    }

    /// <summary>"↻ Re-triggered" with the count when it happened more than once.</summary>
    public static string RetriggerLabel(int count) => count > 1 ? $"Re-triggered ×{count}" : "Re-triggered";
}
