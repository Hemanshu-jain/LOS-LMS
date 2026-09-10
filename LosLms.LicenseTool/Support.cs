using System.Text.Json;

namespace LosLms.LicenseTool;

/// <summary>Vendor tool settings, from cloudflare-config.json at the repo root (gitignored).</summary>
internal sealed class ToolConfig
{
    public string AccountId { get; init; } = "";
    public string ZoneId { get; init; } = "";
    public string ApiToken { get; init; } = "";
    public string Zone { get; init; } = "bhodhix.com";
    public string Service { get; init; } = "http://localhost:5037";

    public static (ToolConfig Cfg, string PrivateKeyPem, string Root) Load()
    {
        var root = FindRepoRoot();

        var cfgPath = Path.Combine(root, "cloudflare-config.json");
        if (!File.Exists(cfgPath))
        {
            throw new FileNotFoundException(
                $"Missing {cfgPath}. Copy cloudflare-config.example.json to cloudflare-config.json and fill it in.");
        }

        var cfg = JsonSerializer.Deserialize<ToolConfig>(
                      File.ReadAllText(cfgPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                  ?? throw new InvalidOperationException("cloudflare-config.json is empty or invalid.");

        var keyPath = Path.Combine(root, "license-signing-private.pem");
        if (!File.Exists(keyPath))
        {
            throw new FileNotFoundException($"Missing the licence signing key at {keyPath}.");
        }

        return (cfg, File.ReadAllText(keyPath), root);
    }

    /// <summary>Just the signing key + repo root — for the `issue` command, which needs no Cloudflare access.</summary>
    public static (string PrivateKeyPem, string Root) LoadKeyOnly()
    {
        var root = FindRepoRoot();
        var keyPath = Path.Combine(root, "license-signing-private.pem");
        if (!File.Exists(keyPath))
        {
            throw new FileNotFoundException($"Missing the licence signing key at {keyPath}.");
        }

        return (File.ReadAllText(keyPath), root);
    }

    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "LosLms.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }
}

/// <summary>One row in the vendor's client registry — every tunnel tied to a named client.</summary>
internal sealed record ClientRecord(
    string Name,
    string Subdomain,
    string Host,
    string TunnelId,
    string TunnelName,
    DateTimeOffset Issued,
    DateTimeOffset Expires);

/// <summary>The tracked list of all onboarded clients (clients-registry.json, gitignored).</summary>
internal static class Registry
{
    private static string PathFor(string root) => Path.Combine(root, "clients-registry.json");

    public static List<ClientRecord> Load(string root)
    {
        var p = PathFor(root);
        return File.Exists(p)
            ? JsonSerializer.Deserialize<List<ClientRecord>>(File.ReadAllText(p)) ?? new()
            : new();
    }

    public static void Save(string root, List<ClientRecord> list) =>
        File.WriteAllText(PathFor(root), JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
}
