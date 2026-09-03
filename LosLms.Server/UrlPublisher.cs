using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LosLms.Server;

/// <summary>
/// Publishes the current tunnel URL to one small, fixed, publicly-fetchable location whose own address
/// never changes — a file in the operator's public GitHub repo — so the Client Shell always knows
/// where to look even though the tunnel URL itself changes on every restart. Writing needs the
/// operator's Personal Access Token (a real credential they provide in server-config.json), so this is a
/// no-op when no token is configured.
/// </summary>
internal sealed class UrlPublisher
{
    // The app's own public repo file the client reads — fixed, so no config knob for it.
    private const string Owner = "Hemanshu-jain";
    private const string Repo = "LOS-LMS";
    private const string FilePath = "url.txt";
    private const string Branch = "main";

    private readonly ServerConfig _config;

    public UrlPublisher(ServerConfig config) => _config = config;

    /// <summary>
    /// Writes <paramref name="url"/> to the configured repo file via the Contents API (create or
    /// update). Returns true on success. Never throws — a publish failure must not take down a server
    /// that is otherwise working locally.
    /// </summary>
    public async Task<bool> PublishAsync(string url, CancellationToken ct)
    {
        if (!_config.CanPublish)
        {
            Log.Warn("No GitHub token in server-config.json — skipping URL publish. Remote clients cannot auto-discover this server.");
            return false;
        }

        var apiUrl = $"https://api.github.com/repos/{Owner}/{Repo}/contents/{FilePath}";

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LosLms-Server", "1.0"));
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.GitHubToken);

            // The Contents API needs the current blob sha to update an existing file.
            var existingSha = await GetExistingShaAsync(http, $"{apiUrl}?ref={Branch}", ct);

            var body = new PutContentsRequest
            {
                Message = $"tunnel url {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z",
                Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(url)),
                Branch = Branch,
                Sha = existingSha,
            };

            using var response = await http.PutAsJsonAsync(apiUrl, body, JsonOpts, ct);
            if (response.IsSuccessStatusCode)
            {
                Log.Info($"Published tunnel URL to {Owner}/{Repo}/{FilePath}.");
                return true;
            }

            var detail = await response.Content.ReadAsStringAsync(ct);
            Log.Error($"Publishing the URL failed ({(int)response.StatusCode} {response.StatusCode}): {detail}");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error("Publishing the URL failed", ex);
            return false;
        }
    }

    private static async Task<string?> GetExistingShaAsync(HttpClient http, string url, CancellationToken ct)
    {
        using var response = await http.GetAsync(url, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null; // first publish — the file does not exist yet
        }

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("sha", out var sha) ? sha.GetString() : null;
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private sealed class PutContentsRequest
    {
        [JsonPropertyName("message")]
        public required string Message { get; init; }

        [JsonPropertyName("content")]
        public required string Content { get; init; }

        [JsonPropertyName("branch")]
        public required string Branch { get; init; }

        [JsonPropertyName("sha")]
        public string? Sha { get; init; }
    }
}
