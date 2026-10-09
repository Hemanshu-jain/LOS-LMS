using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace LosLms.Services;

/// <summary>
/// The interface the partner data API builds against: scheme <c>PartnerBearer</c> reads
/// <c>Authorization: Bearer lct_…</c> and sets the claims below; policies <c>loans:read</c> /
/// <c>loans:write</c> require the matching <c>scope</c> claim. Register once with <c>AddPartnerAuth()</c>.
/// </summary>
public static class PartnerAuth
{
    public const string Scheme = "PartnerBearer";
    public const string CompanyIdClaim = "company_id";
    public const string ConnectionIdClaim = "connection_id";
    public const string PartnerNameClaim = "partner_name";
    public const string ScopeClaim = "scope";

    public static IServiceCollection AddPartnerAuth(this IServiceCollection services)
    {
        services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, PartnerBearerHandler>(Scheme, null);
        services.AddAuthorizationBuilder()
            .AddPolicy(PartnerOAuthService.ScopeRead, p => p.AddAuthenticationSchemes(Scheme).RequireClaim(ScopeClaim, PartnerOAuthService.ScopeRead))
            .AddPolicy(PartnerOAuthService.ScopeWrite, p => p.AddAuthenticationSchemes(Scheme).RequireClaim(ScopeClaim, PartnerOAuthService.ScopeWrite));
        return services;
    }
}

/// <summary>No bearer token (or not an <c>lct_</c> one) = no result, so other schemes still get their turn.</summary>
public sealed class PartnerBearerHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, PartnerOAuthService oauth)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private PartnerBlock? _blocked;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer " + PartnerOAuthService.AccessPrefix, StringComparison.Ordinal))
        {
            return AuthenticateResult.NoResult();
        }

        var identity = await oauth.AuthenticateAsync(header["Bearer ".Length..].Trim(), DateTime.UtcNow);
        if (identity is null)
        {
            return AuthenticateResult.Fail("Invalid or expired token.");
        }

        if (identity.Blocked is { } blocked)
        {
            _blocked = blocked;
            return AuthenticateResult.Fail(blocked.Error);
        }

        var claims = new List<Claim>
        {
            new(PartnerAuth.CompanyIdClaim, identity.CompanyId.ToString(), ClaimValueTypes.Integer32),
            new(PartnerAuth.ConnectionIdClaim, identity.ConnectionId),
            new(PartnerAuth.PartnerNameClaim, identity.PartnerName),
        };
        claims.AddRange(identity.Scopes.Select(s => new Claim(PartnerAuth.ScopeClaim, s)));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, PartnerAuth.Scheme));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, PartnerAuth.Scheme));
    }

    // A valid token for a company that is switched off gets a clear 403; every other failure is the plain 401.
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (_blocked is not { } blocked)
        {
            await base.HandleChallengeAsync(properties);
            return;
        }

        Response.StatusCode = StatusCodes.Status403Forbidden;
        await Response.WriteAsJsonAsync(new { error = blocked.Error, message = blocked.Message, details = Array.Empty<object>() });
    }
}
