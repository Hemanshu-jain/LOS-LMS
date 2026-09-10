using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace LosLms.Services;

/// <summary>
/// Checks GitHub Releases for a newer build on startup and every few hours, stores the outcome for the
/// SuperAdmin banner, and — unless disabled — AUTO-APPLIES a newer signed release so a single published
/// release reaches every install hands-free.
/// </summary>
/// <remarks>
/// Auto-apply reuses the same verified path as the manual button: <see cref="UpdateService.DownloadAsync"/>
/// checks the vendor signature before staging, and the watchdog's swap rolls back on any failure — so an
/// unsigned or broken release is never applied. Set <c>Updates:AutoApply=false</c> to fall back to the
/// notify-only, one-click behaviour. A failed check or apply is swallowed; the previous state is kept.
/// </remarks>
public sealed class UpdateCheckBackgroundService(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    IHostEnvironment env,
    UpdateNotificationService notifications) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(20);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the app finish coming up before the first check.
        if (!await DelayAsync(StartupDelay, stoppingToken))
        {
            return;
        }

        // Same source the System Updates page reads, so both agree on what "current" is.
        var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "unknown";

        while (!stoppingToken.IsCancellationRequested)
        {
            var http = httpClientFactory.CreateClient();
            var result = await UpdateService.CheckAsync(
                http, config["Updates:GitHubOwner"], config["Updates:GitHubRepo"],
                currentVersion, stoppingToken);

            // Only overwrite on a real answer — a failed poll must not clear a standing banner.
            if (result.Checked)
            {
                notifications.Set(result);

                if (config.GetValue("Updates:AutoApply", true)
                    && result.UpdateAvailable
                    && result.AssetDownloadUrl is { } url
                    && result.AssetName is { } name)
                {
                    await TryAutoApplyAsync(http, url, name, result.LatestTag, stoppingToken);
                }
            }

            if (!await DelayAsync(Interval, stoppingToken))
            {
                return;
            }
        }
    }

    /// <summary>
    /// Downloads the newer release (verifying the vendor signature) and signals the watchdog to swap it
    /// in. The watchdog stops the backend, applies, and restarts — so a new version reaches this install
    /// with no one clicking anything. A failure here just leaves the current version running.
    /// </summary>
    private async Task TryAutoApplyAsync(
        HttpClient http, string url, string name, string? tag, CancellationToken ct)
    {
        try
        {
            var stagingFolder = Path.Combine(env.ContentRootPath, config["Updates:StagingFolder"] ?? "updates");
            var zip = await UpdateService.DownloadAsync(http, url, name, stagingFolder, ct);
            await UpdateService.WriteApplySignal(stagingFolder, zip, ct);
            // The watchdog will now stop this process to apply; nothing more to do.
        }
        catch (Exception)
        {
            // Signature mismatch, download error, or disk issue — keep running the current version.
            // The SuperAdmin still has the manual button, and the next poll will retry.
        }
    }

    /// <summary>Delays, returning false when the app is shutting down so the loop can exit cleanly.</summary>
    private static async Task<bool> DelayAsync(TimeSpan delay, CancellationToken token)
    {
        try
        {
            await Task.Delay(delay, token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
