using System.Text.Json;
using Microsoft.Extensions.Options;

namespace LosLms.Services;

/// <summary>
/// Cloudflare Turnstile keys. Bot protection on the sign-in form is OFF until both are configured under
/// <c>Security:Turnstile</c> (appsettings or server-config) — so a fresh install and every LAN-only
/// deployment behave exactly as before. Set both keys once the app is reachable over the public tunnel.
/// </summary>
public sealed class TurnstileOptions
{
    public const string Section = "Security:Turnstile";

    /// <summary>Public site key, rendered into the widget. Safe to ship to the browser.</summary>
    public string? SiteKey { get; set; }

    /// <summary>Secret key, used server-side to verify the token. Never rendered.</summary>
    public string? SecretKey { get; set; }

    public bool Enabled => !string.IsNullOrWhiteSpace(SiteKey) && !string.IsNullOrWhiteSpace(SecretKey);
}

/// <summary>
/// Verifies a Turnstile token with Cloudflare's siteverify API. Fails closed: when Turnstile is
/// configured, any missing/invalid token or a verification error blocks the sign-in. When it is not
/// configured, verification is a no-op that always passes.
/// </summary>
public sealed class TurnstileVerifier(
    IHttpClientFactory httpFactory, IOptions<TurnstileOptions> options, ILogger<TurnstileVerifier> logger)
{
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public TurnstileOptions Options => options.Value;

    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct = default)
    {
        if (!Options.Enabled)
        {
            return true; // not configured — nothing to enforce
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var form = new Dictionary<string, string>
            {
                ["secret"] = Options.SecretKey!,
                ["response"] = token,
            };
            if (!string.IsNullOrWhiteSpace(remoteIp))
            {
                form["remoteip"] = remoteIp;
            }

            var http = httpFactory.CreateClient();
            using var response = await http.PostAsync(VerifyUrl, new FormUrlEncodedContent(form), ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Turnstile siteverify returned HTTP {Status}.", (int)response.StatusCode);
                return false;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement.TryGetProperty("success", out var success) && success.GetBoolean();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Turnstile verification failed; treating the challenge as unsolved.");
            return false;
        }
    }
}
