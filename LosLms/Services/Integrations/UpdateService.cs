using System.Net;
using System.Text.Json;

namespace LosLms.Services;

/// <summary>Outcome of checking GitHub Releases for a newer build.</summary>
/// <param name="Checked">True when GitHub was reached and a release was parsed. False = honest failure.</param>
/// <param name="FailureReason">Why the check couldn't complete. Set only when <paramref name="Checked"/> is false.</param>
/// <param name="CurrentVersion">The running app's version.</param>
/// <param name="LatestTag">The latest release tag, e.g. "v1.0.1". Null on failure.</param>
/// <param name="UpdateAvailable">True when the latest release is a higher version than the running one.</param>
/// <param name="ReleaseNotes">The release body/changelog, rendered as-is. Null when none.</param>
/// <param name="AssetName">The .zip release asset's file name. Null when the release has no zip asset.</param>
/// <param name="AssetDownloadUrl">Direct download URL for that asset.</param>
/// <param name="AssetSize">Asset size in bytes, when GitHub reports it.</param>
public sealed record UpdateCheckResult(
    bool Checked,
    string? FailureReason,
    string CurrentVersion,
    string? LatestTag,
    bool UpdateAvailable,
    string? ReleaseNotes,
    string? AssetName,
    string? AssetDownloadUrl,
    long AssetSize);

/// <summary>
/// Checks for and downloads updates from a public GitHub repository's Releases. Applying an update is
/// deliberately NOT here — a running Windows exe cannot overwrite itself, so the actual swap is done by
/// the separate watchdog process, which this only signals (see <see cref="WriteApplySignal"/>).
/// </summary>
/// <remarks>
/// Owner/repo come from configuration (<c>Updates:GitHubOwner</c> / <c>Updates:GitHubRepo</c>), never
/// hardcoded. A public repo needs no token for unauthenticated release reads. Every failure path
/// returns an honest reason rather than throwing or going quiet.
/// </remarks>
public static class UpdateService
{
    /// <summary>Well-known signal file the watchdog polls for. Its contents are the staged zip's path.</summary>
    public const string ApplySignalFileName = "apply.signal";

    public static async Task<UpdateCheckResult> CheckAsync(
        HttpClient http, string? owner, string? repo, string currentVersion, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
        {
            return Failed(currentVersion,
                "Update source is not configured. Set Updates:GitHubOwner and Updates:GitHubRepo in appsettings.json.");
        }

        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            // GitHub rejects requests without a User-Agent.
            request.Headers.UserAgent.ParseAdd("LosLms-Updater");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");

            using var response = await http.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Failed(currentVersion,
                    $"No published release found for {owner}/{repo} (the repo may be private, misspelled, or have no releases yet).");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return Failed(currentVersion, "GitHub API rate limit reached — try again in a little while.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return Failed(currentVersion, $"GitHub returned {(int)response.StatusCode} {response.ReasonPhrase}.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            var tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() : null;
            if (string.IsNullOrWhiteSpace(tag))
            {
                return Failed(currentVersion, "GitHub returned a release with no tag.");
            }

            var notes = root.TryGetProperty("body", out var b) ? b.GetString() : null;

            string? assetName = null;
            string? assetUrl = null;
            long assetSize = 0;
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (name is not null && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        assetName = name;
                        assetUrl = asset.TryGetProperty("browser_download_url", out var d) ? d.GetString() : null;
                        assetSize = asset.TryGetProperty("size", out var s) ? s.GetInt64() : 0;
                        break;
                    }
                }
            }

            var updateAvailable = IsNewer(tag, currentVersion);
            return new UpdateCheckResult(true, null, currentVersion, tag, updateAvailable, notes, assetName, assetUrl, assetSize);
        }
        catch (Exception ex)
        {
            return Failed(currentVersion, $"Couldn't reach GitHub — {ex.Message}");
        }
    }

    /// <summary>Streams the release asset zip into the staging folder; returns the saved file path.</summary>
    public static async Task<string> DownloadAsync(
        HttpClient http, string assetUrl, string assetName, string stagingFolder, CancellationToken ct = default)
    {
        Directory.CreateDirectory(stagingFolder);
        var destination = Path.Combine(stagingFolder, assetName);

        using var response = await http.GetAsync(assetUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(ct);
        await using var file = File.Create(destination);
        await source.CopyToAsync(file, ct);

        return destination;
    }

    /// <summary>
    /// Writes the apply signal the watchdog polls for: the path of the staged zip to swap in. Written
    /// only after an explicit SuperAdmin confirmation — the actual swap is the watchdog's job.
    /// </summary>
    public static async Task WriteApplySignal(string stagingFolder, string zipPath, CancellationToken ct = default)
    {
        Directory.CreateDirectory(stagingFolder);
        await File.WriteAllTextAsync(Path.Combine(stagingFolder, ApplySignalFileName), zipPath, ct);
    }

    /// <summary>True when <paramref name="tag"/> parses to a higher version than the running one.</summary>
    private static bool IsNewer(string tag, string currentVersion)
    {
        return Version.TryParse(Normalize(tag), out var latest)
            && Version.TryParse(Normalize(currentVersion), out var current)
            && latest > current;
    }

    /// <summary>Strips a leading "v" and any pre-release/build suffix so "v1.2.3-rc1" parses as 1.2.3.</summary>
    private static string Normalize(string version)
    {
        var v = version.Trim().TrimStart('v', 'V');
        var dash = v.IndexOfAny(new[] { '-', '+' });
        return dash >= 0 ? v[..dash] : v;
    }

    private static UpdateCheckResult Failed(string currentVersion, string reason) =>
        new(false, reason, currentVersion, null, false, null, null, null, 0);
}
