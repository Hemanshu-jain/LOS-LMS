using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;

namespace LosLms.Services;

/// <summary>
/// POST /oauth/device_authorization, /oauth/token, /oauth/revoke — form-urlencoded in, JSON out, the same
/// wire contract as Handoff's. A terminal branch placed before authentication, maintenance, the company-setup
/// redirect and antiforgery (partner software has no cookie, no company session and no antiforgery token).
/// Rate limiting is the global limiter's, keyed on /oauth.
/// </summary>
public static class PartnerOAuthEndpoints
{
    public const string PathPrefix = "/oauth";

    public static void UsePartnerOAuth(this WebApplication app) =>
        app.MapWhen(c => c.Request.Path.StartsWithSegments(PathPrefix), branch => branch.Run(HandleAsync));

    public static async Task HandleAsync(HttpContext context)
    {
        var response = context.Response;
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";

        var path = context.Request.Path;
        var isDevice = path.StartsWithSegments("/oauth/device_authorization");
        var isToken = path.StartsWithSegments("/oauth/token");
        var isRevoke = path.StartsWithSegments("/oauth/revoke");
        if (!HttpMethods.IsPost(context.Request.Method) || !(isDevice || isToken || isRevoke))
        {
            await Json(context, StatusCodes.Status404NotFound, new { error = "not_found", error_description = "Unknown endpoint." });
            return;
        }

        var form = await ReadFormAsync(context);
        if (form is null)
        {
            await Json(context, StatusCodes.Status413PayloadTooLarge, new { error = "invalid_request", error_description = "Request body too large." });
            return;
        }

        string? F(string name) => form.GetValueOrDefault(name);
        var oauth = context.RequestServices.GetRequiredService<PartnerOAuthService>();
        var now = DateTime.UtcNow;
        try
        {
            if (isDevice)
            {
                await Json(context, 200, await oauth.StartDeviceAsync(F("client_id"), F("scope"), LinkUrl(context), now, ClientIp.Of(context)));
            }
            else if (isToken)
            {
                await Json(context, 200, await oauth.TokenAsync(
                    F("grant_type"), F("client_id"), F("device_code"), F("refresh_token"), now));
            }
            else if (await oauth.RevokeAsync(F("client_id"), F("token"), now))
            {
                await Json(context, 200, new { });
            }
            else
            {
                await Json(context, 401, new { error = "invalid_client", error_description = "Send your client_id." });
            }
        }
        catch (OAuthException e)
        {
            await Json(context, e.Status, new { error = e.Name, error_description = e.Message });
        }
    }

    // PublicBaseUrl wins when set, so a proxy or cloud host can't leak an internal address into verification_uri;
    // otherwise the request's own scheme + host.
    private static string LinkUrl(HttpContext context)
    {
        var configured = context.RequestServices.GetService<IConfiguration>()?["PublicBaseUrl"]?.Trim().TrimEnd('/');
        return $"{(string.IsNullOrEmpty(configured) ? $"{context.Request.Scheme}://{context.Request.Host}" : configured)}/link";
    }

    private const int MaxBodyBytes = 20_000;

    // Reads at most MaxBodyBytes of an application/x-www-form-urlencoded body, whether or not the caller sent a
    // Content-Length (a chunked request has none). null = too large. Anything that isn't such a body reads as
    // empty, so the endpoint answers with its normal invalid_request / invalid_client rather than throwing.
    private static async Task<Dictionary<string, string>?> ReadFormAsync(HttpContext context)
    {
        var form = new Dictionary<string, string>();
        if (context.Request.ContentType?.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) != true)
        {
            return form;
        }

        var buffer = new byte[MaxBodyBytes + 1];
        var read = 0;
        try
        {
            int n;
            while (read < buffer.Length && (n = await context.Request.Body.ReadAsync(buffer.AsMemory(read))) > 0)
            {
                read += n;
            }
        }
        catch (IOException)
        {
            return form; // the caller hung up mid-body
        }

        if (read > MaxBodyBytes)
        {
            return null;
        }

        foreach (var (key, value) in QueryHelpers.ParseQuery(Encoding.UTF8.GetString(buffer, 0, read)))
        {
            form[key] = value.ToString();
        }

        return form;
    }

    private static Task Json(HttpContext context, int status, object body)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(body, (JsonSerializerOptions?)null);
    }
}

/// <summary>Deletes device codes an hour past expiry: at boot, then hourly.</summary>
public sealed class PartnerOAuthPurgeService(IServiceScopeFactory scopes, ILogger<PartnerOAuthPurgeService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var purged = await scope.ServiceProvider.GetRequiredService<PartnerOAuthService>().PurgeAsync(DateTime.UtcNow);
                if (purged > 0)
                {
                    log.LogInformation("Purged {Count} expired partner device codes", purged);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.LogWarning(e, "Partner device-code purge failed; will retry next hour");
            }
        }
        while (await WaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken token)
    {
        try
        {
            return await timer.WaitForNextTickAsync(token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
