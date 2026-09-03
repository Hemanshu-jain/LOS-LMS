using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace LosLms.Services;

/// <summary>
/// Checks GitHub Releases for a newer build on startup and every few hours, storing the outcome in
/// <see cref="UpdateNotificationService"/> so the SuperAdmin gets a banner without opening the System
/// Updates page.
/// </summary>
/// <remarks>
/// Read-only: it never downloads or applies an update — that stays an explicit SuperAdmin action. A
/// failed check (offline, rate-limited) is swallowed and the previous result is kept, so no banner
/// flickers away just because one poll could not reach GitHub.
/// </remarks>
public sealed class UpdateCheckBackgroundService(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
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
            }

            if (!await DelayAsync(Interval, stoppingToken))
            {
                return;
            }
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
