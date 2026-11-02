using LosLms.Licensing;
using LosLms.LicenseTool;

// Vendor-only tool. Usage:
//   loslms-license new-client "ABC Finance" abc [months=12]   -> provisions tunnel+DNS, signs licence
//   loslms-license renew abc [months=12]                      -> issues a renewal key to send the client
//   loslms-license list                                        -> shows all clients + expiry

try
{
    if (args.Length == 0)
    {
        Usage();
        return 1;
    }

    switch (args[0].ToLowerInvariant())
    {
        case "new-client":
            await NewClientAsync(args);
            break;
        case "issue":
            IssueOnly(args);
            break;
        case "renew":
            Renew(args);
            break;
        case "list":
            ListClients();
            break;
        default:
            Usage();
            return 1;
    }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"ERROR: {ex.Message}");
    return 1;
}

static void Usage()
{
    Console.WriteLine("""
        LOS/LMS licence tool
          new-client "<name>" <subdomain> [months=12]   provision a new client (tunnel + DNS + licence)
          renew <subdomain> [months=12]                 issue a renewal key for an existing client
          list                                          list all clients and expiry
        """);
}

static async Task NewClientAsync(string[] args)
{
    if (args.Length < 3)
    {
        throw new ArgumentException("Usage: new-client \"<name>\" <subdomain> [months=12]");
    }

    var name = args[1];
    var subdomain = args[2].ToLowerInvariant();
    var months = args.Length > 3 ? int.Parse(args[3]) : 12;

    var (cfg, privateKey, root) = ToolConfig.Load();
    var host = $"{subdomain}.{cfg.Zone}";
    var tunnelName = $"LOS-LMS-{subdomain}";

    var clients = Registry.Load(root);
    if (clients.Any(c => c.Subdomain.Equals(subdomain, StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException($"A client with subdomain '{subdomain}' already exists. Use 'renew'.");
    }

    Console.WriteLine($"Provisioning {name}  ->  https://{host}");

    var cf = new CloudflareApi(cfg.ApiToken, cfg.AccountId, cfg.ZoneId);
    Console.WriteLine("  • creating tunnel…");
    var (tunnelId, tunnelToken) = await cf.CreateTunnelAsync(tunnelName);
    Console.WriteLine("  • mapping the public hostname…");
    await cf.ConfigureIngressAsync(tunnelId, host, cfg.Service);
    Console.WriteLine("  • creating the DNS record…");
    await cf.CreateDnsRouteAsync(host, tunnelId);

    var now = DateTimeOffset.UtcNow;
    var licenseToken = Issue(name, host, tunnelId, now, now.AddMonths(months), privateKey);

    WriteText(Path.Combine(root, "tunnel-tokens", $"{subdomain}.txt"), tunnelToken);
    WriteText(Path.Combine(root, "clients", $"{subdomain}.license"), licenseToken);

    clients.Add(new ClientRecord(name, subdomain, host, tunnelId, tunnelName, now, now.AddMonths(months)));
    Registry.Save(root, clients);

    Console.WriteLine($"""

        Done. {name} is provisioned and recorded.
          Subscription valid until: {now.AddMonths(months):d MMM yyyy}

        Build their package:
          .\publish.ps1 -Subdomain {host} -TunnelTokenFile tunnel-tokens\{subdomain}.txt -LicenseFile clients\{subdomain}.license -Label {subdomain} -SkipUpdateArtifact
        """);
}

static void IssueOnly(string[] args)
{
    // issue "<name>" <host> <tunnelId> [months=12] — signs a licence for an EXISTING tunnel (no Cloudflare
    // calls, no registry). Used for the first/default deployment whose tunnel already exists.
    if (args.Length < 4)
    {
        throw new ArgumentException("Usage: issue \"<name>\" <host> <tunnelId> [months=12]");
    }

    var name = args[1];
    var host = args[2].ToLowerInvariant();
    var tunnelId = args[3];
    var months = args.Length > 4 ? int.Parse(args[4]) : 12;

    var (privateKey, root) = ToolConfig.LoadKeyOnly();
    var now = DateTimeOffset.UtcNow;
    var token = Issue(name, host, tunnelId, now, now.AddMonths(months), privateKey);

    var label = host.Split('.')[0];
    WriteText(Path.Combine(root, "clients", $"{label}.license"), token);

    Console.WriteLine($"""
        Issued a {months}-month licence for {name} ({host}), valid until {now.AddMonths(months):d MMM yyyy}.
        Written to clients\{label}.license — publish.ps1 -LicenseFile bakes it in.
        """);
}

static void Renew(string[] args)
{
    if (args.Length < 2)
    {
        throw new ArgumentException("Usage: renew <subdomain> [months=12]");
    }

    var subdomain = args[1].ToLowerInvariant();
    var months = args.Length > 2 ? int.Parse(args[2]) : 12;

    var (_, privateKey, root) = ToolConfig.Load();
    var clients = Registry.Load(root);
    var rec = clients.FirstOrDefault(c => c.Subdomain.Equals(subdomain, StringComparison.OrdinalIgnoreCase))
              ?? throw new InvalidOperationException($"No client with subdomain '{subdomain}'. Use 'new-client'.");

    var now = DateTimeOffset.UtcNow;
    // Extend from the current expiry if still valid, otherwise from today.
    var newExpiry = (rec.Expires > now ? rec.Expires : now).AddMonths(months);
    var token = Issue(rec.Name, rec.Host, rec.TunnelId, now, newExpiry, privateKey);

    WriteText(Path.Combine(root, "clients", $"{subdomain}.license"), token);
    clients[clients.IndexOf(rec)] = rec with { Expires = newExpiry };
    Registry.Save(root, clients);

    Console.WriteLine($"""
        Renewed {rec.Name} until {newExpiry:d MMM yyyy}.

        Send this renewal key to the client — their SuperAdmin pastes it into the app's "Renew" screen:

        {token}
        """);
}

static void ListClients()
{
    var (_, _, root) = ToolConfig.Load();
    var clients = Registry.Load(root);
    if (clients.Count == 0)
    {
        Console.WriteLine("No clients yet.");
        return;
    }

    var now = DateTimeOffset.UtcNow;
    Console.WriteLine($"{"CLIENT",-24} {"HOST",-28} {"EXPIRES",-14} STATUS");
    foreach (var c in clients.OrderBy(c => c.Expires))
    {
        var days = (int)Math.Ceiling((c.Expires - now).TotalDays);
        var status = days < 0 ? "EXPIRED" : days <= 14 ? $"{days}d left ⚠" : "active";
        Console.WriteLine($"{Trunc(c.Name, 24),-24} {c.Host,-28} {c.Expires:dd MMM yyyy}  {status}");
    }
}

static string Issue(string name, string host, string tunnelId, DateTimeOffset issued, DateTimeOffset expires, string privateKey)
{
    var license = new License
    {
        Client = name,
        Host = host,
        TunnelId = tunnelId,
        IssuedUtc = issued,
        ExpiresUtc = expires,
    };
    return LicenseToken.Sign(license, privateKey);
}

static void WriteText(string path, string content)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, content);
}

static string Trunc(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";
