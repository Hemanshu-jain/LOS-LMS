using System.Text.Json;
using System.Text.Json.Serialization;

namespace LosLms.Server;

/// <summary>
/// The operator-provided configuration, read from server-config.json next to the exe. Only the GitHub
/// token is a real secret; everything else has a sensible default so an operator who only wants local
/// use can leave the file absent entirely (the tunnel is then still opened, but publishing is skipped).
/// </summary>
internal sealed class ServerConfig
{
    [JsonPropertyName("GitHubToken")]
    public string? GitHubToken { get; init; }

    [JsonPropertyName("UrlPublish")]
    public UrlPublishConfig UrlPublish { get; init; } = new();

    public bool CanPublish => !string.IsNullOrWhiteSpace(GitHubToken);

    public static ServerConfig Load()
    {
        try
        {
            if (File.Exists(Paths.ServerConfigFile))
            {
                var json = File.ReadAllText(Paths.ServerConfigFile);
                var config = JsonSerializer.Deserialize<ServerConfig>(json, Options);
                if (config is not null)
                {
                    return config;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not read server-config.json ({ex.Message}). Running local-only (no URL publish).");
        }

        return new ServerConfig();
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    internal sealed class UrlPublishConfig
    {
        [JsonPropertyName("Owner")]
        public string Owner { get; init; } = "Hemanshu-jain";

        [JsonPropertyName("Repo")]
        public string Repo { get; init; } = "LOS-LMS";

        [JsonPropertyName("Path")]
        public string Path { get; init; } = "url.txt";

        [JsonPropertyName("Branch")]
        public string Branch { get; init; } = "main";
    }
}
