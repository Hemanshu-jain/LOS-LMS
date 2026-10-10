using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using MySqlConnector;

namespace LosLms.Services;

/// <summary>
/// Settings for running as a container on a cloud host (Render + Aiven MySQL + Supabase Storage). Everything
/// comes from environment variables; nothing here changes the desktop/launcher behaviour unless
/// <c>Hosting__Cloud=true</c> (or, for the proxy, <c>Proxy__ForwardLimit</c>) is set.
/// </summary>
public static class CloudConfig
{
    /// <summary>A setting's value with surrounding whitespace removed (env values often carry a stray newline), or null when blank.</summary>
    public static string? Trimmed(this IConfiguration config, string key)
    {
        var value = config[key]?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public static bool IsCloud(this IConfiguration config) =>
        string.Equals(config.Trimmed("Hosting:Cloud"), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The MySQL connection string, trimmed. In cloud mode TLS is always on: with <c>Database__SslCaPem</c>
    /// (the provider's CA certificate text, e.g. Aiven's) the server certificate is verified against it
    /// (VerifyCA); without it the connection is still encrypted (Required) but not verified.
    /// </summary>
    public static string? ConnectionString(IConfiguration config)
    {
        var connectionString = config.GetConnectionString("LosDb")?.Trim();
        if (string.IsNullOrEmpty(connectionString) || !config.IsCloud())
        {
            return connectionString;
        }

        var builder = new MySqlConnectionStringBuilder(connectionString);
        if (config.Trimmed("Database:SslCaPem") is { } pem)
        {
            builder.SslCa = WriteCaFile(pem);
            builder.SslMode = MySqlSslMode.VerifyCA;
        }
        else if (!connectionString.Contains("SslMode", StringComparison.OrdinalIgnoreCase))
        {
            builder.SslMode = MySqlSslMode.Required;
        }

        return builder.ConnectionString;
    }

    /// <summary>Env values can't hold real newlines reliably, so a literal "\n" is accepted in the PEM text.</summary>
    private static string WriteCaFile(string pem)
    {
        pem = pem.Replace("\\n", "\n");
        var name = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pem)))[..12];
        var path = Path.Combine(Path.GetTempPath(), $"db-ca-{name}.pem");
        if (!File.Exists(path))
        {
            File.WriteAllText(path, pem);
        }

        return path;
    }

    /// <summary>
    /// Trust the cloud proxy's forwarded address/scheme, <paramref name="limit"/> hops deep (Render = 1). Only the
    /// entry the nearest proxy appended is believed, so a client-supplied X-Forwarded-For can't pick the IP the rate
    /// limits see. Without a limit the tunnel setup applies: scheme only.
    /// </summary>
    public static void ConfigureProxy(ForwardedHeadersOptions options, int? limit)
    {
        options.ForwardedHeaders = limit is null
            ? ForwardedHeaders.XForwardedProto
            : ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
        if (limit is { } hops)
        {
            options.ForwardLimit = hops;
        }

        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    }
}
