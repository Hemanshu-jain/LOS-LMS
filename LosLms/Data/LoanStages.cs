namespace LosLms.Data;

/// <summary>
/// The eight stages of the origination flow, in order — the single source of truth for a stage's
/// number, its display label, and its route segment.
/// </summary>
/// <remarks>
/// This used to be seven copies: a <c>StageRoutes</c> array in StageStepper plus a hand-written
/// twelve-line <c>StageRoute(int)</c> switch pasted into six stage screens. Renaming one route meant
/// editing seven files and silently breaking whichever one you missed.
/// </remarks>
public static class LoanStages
{
    /// <summary>Route segment for each stage, index 0 = stage 1. Parallel to <see cref="Labels"/>.</summary>
    public static readonly string[] Routes =
    {
        "customer-details",
        "loan-security",
        "bank-financial",
        "document-checklist",
        "reports-rcu",
        "eligibility",
        "approvals",
        "post-sanction",
    };

    /// <summary>Display label for each stage, index 0 = stage 1. Parallel to <see cref="Routes"/>.</summary>
    public static readonly string[] Labels =
    {
        "Customer Details",
        "Loan & Security",
        "Bank & Financial",
        "Document Checklist",
        "Reports",
        "Eligibility",
        "Approvals",
        "Post Sanction",
    };

    /// <summary>Total number of stages.</summary>
    public static int Count => Routes.Length;

    /// <summary>
    /// Route segment for a 1-based stage number. Out-of-range values clamp to the nearest real stage,
    /// matching the old per-screen switch, whose default arm sent anything unexpected to post-sanction.
    /// </summary>
    public static string Route(int stage) => Routes[Math.Clamp(stage, 1, Count) - 1];

    /// <summary>Full path to a stage screen for a given application.</summary>
    public static string Path(string applicationId, int stage) =>
        $"/applications/{applicationId}/{Route(stage)}";
}
