using Microsoft.AspNetCore.Authentication.Cookies;

namespace LosLms.Services;

/// <summary>
/// The sign-in cookie's lifetimes. "Remember me" gives a persistent cookie that lasts 7 days and slides (every visit pushes it out) and survives
/// a browser restart; without it the cookie is a session cookie that slides over 12 hours, so a normal working day never signs anyone out.
/// ASP.NET's cookie handler renews a cookie by the lifetime it was issued with, so setting the persistent lifetime when it is issued is enough.
/// </summary>
public static class AuthCookie
{
    public static readonly TimeSpan RememberedFor = TimeSpan.FromDays(7);
    public static readonly TimeSpan SessionFor = TimeSpan.FromHours(12);

    public static void Configure(CookieAuthenticationOptions options)
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        // A signed-in user who fails a role check is NOT sent to the sign-in form. They are already
        // signed in; a login page would read as "your session expired" rather than "you lack the role".
        options.AccessDeniedPath = "/account/denied";
        options.ExpireTimeSpan = SessionFor;
        options.SlidingExpiration = true;

        // Also runs when the handler re-issues the cookie (sliding renewal, security-stamp refresh): a remembered sign-in stays remembered.
        var signingIn = options.Events.OnSigningIn;
        options.Events.OnSigningIn = async context =>
        {
            await signingIn(context);
            if (context.Properties.IsPersistent)
            {
                context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.Add(RememberedFor);
            }
        };
    }
}
