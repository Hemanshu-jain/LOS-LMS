using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LosLms.Data;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// One NBFC's Digio account. Digio issues separate sandbox/production credentials per client, and the
/// webhook secret is per account too. Kept in the encrypted <see cref="PasswordVault"/>, never the database.
/// </summary>
public sealed record DigioCredentials(string ClientId, string ClientSecret, string? WebhookSecret, bool Production);

public enum DigioStatus { NotConfigured, Paused, Failed, Ok }

/// <summary>Outcome of a Digio call. <see cref="Data"/> is Digio's JSON body when there was one.</summary>
public sealed record DigioResult(DigioStatus Status, JsonElement? Data, string? Error)
{
    public static readonly DigioResult NotConfigured = new(DigioStatus.NotConfigured, null, null);

    public string? Str(string name) =>
        Data is { ValueKind: JsonValueKind.Object } d && d.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    public bool? Bool(string name) =>
        Data is { ValueKind: JsonValueKind.Object } d && d.TryGetProperty(name, out var v)
        && v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : null;
}

/// <summary>
/// Every call to Digio goes through <see cref="PostAsync"/>: no credentials → NotConfigured (nothing
/// sent); an overdue bill → Paused (nothing sent); otherwise the call is made and metered. HTTP Basic
/// auth with the client id/secret, per Digio's API reference. Base URLs from Digio's environments page.
/// </summary>
public sealed class DigioClient(
    IHttpClientFactory httpFactory, PasswordVault vault, ApiBillingService billing, ILogger<DigioClient> log)
{
    public const string SandboxUrl = "https://ext.digio.in:444";
    public const string ProductionUrl = "https://api.digio.in";

    private static string VaultKey(int companyId) => $"digio:{companyId}";

    public DigioCredentials? Credentials(int companyId) =>
        vault.TryReveal(VaultKey(companyId)) is { } json ? JsonSerializer.Deserialize<DigioCredentials>(json) : null;

    public void SaveCredentials(int companyId, DigioCredentials credentials) =>
        vault.Store(VaultKey(companyId), JsonSerializer.Serialize(credentials));

    /// <summary>
    /// POSTs JSON to Digio for a company. <paramref name="apiCode"/> is the rate-card code to meter, or
    /// null when Digio bills the action elsewhere (e.g. e-Sign is billed on DOC.SIGNED, not on upload).
    /// </summary>
    public async Task<DigioResult> PostAsync(int companyId, string? apiCode, string path, object body, string? applicationId)
    {
        if (Credentials(companyId) is not { } c)
        {
            return DigioResult.NotConfigured;
        }

        var access = await billing.CheckAccessAsync(companyId);
        if (!access.Allowed)
        {
            return new DigioResult(DigioStatus.Paused, null, access.Reason);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, (c.Production ? ProductionUrl : SandboxUrl) + path)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{c.ClientId}:{c.ClientSecret}")));

        bool ok;
        JsonElement? data = null;
        string? error = null;
        try
        {
            var http = httpFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(60);
            using var response = await http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            try { data = JsonDocument.Parse(text).RootElement.Clone(); } catch (JsonException) { }

            ok = response.IsSuccessStatusCode;
            if (!ok)
            {
                error = new DigioResult(DigioStatus.Failed, data, null).Str("message") ?? $"Digio returned {(int)response.StatusCode}.";
                log.LogWarning("Digio {Path} failed for company {CompanyId}: {Status}", path, companyId, (int)response.StatusCode);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            log.LogWarning(ex, "Digio {Path} unreachable for company {CompanyId}", path, companyId);
            ok = false;
            error = "Could not reach Digio. Please try again.";
        }

        var result = new DigioResult(ok ? DigioStatus.Ok : DigioStatus.Failed, data, error);
        if (apiCode is not null)
        {
            // Failed calls are logged too (not billed) so the report shows them.
            await billing.RecordAsync(companyId, apiCode, ok, applicationId, result.Str("id"));
        }

        return result;
    }

    /// <summary>Digio signs webhooks: X-Digio-Checksum = hex HMAC-SHA256 of the raw body with the account's secret.</summary>
    public static bool ChecksumMatches(string secret, string body, string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return false;
        }

        var expected = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body)));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected.ToLowerInvariant()),
            Encoding.ASCII.GetBytes(header.Trim().ToLowerInvariant()));
    }

    /// <summary>
    /// Applies a verified webhook. Only DigiSign events are acted on today: the ONLY place an agreement
    /// becomes 'Signed'. Idempotent — Digio delivers at least once, and a repeat changes nothing.
    /// Returns true when the agreement newly became signed (the caller meters the e-Sign credit).
    /// </summary>
    public static async Task<bool> ApplyWebhookAsync(LosDbContext db, int companyId, string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var evt = root.TryGetProperty("event", out var e) ? e.GetString()?.ToLowerInvariant() : null;
        var newStatus = evt switch
        {
            "doc.signed" => "Signed",
            "doc.sign.failed" => "Failed",
            "doc.sign.rejected" => "Rejected",
            _ => null,
        };

        if (newStatus is null
            || !root.TryGetProperty("payload", out var payload)
            || !payload.TryGetProperty("document", out var document)
            || !document.TryGetProperty("id", out var idProp)
            || idProp.GetString() is not { Length: > 0 } documentId)
        {
            return false;
        }

        // No signed-in user on a webhook, so bypass the tenant filter and scope by the URL's company.
        var disbursement = await db.Disbursements.IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.EsignDocumentId == documentId && d.Application!.CompanyId == companyId);
        if (disbursement is null || disbursement.AgreementEsignStatus == newStatus || disbursement.AgreementEsignStatus == "Signed")
        {
            return false;
        }

        disbursement.AgreementEsignStatus = newStatus;
        await db.SaveChangesAsync();
        return newStatus == "Signed";
    }
}
