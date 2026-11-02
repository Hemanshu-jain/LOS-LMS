using System.Text.Json.Serialization;

namespace LosLms.Licensing;

/// <summary>
/// What a license grants and to whom. Signed as a whole, so none of it — least of all the expiry — can
/// be edited without the vendor's private key. Bound to a client + tunnel so one client's key cannot run
/// another client's install.
/// </summary>
public sealed record License
{
    /// <summary>The financier this license is issued to, e.g. "ABC Finance".</summary>
    [JsonPropertyName("client")]
    public required string Client { get; init; }

    /// <summary>Their subdomain host, e.g. "abc.bhodhix.com" — must match the build it runs in.</summary>
    [JsonPropertyName("host")]
    public required string Host { get; init; }

    /// <summary>The Cloudflare tunnel id provisioned for them (for the vendor's records / binding).</summary>
    [JsonPropertyName("tunnel")]
    public string? TunnelId { get; init; }

    [JsonPropertyName("issued")]
    public required DateTimeOffset IssuedUtc { get; init; }

    /// <summary>When the app freezes unless renewed.</summary>
    [JsonPropertyName("expires")]
    public required DateTimeOffset ExpiresUtc { get; init; }

    /// <summary>Monotonic version so a newer renewal always supersedes an older stored license.</summary>
    [JsonPropertyName("v")]
    public int Version { get; init; } = 1;
}
