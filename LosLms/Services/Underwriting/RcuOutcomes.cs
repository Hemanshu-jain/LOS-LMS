using LosLms.Data;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// The single RCU verdict for an application, rolled up from its per-party outcomes.
/// </summary>
/// <remarks>
/// Two entry points over one rule, for the same reason as
/// <see cref="EligibilityCalculation"/>: Reports (RCU) owns the screen and calls
/// <see cref="OverallStatus"/> against dropdowns the officer is still changing, so the verdict moves
/// live. Approvals has no such state and calls <see cref="GetOverallStatusAsync"/>.
/// </remarks>
public static class RcuOutcomes
{
    public const string Pending = "Pending";
    public const string Recommended = "Recommended";
    public const string NotRecommended = "NotRecommended";

    /// <summary>
    /// Pending wins over everything — an unfinished verification is not a positive one. A single
    /// NotRecommended then outranks any number of Recommended.
    /// </summary>
    /// <remarks>
    /// An empty sequence is Pending, not Recommended: no outcomes recorded means nothing has been
    /// verified, which must never read as a clean result.
    /// </remarks>
    public static string OverallStatus(IEnumerable<string> statuses)
    {
        var list = statuses as IReadOnlyCollection<string> ?? statuses.ToList();

        if (list.Count == 0 || list.Any(s => s == Pending))
        {
            return Pending;
        }

        return list.Any(s => s == NotRecommended) ? NotRecommended : Recommended;
    }

    /// <summary>
    /// Loads the outcomes for an application's visible parties and rolls them up. A guarantor with
    /// no name is not a party, so their outcome row — if one exists — is ignored, matching what the
    /// Reports (RCU) screen displays.
    /// </summary>
    public static async Task<string> GetOverallStatusAsync(LosDbContext db, string applicationId)
    {
        var parties = await db.Parties.AsNoTracking()
            .Where(p => p.ApplicationId == applicationId)
            .ToListAsync();

        var hasGuarantor = PartyRules.HasGuarantor(parties);

        var outcomes = await db.RcuOutcomes.AsNoTracking()
            .Where(o => o.ApplicationId == applicationId)
            .ToListAsync();

        return OverallStatus(outcomes
            .Where(o => o.PartyType != "Guarantor" || hasGuarantor)
            .Select(o => o.Status));
    }
}
