using System.Security.Claims;
using LosLms.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LosLms.Services;

/// <summary>
/// The HTTP side of "Continue with Google". Three intents share one Google round trip:
/// signin (an existing user), signup (a new company, finished on /account/register/google) and link (the signed-in user's own account).
/// Google's answer is held in Identity's short-lived external cookie only until the callback has used it.
/// </summary>
public static class GoogleLoginEndpoints
{
    public static void MapGoogleLogin(this WebApplication app)
    {
        app.MapGet("/account/external/google", (HttpContext http, IOptions<GoogleLoginOptions> google, IOptions<WebOptions> web, string? intent, string? returnUrl) =>
        {
            if (!google.Value.Enabled)
            {
                return Results.NotFound();
            }

            intent = intent is "signup" or "link" ? intent : "signin";
            if (intent == "link" && http.User.Identity?.IsAuthenticated != true)
            {
                return Results.Redirect("/account/login");
            }

            if (intent == "signup" && !web.Value.AllowSelfRegistration)
            {
                return Results.Redirect("/account/login");
            }

            var back = !string.IsNullOrWhiteSpace(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") ? returnUrl : "";
            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = $"/account/external/callback?intent={intent}&returnUrl={Uri.EscapeDataString(back)}" },
                [GoogleLoginOptions.Provider]);
        }).AllowAnonymous();

        app.MapGet("/account/external/callback", async (
            HttpContext http, SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users, GoogleAccountService accounts,
            ActingCompanyStore acting, string? intent, string? returnUrl) =>
        {
            var external = await http.AuthenticateAsync(IdentityConstants.ExternalScheme);
            if (!external.Succeeded || Identity(external.Principal) is not { } google)
            {
                await http.SignOutAsync(IdentityConstants.ExternalScheme);
                return Results.Redirect("/account/login?notice=google-failed");
            }

            if (intent == "signup")
            {
                // Keep the external cookie: /account/register/google reads it to finish the company profile.
                return google.EmailVerified ? Results.Redirect("/account/register/google") : Results.Redirect("/account/login?notice=google-unverified");
            }

            await http.SignOutAsync(IdentityConstants.ExternalScheme);

            if (intent == "link")
            {
                var me = await users.GetUserAsync(http.User);
                if (me is null)
                {
                    return Results.Redirect("/account/login");
                }

                var problem = await accounts.LinkAsync(me, google);
                return Results.Redirect(problem is null ? "/account/change-password?notice=google-linked" : "/account/change-password?notice=google-link-failed");
            }

            var (user, why) = await accounts.ResolveForSignInAsync(google);
            if (user is null)
            {
                return Results.Redirect(why == GoogleAccountService.Unverified ? "/account/login?notice=google-unverified" : "/account/login?notice=google-nouser");
            }

            await signIn.SignInAsync(user, isPersistent: false);
            if (await users.IsInRoleAsync(user, TenantContext.SuperAdminRole))
            {
                acting.Clear(user.Id);
                return Results.Redirect("/companies");
            }

            return Results.Redirect(!string.IsNullOrWhiteSpace(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") ? returnUrl : "/applications");
        }).AllowAnonymous();
    }

    /// <summary>The Google identity out of the external cookie's principal; null when it carries no id or email.</summary>
    public static GoogleIdentity? Identity(ClaimsPrincipal? p)
    {
        var sub = p?.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = p?.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(sub) || string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var verified = string.Equals(p!.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase);
        return new GoogleIdentity(sub, email.Trim(), verified, p.FindFirstValue(ClaimTypes.Name));
    }
}
