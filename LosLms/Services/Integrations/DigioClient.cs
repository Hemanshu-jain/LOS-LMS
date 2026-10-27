using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// One NBFC's Digio account. Digio issues separate sandbox/production credentials per client, and the
/// webhook secret is per account too. Kept in the encrypted <see cref="PasswordVault"/>, never the database.
/// </summary>
public sealed record DigioCredentials(string ClientId, string ClientSecret, string? WebhookSecret, bool Production);

public enum DigioStatus { NotConfigured, Paused, Failed, Ok }

/// <summary>Digio's addresses, from its environments page. One place, so nothing else spells a URL.</summary>
public static class DigioEndpoints
{
    public const string SandboxApi = "https://ext.digio.in:444";
    public const string ProductionApi = "https://api.digio.in";

    /// <summary>Hosted customer pages (sign, KYC, mandate): <c>{gateway}{id}/{txn}/{identifier}?redirect_url=…</c>.</summary>
    public const string SandboxGateway = "https://ext.digio.in/#/gateway/login/";
    public const string ProductionGateway = "https://app.digio.in/#/gateway/login/";

    /// <summary>The addresses Digio's webhooks come from.</summary>
    public const string SandboxWebhookIp = "35.154.20.28";
    public const string ProductionWebhookIp = "13.126.198.236";

    public static string Api(bool production) => production ? ProductionApi : SandboxApi;

    public static string Gateway(bool production) => production ? ProductionGateway : SandboxGateway;

    public static string Environment(bool production) => production ? "Production" : "Sandbox";
}

/// <summary>Outcome of a Digio call. <see cref="Data"/> is Digio's JSON body when there was one.</summary>
public sealed record DigioResult(DigioStatus Status, JsonElement? Data, string? Error)
{
    /// <summary>The body of a binary download (signed PDF…); null for JSON calls and failures.</summary>
    public byte[]? Bytes { get; init; }

    /// <summary>Sandbox or Production, whenever a call was actually made.</summary>
    public string? Environment { get; init; }

    /// <summary>Our <see cref="DigioRequest"/> id for a tracked call.</summary>
    public string? RequestId { get; init; }

    /// <summary>Digio's own error code (its GenericExceptionDto.code), when it sent one.</summary>
    public string? ErrorCode { get; init; }

    public static readonly DigioResult NotConfigured = new(DigioStatus.NotConfigured, null, null);

    public string? Str(string name) =>
        Data is { ValueKind: JsonValueKind.Object } d && d.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    public bool? Bool(string name) =>
        Data is { ValueKind: JsonValueKind.Object } d && d.TryGetProperty(name, out var v)
        && v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : null;
}

/// <summary>
/// One call to Digio. A call with a <see cref="Product"/> is tracked: a <see cref="DigioRequest"/> row (its id =
/// <see cref="RequestId"/>) is written before the call is sent and updated after. The caller sends <see cref="RequestId"/>
/// to Digio as its idempotency key (unique_request_id / reference_id / customer_ref_number), so a retry of the same
/// action reuses it; a null one gets a fresh id.
/// </summary>
public sealed record DigioCall(
    int CompanyId,
    HttpMethod Method,
    string Path,
    object? Json = null,
    string? ApiCode = null,
    string? Product = null,
    string? Operation = null,
    string? ApplicationId = null,
    int? PartyId = null,
    string? RequestId = null,
    string? InputFingerprint = null,
    string? RequestedByUserId = null,
    bool ExpectBinary = false,
    TimeSpan? Timeout = null,
    string SuccessStatus = "Succeeded");

/// <summary>
/// Every call to Digio goes through <see cref="SendAsync"/>: no credentials → NotConfigured (nothing sent); an overdue
/// bill → Paused (nothing sent); otherwise the call is made, metered and (when tracked) recorded. HTTP Basic auth with the
/// client id/secret, per Digio's API reference.
/// </summary>
public sealed class DigioClient(
    IHttpClientFactory httpFactory, PasswordVault vault, ApiBillingService billing,
    IDbContextFactory<LosDbContext> dbFactory, ILogger<DigioClient> log)
{
    public const string SandboxUrl = DigioEndpoints.SandboxApi;
    public const string ProductionUrl = DigioEndpoints.ProductionApi;

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    private static string VaultKey(int companyId) => $"digio:{companyId}";

    public DigioCredentials? Credentials(int companyId) =>
        vault.TryReveal(VaultKey(companyId)) is { } json ? JsonSerializer.Deserialize<DigioCredentials>(json) : null;

    public void SaveCredentials(int companyId, DigioCredentials credentials) =>
        vault.Store(VaultKey(companyId), JsonSerializer.Serialize(credentials));

    /// <summary>
    /// POSTs JSON to Digio for a company. <paramref name="apiCode"/> is the rate-card code to meter, or
    /// null when Digio bills the action elsewhere (e.g. e-Sign is billed on DOC.SIGNED, not on upload).
    /// </summary>
    public Task<DigioResult> PostAsync(int companyId, string? apiCode, string path, object body, string? applicationId) =>
        SendAsync(new DigioCall(companyId, HttpMethod.Post, path, body, apiCode, ApplicationId: applicationId));

    /// <summary>GET with the same gate (keys, billing pause, metering) as <see cref="PostAsync"/>.</summary>
    public Task<DigioResult> GetAsync(int companyId, string? apiCode, string path, string? applicationId) =>
        SendAsync(new DigioCall(companyId, HttpMethod.Get, path, null, apiCode, ApplicationId: applicationId));

    /// <summary>GET of a binary body (e.g. the signed agreement PDF); <see cref="DigioResult.Bytes"/> on success.</summary>
    public Task<DigioResult> DownloadAsync(int companyId, string? apiCode, string path, string? applicationId) =>
        SendAsync(new DigioCall(companyId, HttpMethod.Get, path, null, apiCode, ApplicationId: applicationId, ExpectBinary: true));

    /// <summary>The one implementation behind every verb. See <see cref="DigioCall"/>.</summary>
    public async Task<DigioResult> SendAsync(DigioCall call)
    {
        if (Credentials(call.CompanyId) is not { } c)
        {
            return DigioResult.NotConfigured;
        }

        var access = await billing.CheckAccessAsync(call.CompanyId);
        if (!access.Allowed)
        {
            return new DigioResult(DigioStatus.Paused, null, access.Reason);
        }

        var environment = DigioEndpoints.Environment(c.Production);
        var requestId = call.Product is null ? null : call.RequestId ?? Guid.NewGuid().ToString("N");
        if (requestId is not null)
        {
            await InsertRequestAsync(call, requestId, environment);
        }

        var result = (await HttpAsync(call, c)) with { Environment = environment, RequestId = requestId };
        if (call.ApiCode is not null)
        {
            // Failed calls are logged too (not billed) so the report shows them.
            await billing.RecordAsync(call.CompanyId, call.ApiCode, result.Status == DigioStatus.Ok, call.ApplicationId, result.Str("id"));
        }

        if (requestId is not null)
        {
            await CompleteRequestAsync(requestId, call, result);
        }

        return result;
    }

    // A POST is never retried (it could create twice; the user's "Try again" reuses the same request id).
    // A GET gets one more try after a network error.
    private async Task<DigioResult> HttpAsync(DigioCall call, DigioCredentials c)
    {
        var attempts = call.Method == HttpMethod.Get ? 2 : 1;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(call.Method, DigioEndpoints.Api(c.Production) + call.Path)
                {
                    Content = call.Json is null ? null : JsonContent.Create(call.Json),
                };
                request.Headers.Authorization = new("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{c.ClientId}:{c.ClientSecret}")));

                var http = httpFactory.CreateClient();
                http.Timeout = call.Timeout ?? DefaultTimeout;
                using var response = await http.SendAsync(request);
                var ok = response.IsSuccessStatusCode;
                if (ok && call.ExpectBinary)
                {
                    return new DigioResult(DigioStatus.Ok, null, null) { Bytes = await response.Content.ReadAsByteArrayAsync() };
                }

                JsonElement? data = null;
                try { data = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone(); } catch (JsonException) { }
                var parsed = new DigioResult(ok ? DigioStatus.Ok : DigioStatus.Failed, data, null);
                if (ok)
                {
                    return parsed;
                }

                // Never log or return the raw body (it can carry PII): a code, a plain-words reason, nothing else.
                log.LogWarning("Digio {Path} failed for company {CompanyId}: {Status}", call.Path.Split('?')[0], call.CompanyId, (int)response.StatusCode);
                var code = data is { ValueKind: JsonValueKind.Object } d && d.TryGetProperty("code", out var cv) ? cv.ToString() : null;
                return parsed with { Error = Describe(response.StatusCode, parsed.Str("message")), ErrorCode = code is { Length: > 0 } ? Cut(code, 40) : null };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                if (attempt < attempts)
                {
                    continue;
                }

                log.LogWarning("Digio {Path} unreachable for company {CompanyId}: {Error}", call.Path.Split('?')[0], call.CompanyId, ex.GetType().Name);
                return new DigioResult(DigioStatus.Failed, null, "Could not reach Digio. Please try again.");
            }
        }
    }

    private static string Describe(HttpStatusCode status, string? digioMessage) => (int)status switch
    {
        401 => "Digio rejected the keys. Check them and the sandbox/production setting.",
        403 => "This Digio product is not enabled on your account. Contact Digio.",
        404 => "Digio could not find that item.",
        429 => "Digio is busy. Try again shortly.",
        >= 500 => "Digio is unavailable. Try again in a moment.",
        _ => string.IsNullOrWhiteSpace(digioMessage) ? $"Digio returned {(int)status}." : Cut(digioMessage, 200),
    };

    private static string Cut(string text, int max) => text.Length > max ? text[..max] : text;

    // ---- the request log (Id is ours; the row exists before the call leaves) ----

    private async Task InsertRequestAsync(DigioCall call, string id, string environment)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var existing = await db.DigioRequests.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == call.CompanyId);
        if (existing is not null)
        {
            // The same action tried again: reuse the row.
            existing.Status = "Sending";
            existing.ErrorCode = null;
            existing.ErrorMessage = null;
        }
        else
        {
            db.DigioRequests.Add(new DigioRequest
            {
                Id = id, CompanyId = call.CompanyId, ApplicationId = call.ApplicationId, PartyId = call.PartyId, Environment = environment,
                Product = call.Product!, Operation = call.Operation ?? string.Empty, InputFingerprint = call.InputFingerprint,
                RequestedByUserId = call.RequestedByUserId, RequestedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task CompleteRequestAsync(string id, DigioCall call, DigioResult result)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var row = await db.DigioRequests.IgnoreQueryFilters().FirstAsync(r => r.Id == id && r.CompanyId == call.CompanyId);
            var ok = result.Status == DigioStatus.Ok;
            row.Status = ok ? call.SuccessStatus : "Failed";
            row.DigioId = ok && result.Str("id") is { } digioId ? Cut(digioId, 80) : null;
            row.ProviderStatus = ok && (result.Str("status") ?? result.Str("agreement_status") ?? result.Str("state")) is { } ps ? Cut(ps, 60) : null;
            row.ErrorCode = result.ErrorCode;
            row.ErrorMessage = ok || result.Error is null ? null : Cut(result.Error, 300);
            row.ResultJson = ok && result.Data is { } json ? vault.Seal(json.GetRawText()) : null;
            row.CompletedAt = row.Status is "Succeeded" or "Failed" ? DateTime.UtcNow : null;
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // The Digio call already happened; a logging hiccup must not hide its outcome from the caller.
            log.LogError(ex, "Could not update Digio request {RequestId} for company {CompanyId}", id, call.CompanyId);
        }
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
}
