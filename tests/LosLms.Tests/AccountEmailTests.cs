using System.Security.Claims;
using System.Text.RegularExpressions;
using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LosLms.Tests;

/// <summary>
/// Email confirmation for new sign-ups and the forgotten-password flow, on real Identity tokens and a real (SQLite) user
/// store. A capturing sender stands in for SMTP, so the tests read the very link a customer would be emailed.
/// </summary>
public sealed class AccountEmailTests : IDisposable
{
    private const string Base = "https://app.test";
    private const string Password = "Str0ng@Pass1";
    private const string NewPassword = "N3w@Password9";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<LosDbContext> _options;
    private readonly string _vaultDir = Path.Combine(Path.GetTempPath(), "los-vault-" + Guid.NewGuid().ToString("N"));
    private ServiceProvider _provider;

    public AccountEmailTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite(_connection).Options;
        using (var db = new LosDbContext(_options, TenantContext.ForSeeding()))
        {
            db.Database.EnsureCreated();
        }

        _provider = Build(null);
        using var scope = _provider.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { TenantContext.StaffRole, TenantContext.AdminRole, TenantContext.SuperAdminRole })
        {
            roles.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
        }
    }

    private ServiceProvider Build(TimeSpan? tokenLifetime)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_options);
        services.AddDataProtection().UseEphemeralDataProtectionProvider(); // token providers need it (the web host adds it itself)
        services.AddScoped(_ => new LosDbContext(_options, AnonymousTenant()));
        services.AddIdentityCore<ApplicationUser>(o =>
            {
                o.Password.RequiredLength = 10;
                o.Password.RequireNonAlphanumeric = true;
                o.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<LosDbContext>()
            .AddDefaultTokenProviders();
        if (tokenLifetime is { } life)
        {
            services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = life);
        }

        services.AddSingleton<IWebHostEnvironment>(new FakeEnvironment(_vaultDir));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<PasswordVault>();
        services.AddSingleton<Capture>();
        services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<Capture>());
        services.AddSingleton<SendThrottle>();
        services.AddScoped<CompanyProvisioningService>();
        services.AddScoped<AccountEmailService>();
        return services.BuildServiceProvider();
    }

    private sealed record Sent(string To, string Subject, string Html, string Text);

    private sealed class Capture : IEmailSender
    {
        public List<Sent> Messages { get; } = [];

        public bool IsConfigured => true;

        public Task<bool> SendAsync(string to, string subject, string html, string text)
        {
            Messages.Add(new Sent(to, subject, html, text));
            return Task.FromResult(true);
        }
    }

    private (AccountEmailService Svc, UserManager<ApplicationUser> Users, Capture Mail, IServiceScope Scope) Open(ServiceProvider? provider = null)
    {
        var scope = (provider ?? _provider).CreateScope();
        return (scope.ServiceProvider.GetRequiredService<AccountEmailService>(),
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            scope.ServiceProvider.GetRequiredService<Capture>(), scope);
    }

    private static (string UserId, string Code) LinkParts(Sent mail)
    {
        var m = Regex.Match(mail.Text, @"\?userId=([^&\s]+)&code=([^\s]+)");
        Assert.True(m.Success, "the email has a link");
        return (Uri.UnescapeDataString(m.Groups[1].Value), m.Groups[2].Value);
    }

    private async Task<ApplicationUser> SignUpAsync(AccountEmailService svc, string email, bool requireConfirmation = true, ServiceProvider? provider = null)
    {
        using var scope = (provider ?? _provider).CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<CompanyProvisioningService>().ProvisionCompanyAsync(
            new CompanyProfile("Acme " + email, email, "9000000001", "1 MG Road"),
            new AdminAccount("Asha <b>K</b>", email, Password), requireConfirmation);
        Assert.True(result.Succeeded);
        return result.User!;
    }

    // ---- confirmation ----

    [Fact]
    public async Task NewSelfSignUp_IsUnconfirmed_AndMustConfirmBeforeSignIn()
    {
        var (svc, users, mail, scope) = Open();
        using var _ = scope;
        var user = await SignUpAsync(svc, "owner@acme.test");

        var stored = await users.FindByEmailAsync("owner@acme.test");
        Assert.False(stored!.EmailConfirmed);
        Assert.True(AccountEmailService.RequiresConfirmation(stored)); // what the sign-in page checks
        Assert.True(await users.CheckPasswordAsync(stored, Password)); // right password, still blocked until confirmed

        await svc.SendConfirmationAsync(stored, Base);
        var email = Assert.Single(mail.Messages);
        Assert.Equal("owner@acme.test", email.To);
        Assert.StartsWith(Base + "/account/confirm-email?userId=", Regex.Match(email.Text, @"https://\S+").Value);
    }

    [Fact]
    public async Task ConfirmLink_Works_AndABadOneFails()
    {
        var (svc, users, mail, scope) = Open();
        using var _ = scope;
        var user = await SignUpAsync(svc, "owner@acme.test");
        var other = await SignUpAsync(svc, "other@acme.test");
        await svc.SendConfirmationAsync(user, Base);
        var (userId, code) = LinkParts(mail.Messages.Single());

        Assert.False(await svc.ConfirmEmailAsync(userId, code[..^2] + "AA"));   // tampered token
        Assert.False(await svc.ConfirmEmailAsync(other.Id, code));              // someone else's account
        Assert.False(await svc.ConfirmEmailAsync(userId, "not*base64!"));       // garbage
        Assert.False(await svc.ConfirmEmailAsync(userId, null));
        Assert.False(await svc.ConfirmEmailAsync(null, code));
        Assert.False((await users.FindByIdAsync(userId))!.EmailConfirmed);

        Assert.True(await svc.ConfirmEmailAsync(userId, code));
        var confirmed = (await users.FindByIdAsync(userId))!;
        Assert.True(confirmed.EmailConfirmed);
        Assert.False(AccountEmailService.RequiresConfirmation(confirmed));
        Assert.True(await svc.ConfirmEmailAsync(userId, code)); // a mail scanner opening it first doesn't spoil it
    }

    [Fact]
    public async Task ExistingUsers_AndAdminCreatedStaff_AreNotAffected()
    {
        var (svc, users, _, scope) = Open();
        using var _ = scope;
        await SignUpAsync(svc, "demo@acme.test", requireConfirmation: false); // the demo seeder / trusted callers
        Assert.False(AccountEmailService.RequiresConfirmation((await users.FindByEmailAsync("demo@acme.test"))!));

        var master = new ApplicationUser { Id = "usr-master", UserName = "master@x.test", Email = "master@x.test", EmailConfirmed = false, IsActive = true };
        Assert.False(AccountEmailService.RequiresConfirmation(master)); // the vendor account is always exempt

        // The migration marks everyone who exists today as confirmed.
        await using var db = new LosDbContext(_options, TenantContext.ForSeeding());
        db.Users.Add(new ApplicationUser { Id = "old-1", UserName = "old@x.test", NormalizedUserName = "OLD@X.TEST", Email = "old@x.test", EmailConfirmed = false, IsActive = true });
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync(LosLms.Migrations.ConfirmExistingUsersEmail.ConfirmEveryoneSql);
        Assert.True((await db.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Id == "old-1")).EmailConfirmed);
    }

    [Fact]
    public async Task Resend_OnlyForUnconfirmed_AndCappedPerHour()
    {
        var (svc, _, mail, scope) = Open();
        using var _ = scope;
        var user = await SignUpAsync(svc, "owner@acme.test");
        await SignUpAsync(svc, "done@acme.test", requireConfirmation: false);
        var now = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);

        await svc.ResendConfirmationAsync("done@acme.test", Base, now);    // already confirmed: nothing
        await svc.ResendConfirmationAsync("nobody@acme.test", Base, now);  // unknown: nothing, no error
        Assert.Empty(mail.Messages);

        for (var i = 0; i < 5; i++) { await svc.ResendConfirmationAsync(user.Email, Base, now.AddMinutes(i)); }
        Assert.Equal(3, mail.Messages.Count);                               // the cap

        await svc.ResendConfirmationAsync(user.Email, Base, now.AddMinutes(61));
        Assert.Equal(4, mail.Messages.Count);                               // an hour later it is allowed again
    }

    // ---- forgot password ----

    [Fact]
    public async Task ForgotPassword_SendsOnlyForAConfirmedActiveAccount_AndNeverSaysWhich()
    {
        var (svc, users, mail, scope) = Open();
        using var _ = scope;
        await SignUpAsync(svc, "known@acme.test", requireConfirmation: false);
        await SignUpAsync(svc, "unconfirmed@acme.test");
        await SignUpAsync(svc, "off@acme.test", requireConfirmation: false);
        var off = (await users.FindByEmailAsync("off@acme.test"))!;   // this scope's own copy, not the one provisioning made
        off.IsActive = false;
        await users.UpdateAsync(off);

        // Every call returns the same nothing, so a page built on it can only ever give one answer.
        foreach (var email in new[] { "known@acme.test", "nobody@acme.test", "unconfirmed@acme.test", "off@acme.test", "  ", null })
        {
            await svc.RequestPasswordResetAsync(email, Base);
        }

        var sent = Assert.Single(mail.Messages);
        Assert.Equal("known@acme.test", sent.To);
        Assert.Contains("/account/reset-password?userId=", sent.Text);
    }

    [Fact]
    public async Task ForgotPassword_NeverEmailsTheMasterAccount()
    {
        var (svc, users, mail, scope) = Open();
        using var _ = scope;
        await users.CreateAsync(new ApplicationUser { Id = "usr-master", UserName = "master@x.test", Email = "master@x.test", EmailConfirmed = true, IsActive = true }, Password);

        await svc.RequestPasswordResetAsync("master@x.test", Base);

        Assert.Empty(mail.Messages);
    }

    [Fact]
    public async Task Reset_WithAValidLink_ChangesThePassword_RotatesTheStamp_AndRefreshesTheVault()
    {
        var (svc, users, mail, scope) = Open();
        using var _ = scope;
        var user = await SignUpAsync(svc, "known@acme.test", requireConfirmation: false);
        var stampBefore = (await users.FindByIdAsync(user.Id))!.SecurityStamp;
        await users.SetLockoutEndDateAsync((await users.FindByIdAsync(user.Id))!, DateTimeOffset.UtcNow.AddMinutes(10)); // locked out by earlier wrong guesses
        await svc.RequestPasswordResetAsync("known@acme.test", Base);
        var (userId, code) = LinkParts(mail.Messages.Single());

        var result = await svc.ResetPasswordAsync(userId, code, NewPassword);

        Assert.True(result.Succeeded);
        var after = (await users.FindByIdAsync(userId))!;
        Assert.True(await users.CheckPasswordAsync(after, NewPassword));
        Assert.False(await users.CheckPasswordAsync(after, Password));
        Assert.NotEqual(stampBefore, after.SecurityStamp);                       // every old session ends
        Assert.False(await users.IsLockedOutAsync(after));
        Assert.Equal(NewPassword, _provider.GetRequiredService<PasswordVault>().TryReveal(userId));
    }

    [Fact]
    public async Task Reset_ALinkWorksOnce_AndATamperedOrWeakAttemptDoesNotSpendIt()
    {
        var (svc, users, mail, scope) = Open();
        using var _ = scope;
        var user = await SignUpAsync(svc, "known@acme.test", requireConfirmation: false);
        await svc.RequestPasswordResetAsync("known@acme.test", Base);
        var (userId, code) = LinkParts(mail.Messages.Single());

        Assert.Equal("InvalidToken", (await svc.ResetPasswordAsync(userId, code[..^2] + "AA", NewPassword)).Errors.Single().Code);
        var weak = await svc.ResetPasswordAsync(userId, code, "short");
        Assert.False(weak.Succeeded);
        Assert.DoesNotContain(weak.Errors, e => e.Code == "InvalidToken");       // the password rules, not "bad link"

        Assert.True((await svc.ResetPasswordAsync(userId, code, NewPassword)).Succeeded);
        var reused = await svc.ResetPasswordAsync(userId, code, "An0ther@Password7");
        Assert.Equal("InvalidToken", reused.Errors.Single().Code);               // used already
        Assert.True(await users.CheckPasswordAsync((await users.FindByIdAsync(userId))!, NewPassword));
        Assert.Equal("InvalidToken", (await svc.ResetPasswordAsync("no-such-user", code, NewPassword)).Errors.Single().Code);
    }

    [Fact]
    public async Task Reset_AnExpiredLink_Fails()
    {
        using var shortLived = Build(TimeSpan.FromMilliseconds(300));
        var (svc, _, mail, scope) = Open(shortLived);
        using var _ = scope;
        await SignUpAsync(svc, "known@acme.test", requireConfirmation: false, provider: shortLived);
        await svc.RequestPasswordResetAsync("known@acme.test", Base);
        var (userId, code) = LinkParts(mail.Messages.Single());

        await Task.Delay(900);

        Assert.Equal("InvalidToken", (await svc.ResetPasswordAsync(userId, code, NewPassword)).Errors.Single().Code);
    }

    // ---- sending ----

    [Fact]
    public async Task WithNoSmtpConfigured_NothingThrows_AndTheEmailIsLogged()
    {
        var sender = new SmtpEmailSender(new ConfigurationBuilder().Build(), NullLogger<SmtpEmailSender>.Instance);

        Assert.False(sender.IsConfigured);
        Assert.False(await sender.SendAsync("a@b.test", "Hi", "<p>x</p>", "x")); // logged, not sent, not thrown
    }

    [Fact]
    public async Task AnUnreachableSmtpServer_IsLoggedNotThrown()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Smtp:Host"] = " 127.0.0.1 ", ["Smtp:Port"] = "1", ["Smtp:From"] = "no-reply@x.test", ["Smtp:User"] = "u", ["Smtp:Password"] = "p",
        }).Build();
        var sender = new SmtpEmailSender(config, NullLogger<SmtpEmailSender>.Instance);

        Assert.True(sender.IsConfigured);
        Assert.False(await sender.SendAsync("a@b.test", "Hi", "<p>x</p>", "x"));
    }

    [Fact]
    public void Emails_AreShortPlain_AndEncodeWhatTheyEmbed()
    {
        var (subject, html, text) = AccountEmails.Confirm("Asha <script>alert(1)</script>", "https://app.test/account/confirm-email?userId=u&code=c");

        Assert.Equal("Confirm your email address", subject);
        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);   // no tracking pixel
        Assert.Contains("confirm-email?userId=u&amp;code=c", html);                // the link, HTML-encoded
        Assert.Contains("confirm-email?userId=u&code=c", text);
        Assert.Equal("https://x.test", AccountLinks.BaseUrl(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["PublicBaseUrl"] = " https://x.test/ " }).Build(), null));
    }

    // ---- plumbing ----

    private static TenantContext AnonymousTenant()
    {
        var tenant = new TenantContext(
            new FakeAuth(new ClaimsPrincipal(new ClaimsIdentity())), new HttpContextAccessor(), new ActingCompanyStore());
        tenant.EnsureLoadedAsync().GetAwaiter().GetResult();
        return tenant;
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
        try { Directory.Delete(_vaultDir, recursive: true); } catch { /* best effort */ }
    }

    private sealed class FakeAuth(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
    }

    private sealed class FakeEnvironment(string contentRoot) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "LosLms.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = contentRoot;
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
