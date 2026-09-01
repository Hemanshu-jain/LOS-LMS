using System.Text.Json;
using System.Text.Json.Serialization;

namespace LosLms.Server;

/// <summary>
/// Where the client role reads the current server URL from. Defaults to the same public repo file the
/// host publishes to, so a staff machine needs no configuration at all; an optional client-config.json
/// next to the exe can repoint it without a rebuild.
/// </summary>
internal sealed class ClientConfig
{
    [JsonPropertyName("Owner")]
    public string Owner { get; init; } = "Hemanshu-jain";

    [JsonPropertyName("Repo")]
    public string Repo { get; init; } = "LOS-LMS";

    [JsonPropertyName("Path")]
    public string Path { get; init; } = "url.txt";

    [JsonPropertyName("Branch")]
    public string Branch { get; init; } = "main";

    /// <summary>
    /// The raw URL, with a cache-buster so a host restart is seen promptly instead of being masked by
    /// the raw.githubusercontent.com CDN cache.
    /// </summary>
    public string RawUrl() =>
        $"https://raw.githubusercontent.com/{Owner}/{Repo}/{Branch}/{Path}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

    public static ClientConfig Load()
    {
        try
        {
            var file = System.IO.Path.Combine(Paths.InstallRoot, "client-config.json");
            if (File.Exists(file))
            {
                var config = JsonSerializer.Deserialize<ClientConfig>(
                    File.ReadAllText(file), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (config is not null)
                {
                    return config;
                }
            }
        }
        catch
        {
            // Fall back to the compiled defaults — the shipped client works with no config file.
        }

        return new ClientConfig();
    }
}
