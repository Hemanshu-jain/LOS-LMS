namespace LosLms.Server;

/// <summary>Fetches the current server URL from the fixed public location (client role).</summary>
internal sealed class UrlSource
{
    private readonly ClientConfig _config;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public UrlSource(ClientConfig config)
    {
        _config = config;
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
            var raw = (await _http.GetStringAsync(_config.RawUrl(), ct)).Trim();
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
