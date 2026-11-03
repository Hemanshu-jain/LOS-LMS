using System.Security.Cryptography;
using LosLms.Data;
using LosLms.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>One line of "who did what" for actions that change tenants or sign-in (company created by the vendor, Google account linked...).</summary>
public class AuditEntry
{
    public long Id { get; set; }

    /// <summary>The company the action concerns, when there is one.</summary>
    public int? CompanyId { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(450)]
    public string? ActorUserId { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(60)]
    public string Action { get; set; } = string.Empty;

    /// <summary>Plain words, never a password or token.</summary>
    [System.ComponentModel.DataAnnotations.MaxLength(400)]
    public string? Detail { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class AuditService(DbContextOptions<LosDbContext> dbOptions)
{
    public async Task WriteAsync(string action, int? companyId, string? actorUserId, string? detail)
    {
        await using var db = new LosDbContext(dbOptions, TenantContext.ForSeeding());
        db.AuditEntries.Add(new AuditEntry
        {
            Action = action, CompanyId = companyId, ActorUserId = actorUserId,
            Detail = detail is { Length: > 400 } ? detail[..400] : detail, CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}

/// <summary>A strong random password for a first sign-in. It meets the Identity policy (10+ chars, upper, lower, digit, symbol) by construction.</summary>
public static class TempPassword
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ", Lower = "abcdefghijkmnopqrstuvwxyz", Digits = "23456789", Symbols = "!@#$%*?-_";

    public static string Generate(int length = 16)
    {
        var all = Upper + Lower + Digits + Symbols;
        var chars = new List<char> { Pick(Upper), Pick(Lower), Pick(Digits), Pick(Symbols) };
        while (chars.Count < length)
        {
            chars.Add(Pick(all));
        }

        // Shuffle so the four required characters are not always first.
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars.ToArray());
    }

    private static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
}

/// <summary>"Continue with Google" is offered only when the owner has put a client id and secret in the environment.</summary>
public sealed class GoogleLoginOptions
{
    public const string Provider = "Google";

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    public bool Enabled => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}

/// <summary>What Google told us about the person, after the OAuth round trip. <see cref="EmailVerified"/> is Google's own email_verified.</summary>
public sealed record GoogleIdentity(string Subject, string Email, bool EmailVerified, string? Name);

/// <summary>
/// The rules for Google sign-in, apart from the HTTP plumbing:
/// sign-in only for a user that already exists (by the linked Google id, else by the SAME email), never creating one inside an existing company;
/// signing up creates a brand-new company whose Admin is the Google user; linking only for the user's own email. Google must say the email is verified.
/// A Google-verified email counts as confirmed, so Brevo's confirmation link is not needed for it (password sign-ups still use it).
/// </summary>
public sealed class GoogleAccountService(UserManager<ApplicationUser> users, CompanyProvisioningService provisioning, AuditService audit)
{
    public const string NoAccount = "No account uses that Google email. Ask your administrator to add you, or sign in with your password.";
    public const string Unverified = "Google has not verified that email address, so it cannot be used to sign in.";
    public const string AlreadyRegistered = "That email is already registered. Sign in with Google or your password instead.";

    public async Task<(ApplicationUser? User, string? Problem)> ResolveForSignInAsync(GoogleIdentity g)
    {
        if (!g.EmailVerified)
        {
            return (null, Unverified);
        }

        var user = await users.FindByLoginAsync(GoogleLoginOptions.Provider, g.Subject) ?? await users.FindByEmailAsync(g.Email);
        if (user is null || !user.IsActive)
        {
            return (null, NoAccount); // the same words for "no such user" and "deactivated": nothing about who has an account leaks
        }

        if (await users.IsLockedOutAsync(user))
        {
            return (null, "This account is locked for a short while after too many failed attempts. Try again later.");
        }

        // The rule is email equality, even for an already linked login: the verified Google email must be this user's email.
        if (!string.Equals(user.Email, g.Email, StringComparison.OrdinalIgnoreCase))
        {
            return (null, NoAccount);
        }

        await EnsureLinkedAndConfirmedAsync(user, g);
        return (user, null);
    }

    /// <summary>Links the signed-in user's own Google account. Only the user's own email can be linked.</summary>
    public async Task<string?> LinkAsync(ApplicationUser user, GoogleIdentity g)
    {
        if (!g.EmailVerified)
        {
            return Unverified;
        }

        if (!string.Equals(user.Email, g.Email, StringComparison.OrdinalIgnoreCase))
        {
            return "That Google account uses a different email address than this user. Use the Google account for " + user.Email + ".";
        }

        if (await users.FindByLoginAsync(GoogleLoginOptions.Provider, g.Subject) is { } other && other.Id != user.Id)
        {
            return "That Google account is already linked to another user.";
        }

        await EnsureLinkedAndConfirmedAsync(user, g);
        await audit.WriteAsync("google.linked", user.CompanyId, user.Id, "Google account linked");
        return null;
    }

    public bool IsLinked(IList<UserLoginInfo> logins) => logins.Any(l => l.LoginProvider == GoogleLoginOptions.Provider);

    /// <summary>A new company whose Admin is this Google user (email verified by Google). The password is random and never shown: they sign in with Google, or use "Forgot password".</summary>
    public async Task<CompanyProvisioningResult> SignUpAsync(GoogleIdentity g, CompanyProfile profile, string displayName)
    {
        if (!g.EmailVerified)
        {
            return CompanyProvisioningResult.Failure(Unverified);
        }

        if (await users.FindByEmailAsync(g.Email) is not null)
        {
            return CompanyProvisioningResult.Failure(AlreadyRegistered);
        }

        var result = await provisioning.ProvisionCompanyAsync(profile, new AdminAccount(displayName, g.Email, TempPassword.Generate()), requireEmailConfirmation: false);
        if (result.Succeeded && result.User is { } admin)
        {
            await users.AddLoginAsync(admin, new UserLoginInfo(GoogleLoginOptions.Provider, g.Subject, "Google"));
            await audit.WriteAsync("company.signup.google", result.CompanyId, admin.Id, $"Company {profile.Name.Trim()} created with Google");
        }

        return result;
    }

    private async Task EnsureLinkedAndConfirmedAsync(ApplicationUser user, GoogleIdentity g)
    {
        if (await users.FindByLoginAsync(GoogleLoginOptions.Provider, g.Subject) is null)
        {
            await users.AddLoginAsync(user, new UserLoginInfo(GoogleLoginOptions.Provider, g.Subject, "Google"));
        }

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true; // Google verified the address: no confirmation email needed
            await users.UpdateAsync(user);
        }
    }
}
