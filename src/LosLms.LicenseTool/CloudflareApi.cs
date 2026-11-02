using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace LosLms.LicenseTool;

/// <summary>The three Cloudflare API calls that provision a client: create the tunnel, point the public
/// hostname at localhost:5037, and create the DNS record.</summary>
internal sealed class CloudflareApi
{
    private readonly HttpClient _http;
    private readonly string _account;
    private readonly string _zone;

    public CloudflareApi(string apiToken, string accountId, string zoneId)
    {
        _account = accountId;
        _zone = zoneId;
        _http = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
    }

    public async Task<(string Id, string Token)> CreateTunnelAsync(string name)
    {
        var resp = await _http.PostAsJsonAsync(
            $"accounts/{_account}/cfd_tunnel", new { name, config_src = "cloudflare" });
        var root = await ReadAsync(resp, $"create tunnel '{name}'");
        var result = root.GetProperty("result");
        var id = result.GetProperty("id").GetString()!;

        var token = result.TryGetProperty("token", out var t) ? t.GetString() : null;
        if (string.IsNullOrEmpty(token))
        {
            var tokResp = await _http.GetAsync($"accounts/{_account}/cfd_tunnel/{id}/token");
            var tokRoot = await ReadAsync(tokResp, "fetch tunnel token");
            token = tokRoot.GetProperty("result").GetString();
        }

        return (id, token!);
    }

    public async Task ConfigureIngressAsync(string tunnelId, string hostname, string service)
    {
        var body = new
        {
            config = new
            {
                ingress = new object[]
                {
                    new { hostname, service },
                    new { service = "http_status:404" }, // required catch-all
                },
            },
        };
        var resp = await _http.PutAsJsonAsync(
            $"accounts/{_account}/cfd_tunnel/{tunnelId}/configurations", body);
        await ReadAsync(resp, "configure tunnel ingress");
    }

    public async Task CreateDnsRouteAsync(string hostname, string tunnelId)
    {
        var resp = await _http.PostAsJsonAsync($"zones/{_zone}/dns_records", new
        {
            type = "CNAME",
            name = hostname,
            content = $"{tunnelId}.cfargotunnel.com",
            proxied = true,
        });
        await ReadAsync(resp, $"create DNS record {hostname}");
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage resp, string what)
    {
        var text = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement.Clone();
        if (!root.TryGetProperty("success", out var ok) || !ok.GetBoolean())
        {
            var errors = root.TryGetProperty("errors", out var e) ? e.ToString() : text;
            throw new InvalidOperationException($"Cloudflare API could not {what}: {errors}");
        }

        return root;
    }
}
