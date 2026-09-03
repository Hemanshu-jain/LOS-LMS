namespace LosLms.Server;

/// <summary>Fetches the current server URL from the fixed public location (client role).</summary>
internal sealed class UrlSource
{
    // The client reads the same repo file the host publishes to. Fixed — it is the app's own repo.
    private const string RawUrlBase = "https://raw.githubusercontent.com/Hemanshu-jain/LOS-LMS/main/url.txt";

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public UrlSource()
    {
        _http.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
        {
            NoCache = true,
        };
    }

    /// <summary>
    /// Returns the trimmed server URL, or null if it can't be fetched or the location holds nothing
    /// usable. Never throws — the caller shows a friendly retry panel on null.
    /// </summary>
    public async Task<string?> FetchAsync(CancellationToken ct)
    {
        try
        {
            // Cache-buster so a host restart is seen promptly, past the raw.githubusercontent CDN.
            var url = $"{RawUrlBase}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            var raw = (await _http.GetStringAsync(url, ct)).Trim();
            if (raw.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                && Uri.TryCreate(raw, UriKind.Absolute, out _))
            {
                return raw;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
