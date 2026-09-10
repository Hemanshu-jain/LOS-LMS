using System.Text.Json;
using System.Text.Json.Serialization;

namespace LosLms.Server;

/// <summary>
/// Operator-provided configuration, read from server-config.json next to the exe (gitignored). Holds the
/// fixed public URL every machine opens and the named Cloudflare tunnel token the host runs, plus
/// optional offsite-backup (FTP) credentials. Absent file ⇒ sensible defaults: a host with no tunnel
/// token simply runs LAN-only; a machine with no URL override uses the built-in default.
/// </summary>
internal sealed class ServerConfig
{
    /// <summary>The permanent public address staff open, e.g. https://los-lms.bhodhix.com.</summary>
    [JsonPropertyName("HostedUrl")]
    public string HostedUrl { get; init; } = DefaultHostedUrl;

    /// <summary>
    /// The named Cloudflare tunnel token (Zero Trust → Networks → Tunnels). The host runs
    /// <c>cloudflared tunnel run --token</c> with it; the hostname → localhost mapping is set once in the
    /// Cloudflare dashboard. Absent ⇒ no tunnel is started (LAN-only host).
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

    public bool HasTunnel => !string.IsNullOrWhiteSpace(TunnelToken);

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
                var config = JsonSerializer.Deserialize<ServerConfig>(json, Options);
                if (config is not null && !string.IsNullOrWhiteSpace(config.HostedUrl))
                {
                    return config;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not read server-config.json ({ex.Message}). Using defaults (LAN-only, default URL).");
        }

        return new ServerConfig();
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}
