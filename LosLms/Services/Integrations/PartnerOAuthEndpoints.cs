using System.Text.Json;
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
        var oauth = context.RequestServices.GetRequiredService<PartnerOAuthService>();
        var now = DateTime.UtcNow;
        try
        {
            if (isDevice)
            {
                await Json(context, 200, await oauth.StartDeviceAsync(form["client_id"], form["scope"], LinkUrl(context), now));
            }
            else if (isToken)
            {
                await Json(context, 200, await oauth.TokenAsync(
                    form["grant_type"], form["client_id"], form["device_code"], form["refresh_token"], now));
            }
            else if (await oauth.RevokeAsync(form["client_id"], form["token"], now))
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

    // Anything that isn't a small form-urlencoded body reads as empty, so the endpoint answers with its normal
    // invalid_request / invalid_client rather than throwing.
    private static async Task<IFormCollection> ReadFormAsync(HttpContext context)
    {
        if (context.Request.HasFormContentType && context.Request.ContentLength is null or <= 20_000)
        {
            try
            {
                return await context.Request.ReadFormAsync();
            }
            catch (Exception e) when (e is InvalidDataException or InvalidOperationException or IOException)
            {
                // fall through to the empty form
            }
        }

        return FormCollection.Empty;
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
