using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LosLms.Server;

/// <summary>
/// Per-deployment configuration. The public URL and the tunnel token are normally BAKED into the build
/// (one build per client — see publish.ps1), so a client install pastes nothing. A server-config.json
/// next to the exe can still override either value and supply optional offsite-backup (FTP) details.
/// With nothing baked and no config, the host runs LAN-only at the built-in default URL.
/// </summary>
internal sealed class ServerConfig
{
    /// <summary>Override for the permanent public address (e.g. https://client1.bhodhix.com). Normally baked.</summary>
    [JsonPropertyName("HostedUrl")]
    public string? HostedUrl { get; init; }

    /// <summary>
    /// Override for the named Cloudflare tunnel token. Normally baked. The host runs
    /// <c>cloudflared tunnel run --token</c> with it; the hostname → localhost mapping is set once in the
    /// Cloudflare dashboard.
    /// </summary>
    [JsonPropertyName("TunnelToken")]
    public string? TunnelToken { get; init; }

    // Optional offsite backup target. When host/user/password are all set, the nightly database dump is
    // uploaded here after it is written locally.
    [JsonPropertyName("BackupFtpHost")] public string? BackupFtpHost { get; init; }
    [JsonPropertyName("BackupFtpUser")] public string? BackupFtpUser { get; init; }
    [JsonPropertyName("BackupFtpPassword")] public string? BackupFtpPassword { get; init; }
    [JsonPropertyName("BackupFtpDir")] public string? BackupFtpDir { get; init; }

    public const string DefaultHostedUrl = "https://los-lms.bhodhix.com";

    /// <summary>The address this build opens: a config override wins, else the baked URL, else the default.</summary>
    public string EffectiveHostedUrl =>
        FirstNonBlank(HostedUrl, Baked("HostedUrl").Value) ?? DefaultHostedUrl;

    /// <summary>
    /// The token used to start the tunnel: a config override wins, else the token baked into this build.
    /// This is what lets a per-client install just work — nobody pastes anything.
    /// </summary>
    public string? EffectiveTunnelToken => FirstNonBlank(TunnelToken, Baked("TunnelToken").Value);

    public bool HasTunnel => !string.IsNullOrWhiteSpace(EffectiveTunnelToken);

    public bool HasBackupUpload =>
        !string.IsNullOrWhiteSpace(BackupFtpHost)
        && !string.IsNullOrWhiteSpace(BackupFtpUser)
        && !string.IsNullOrWhiteSpace(BackupFtpPassword);

    public static ServerConfig Load()
    {
        try
        {
            if (File.Exists(Paths.ServerConfigFile))
            {
                var json = File.ReadAllText(Paths.ServerConfigFile);
                if (JsonSerializer.Deserialize<ServerConfig>(json, Options) is { } config)
                {
                    return config;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not read server-config.json ({ex.Message}). Using the baked/default values.");
        }

        return new ServerConfig();
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static string? FirstNonBlank(string? a, string? b) =>
        !string.IsNullOrWhiteSpace(a) ? a : (!string.IsNullOrWhiteSpace(b) ? b : null);

    // Values baked into the exe at publish time as base64 assembly metadata (publish.ps1). Cached per key.
    private static readonly Dictionary<string, Lazy<string?>> BakedCache = new();

    private static Lazy<string?> Baked(string key)
    {
        lock (BakedCache)
        {
            if (!BakedCache.TryGetValue(key, out var value))
            {
                value = new Lazy<string?>(() => ReadBaked(key));
                BakedCache[key] = value;
            }

            return value;
        }
    }

    private static string? ReadBaked(string key)
    {
        var encoded = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == key)?.Value;

        if (string.IsNullOrEmpty(encoded))
        {
            return null;
        }

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(encoded)).Trim();
        }
        catch
        {
            return null;
        }
    }
}
