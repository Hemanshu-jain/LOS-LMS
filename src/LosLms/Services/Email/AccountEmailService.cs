using System.Net;
using System.Text;
using LosLms.Data;
using LosLms.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace LosLms.Services;

/// <summary>Where emailed links point: <c>PublicBaseUrl</c> when set (so a proxy can't leak an internal address), else the request's own.</summary>
public static class AccountLinks
{
    public static string BaseUrl(IConfiguration config, HttpRequest? request)
    {
        if (config.Trimmed("PublicBaseUrl") is { } configured)
        {
            return configured.TrimEnd('/');
        }

        return request is null ? string.Empty : $"{request.Scheme}://{request.Host}";
    }
}

/// <summary>The short, plain account emails: HTML plus a text copy, no images and no tracking.</summary>
public static class AccountEmails
{
    public static (string Subject, string Html, string Text) Confirm(string name, string link) => Build(
        "Confirm your email address", name,
        "Thanks for creating your LOS/LMS company. Please confirm your email address to sign in.",
        "Confirm email address", link,
        "If you didn't create this account, you can ignore this email.");

    public static (string Subject, string Html, string Text) Reset(string name, string link) => Build(
        "Reset your LOS/LMS password", name,
        "We received a request to reset the password for your account. The link works for 24 hours and only once.",
        "Choose a new password", link,
        "If you didn't ask for this, you can ignore this email: your password stays as it is.");

    private static (string, string, string) Build(string subject, string name, string intro, string button, string link, string outro)
    {
        var who = string.IsNullOrWhiteSpace(name) ? "Hello" : $"Hello {name.Trim()}";
        var text = $"{who},\n\n{intro}\n\n{button}: {link}\n\n{outro}\n\nLOS/LMS";
        static string e(string s) => WebUtility.HtmlEncode(s);
        var html = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#1d2733;max-width:480px">
              <p style="font-size:11px;letter-spacing:.12em;color:#6b7686;margin:0 0 12px">LOS/LMS</p>
              <p>{e(who)},</p>
              <p>{e(intro)}</p>
              <p><a href="{e(link)}" style="display:inline-block;background:#1f3a5f;color:#ffffff;padding:10px 18px;text-decoration:none;border-radius:3px">{e(button)}</a></p>
              <p style="font-size:12px;color:#6b7686">Or paste this address into your browser:<br>{e(link)}</p>
              <p style="font-size:12px;color:#6b7686">{e(outro)}</p>
            </div>
            """;
        return (subject, html, text);
    }
}

/// <summary>
/// Email confirmation and forgotten-password flows on top of Identity's tokens. Every public entry point is safe to call
/// with anything a visitor can type: unknown accounts, bad or tampered tokens and rate-limit hits all end quietly, so
/// the pages can give one identical answer (no account enumeration).
/// </summary>
public sealed class AccountEmailService(
    UserManager<ApplicationUser> users,
    IEmailSender mail,
    PasswordVault vault,
    SendThrottle throttle,
    ILogger<AccountEmailService> log)
{
    /// <summary>
    /// Whether this account must confirm its email before it may sign in: a new self-sign-up owner who hasn't yet. Existing
    /// users were all marked confirmed by a migration, staff an Admin creates are confirmed, and the vendor's master account
    /// is always exempt.
    /// </summary>
    public static bool RequiresConfirmation(ApplicationUser user) =>
        !user.EmailConfirmed && user.IsActive && user.Id != MasterAccount.Id;

    private static string Encode(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static string? Decode(string? code)
    {
        try
        {
            return string.IsNullOrWhiteSpace(code) ? null : Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    // ---- email confirmation ----

    public async Task SendConfirmationAsync(ApplicationUser user, string baseUrl)
    {
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        var link = $"{baseUrl}/account/confirm-email?userId={Uri.EscapeDataString(user.Id)}&code={Encode(token)}";
        var (subject, html, text) = AccountEmails.Confirm(user.DisplayName, link);
        await mail.SendAsync(user.Email!, subject, html, text);
    }

    /// <summary>The "Resend link" action. Silent for unknown, already-confirmed or deactivated accounts and over the hourly cap.</summary>
    public async Task ResendConfirmationAsync(string? email, string baseUrl, DateTime? nowUtc = null)
    {
        if (await FindActiveAsync(email) is not { EmailConfirmed: false } user
            || !throttle.Allow("confirm:" + user.Id, nowUtc ?? DateTime.UtcNow))
        {
            return;
        }

        await SendConfirmationAsync(user, baseUrl);
    }

    /// <summary>True when the link's token is genuine for that user (tampered, foreign or stale links are false).</summary>
    public async Task<bool> ConfirmEmailAsync(string? userId, string? code)
    {
        if (string.IsNullOrWhiteSpace(userId) || Decode(code) is not { } token || await users.FindByIdAsync(userId) is not { } user)
        {
            return false;
        }

        return (await users.ConfirmEmailAsync(user, token)).Succeeded;
    }

    // ---- forgot password ----

    /// <summary>
    /// Sends a reset link when the address belongs to an active, confirmed account. Never says whether it did. The vendor's
    /// master account is excluded: its recovery is not an inbox.
    /// </summary>
    public async Task RequestPasswordResetAsync(string? email, string baseUrl, DateTime? nowUtc = null)
    {
        if (await FindActiveAsync(email) is not { EmailConfirmed: true } user
            || user.Id == MasterAccount.Id
            || !throttle.Allow("reset:" + user.Id, nowUtc ?? DateTime.UtcNow))
        {
            return;
        }

        var token = await users.GeneratePasswordResetTokenAsync(user);
        var link = $"{baseUrl}/account/reset-password?userId={Uri.EscapeDataString(user.Id)}&code={Encode(token)}";
        var (subject, html, text) = AccountEmails.Reset(user.DisplayName, link);
        await mail.SendAsync(user.Email!, subject, html, text);
    }

    /// <summary>
    /// Sets the new password if the token is genuine, unused and unexpired. Identity applies the password rules and rotates the
    /// security stamp (which also kills the token and every old session). The saved copy in User Management is refreshed the
    /// same way the change-password page does, and a lockout from earlier wrong guesses is cleared.
    /// </summary>
    public async Task<IdentityResult> ResetPasswordAsync(string? userId, string? code, string newPassword)
    {
        var invalid = IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "This link is invalid or has expired. Ask for a new one." });
        if (string.IsNullOrWhiteSpace(userId) || Decode(code) is not { } token
            || await users.FindByIdAsync(userId) is not { IsActive: true } user)
        {
            return invalid;
        }

        var result = await users.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Code == "InvalidToken") ? invalid : result;
        }

        vault.Store(user.Id, newPassword);
        if (user.MustChangePassword)
        {
            user.MustChangePassword = false;
            await users.UpdateAsync(user);
        }

        await users.SetLockoutEndDateAsync(user, null);
        await users.ResetAccessFailedCountAsync(user);
        log.LogInformation("Password reset for user {UserId}.", user.Id);
        return result;
    }

    private async Task<ApplicationUser?> FindActiveAsync(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null
            : await users.FindByEmailAsync(email.Trim()) is { IsActive: true } user ? user : null;
}
