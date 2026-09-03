namespace LosLms.Services;

/// <summary>
/// Holds the most recent update check so any screen can show a "new version available" banner without
/// hitting GitHub itself. A singleton: one shared result for the whole server.
/// </summary>
/// <remarks>
/// Populated by <see cref="UpdateCheckBackgroundService"/> on startup and on a timer, and refreshed by
/// the System Updates page when a SuperAdmin checks manually. It never downloads or applies anything —
/// that stays an explicit SuperAdmin action on the updates page.
/// </remarks>
public sealed class UpdateNotificationService
{
    private volatile UpdateCheckResult? _latest;

    /// <summary>The last successful check, or null if none has succeeded yet.</summary>
    public UpdateCheckResult? Latest => _latest;

    /// <summary>Stores a check result. Callers pass only successful checks, so a transient failure
    /// (offline, rate-limited) leaves the last known-good result — and its banner — in place.</summary>
    public void Set(UpdateCheckResult result) => _latest = result;

    /// <summary>True only when GitHub was reached and reported a genuinely newer release.</summary>
    public bool UpdateAvailable => _latest is { Checked: true, UpdateAvailable: true };

    public string? LatestTag => _latest?.LatestTag;
}
