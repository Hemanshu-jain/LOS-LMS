using System.Security.Claims;
using System.Text.RegularExpressions;
using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace LosLms.Tests;

/// <summary>
/// "Continue with Google" rules and the vendor's Create company: Google signs in only an EXISTING user with the same verified email, never creates
/// one inside a company; signing up makes a new company's Admin; linking is for the user's own email; a vendor-created admin must change the
/// temporary password; every one of those writes an audit row.
/// </summary>
public sealed class GoogleAccountTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<LosDbContext> _options;
    private readonly ServiceProvider _provider;
    private readonly string _vaultDir = Path.Combine(Path.GetTempPath(), "los-vault-" + Guid.NewGuid().ToString("N"));

    public GoogleAccountTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite(_connection).Options;
        using (var db = new LosDbContext(_options, Tenant(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "s"), new Claim(ClaimTypes.Role, TenantContext.SuperAdminRole)], "T")))))
        {
            db.Database.EnsureCreated();
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_options);
        services.AddScoped(_ => new LosDbContext(_options, Tenant(new ClaimsPrincipal(new ClaimsIdentity()))));
        services.AddIdentityCore<ApplicationUser>(o =>
            {
                o.Password.RequiredLength = 10;
                o.Password.RequireNonAlphanumeric = true;
                o.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<LosDbContext>();
        services.AddSingleton<IWebHostEnvironment>(new Env(_vaultDir));
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        services.AddSingleton<PasswordVault>();
        services.AddSingleton<AuditService>();
        services.AddScoped<CompanyProvisioningService>();
        services.AddScoped<GoogleAccountService>();
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { TenantContext.StaffRole, TenantContext.AdminRole, TenantContext.SuperAdminRole })
        {
            roles.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
        }
    }

    private static TenantContext Tenant(ClaimsPrincipal user)
    {
        var t = new TenantContext(new Auth(user), new HttpContextAccessor(), new ActingCompanyStore());
        t.EnsureLoadedAsync().GetAwaiter().GetResult();
        return t;
    }

    private static GoogleIdentity G(string email, string sub = "g-1", bool verified = true, string? name = "Pat Lee") => new(sub, email, verified, name);

    private async Task<(IServiceScope Scope, ApplicationUser Admin, int CompanyId)> CompanyWithAdminAsync(string email = "owner@acme.test")
    {
        var scope = _provider.CreateScope();
        var r = await scope.ServiceProvider.GetRequiredService<CompanyProvisioningService>().ProvisionCompanyAsync(
            new CompanyProfile("Acme", email, "9000000001", "1 MG Road"), new AdminAccount("Owner", email, "Str0ng@Pass1"));
        Assert.True(r.Succeeded);
        return (scope, r.User!, r.CompanyId);
    }

    private int AuditCount(string action)
    {
        using var db = new LosDbContext(_options, TenantContext.ForSeeding());
        return db.AuditEntries.Count(a => a.Action == action);
    }

    // ---- temporary password / vendor-created company ----

    [Fact]
    public void TempPassword_AlwaysMeetsThePolicy_AndIsNotRepeated()
    {
        var seen = new HashSet<string>();
        for (var i = 0; i < 300; i++)
        {
            var p = TempPassword.Generate();
            Assert.Matches(new Regex("^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[^A-Za-z0-9]).{16}$"), p);
            Assert.True(seen.Add(p));
        }
    }

    [Fact]
    public async Task AVendorCreatedAdmin_MustChangeTheTemporaryPassword_AndCanSignInWithIt()
    {
        using var scope = _provider.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var password = TempPassword.Generate();

        var r = await scope.ServiceProvider.GetRequiredService<CompanyProvisioningService>().ProvisionCompanyAsync(
            new CompanyProfile("Client Co", "c@client.test", "9000000009", "Addr"), new AdminAccount("Client Admin", "admin@client.test", password),
            requireEmailConfirmation: false, mustChangePassword: true);

        Assert.True(r.Succeeded);
        var admin = await users.FindByEmailAsync("admin@client.test");
        Assert.True(admin!.MustChangePassword);
        Assert.True(admin.EmailConfirmed);
        Assert.True(await users.CheckPasswordAsync(admin, password));
        Assert.True(await users.IsInRoleAsync(admin, TenantContext.AdminRole));
        Assert.False(await users.IsInRoleAsync(admin, TenantContext.SuperAdminRole));
    }

    [Fact]
    public async Task SelfSignUpAdmin_IsNotForcedToChangeTheirPassword()
    {
        var (scope, admin, _) = await CompanyWithAdminAsync();
        using (scope) { Assert.False(admin.MustChangePassword); }
    }

    // ---- sign in ----

    [Fact]
    public async Task Google_SignsInAnExistingUser_WithTheSameVerifiedEmail_ConfirmsItAndLinksTheLogin()
    {
        var (scope, admin, _) = await CompanyWithAdminAsync();
        using (scope)
        {
            var accounts = scope.ServiceProvider.GetRequiredService<GoogleAccountService>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            admin.EmailConfirmed = false; // a password sign-up still waiting for Brevo's link
            await users.UpdateAsync(admin);

            var (user, problem) = await accounts.ResolveForSignInAsync(G("OWNER@acme.test"));

            Assert.Null(problem);
            Assert.Equal(admin.Id, user!.Id);
            Assert.True((await users.FindByIdAsync(admin.Id))!.EmailConfirmed); // Google verified it: no link needed
            Assert.NotNull(await users.FindByLoginAsync("Google", "g-1"));
        }
    }

    [Fact]
    public async Task Google_NeverCreatesAUser_AndRefusesUnknownUnverifiedAndDeactivated_WithTheSameWordsForUnknownAndDeactivated()
    {
        var (scope, admin, _) = await CompanyWithAdminAsync();
        using (scope)
        {
            var accounts = scope.ServiceProvider.GetRequiredService<GoogleAccountService>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var before = users.Users.Count();

            Assert.Equal(GoogleAccountService.NoAccount, (await accounts.ResolveForSignInAsync(G("stranger@else.test", "g-2"))).Problem);
            Assert.Equal(GoogleAccountService.Unverified, (await accounts.ResolveForSignInAsync(G("owner@acme.test", "g-3", verified: false))).Problem);

            admin.IsActive = false;
            await users.UpdateAsync(admin);
            Assert.Equal(GoogleAccountService.NoAccount, (await accounts.ResolveForSignInAsync(G("owner@acme.test", "g-4"))).Problem);

            Assert.Equal(before, users.Users.Count()); // nobody was created inside any company
        }
    }

    [Fact]
    public async Task Google_AKnownGoogleIdWhoseEmailNoLongerMatchesTheUser_IsRefused()
    {
        var (scope, admin, _) = await CompanyWithAdminAsync();
        using (scope)
        {
            var accounts = scope.ServiceProvider.GetRequiredService<GoogleAccountService>();
            Assert.Null(await accounts.LinkAsync(admin, G("owner@acme.test", "g-9")));

            var (user, problem) = await accounts.ResolveForSignInAsync(G("someone.else@gmail.test", "g-9"));

            Assert.Null(user);
            Assert.Equal(GoogleAccountService.NoAccount, problem);
        }
    }

    // ---- link ----

    [Fact]
    public async Task Link_IsForTheUsersOwnVerifiedEmail_OnlyAndOnce_AndIsAudited()
    {
        var (scope, admin, _) = await CompanyWithAdminAsync();
        using (scope)
        {
            var accounts = scope.ServiceProvider.GetRequiredService<GoogleAccountService>();

            Assert.Contains("different email", await accounts.LinkAsync(admin, G("other@gmail.test", "g-5")));
            Assert.Equal(GoogleAccountService.Unverified, await accounts.LinkAsync(admin, G("owner@acme.test", "g-5", verified: false)));
            Assert.Null(await accounts.LinkAsync(admin, G("owner@acme.test", "g-5")));
            Assert.Equal(1, AuditCount("google.linked"));
        }

        var (scope2, _, _) = await CompanyWithAdminAsync("second@beta.test");
        using (scope2)
        {
            var accounts = scope2.ServiceProvider.GetRequiredService<GoogleAccountService>();
            var users = scope2.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.Contains("already linked", await accounts.LinkAsync((await users.FindByEmailAsync("second@beta.test"))!, G("second@beta.test", "g-5"))); // g-5 is the first user's
        }
    }

    // ---- sign up ----

    [Fact]
    public async Task GoogleSignUp_CreatesANewCompanyAndItsAdmin_WithTheEmailConfirmed_AndAuditsIt()
    {
        using var scope = _provider.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<GoogleAccountService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var r = await accounts.SignUpAsync(G("new.owner@fresh.test", "g-7"), new CompanyProfile("Fresh Finance", "new.owner@fresh.test", "9000000007", "Addr"), "New Owner");

        Assert.True(r.Succeeded, string.Join(" ", r.Errors));
        var admin = await users.FindByEmailAsync("new.owner@fresh.test");
        Assert.True(admin!.EmailConfirmed);
        Assert.False(admin.MustChangePassword);
        Assert.Equal(r.CompanyId, admin.CompanyId);
        Assert.True(await users.IsInRoleAsync(admin, TenantContext.AdminRole));
        Assert.NotNull(await users.FindByLoginAsync("Google", "g-7"));
        Assert.Equal(1, AuditCount("company.signup.google"));
    }

    [Fact]
    public async Task GoogleSignUp_RefusesAnEmailThatIsAlreadyRegistered_AndAnUnverifiedOne_WithoutLeavingACompanyBehind()
    {
        var (scope, _, _) = await CompanyWithAdminAsync();
        using (scope)
        {
            var accounts = scope.ServiceProvider.GetRequiredService<GoogleAccountService>();
            int Companies() { using var db = new LosDbContext(_options, TenantContext.ForSeeding()); return db.Companies.Count(); }
            var before = Companies();

            var dup = await accounts.SignUpAsync(G("owner@acme.test", "g-8"), new CompanyProfile("Dup", "d@d.test", "9000000008", "A"), "Dup");
            var unverified = await accounts.SignUpAsync(G("fresh@x.test", "g-10", verified: false), new CompanyProfile("Unv", "u@u.test", "9000000010", "A"), "Unv");

            Assert.Equal(GoogleAccountService.AlreadyRegistered, Assert.Single(dup.Errors));
            Assert.Equal(GoogleAccountService.Unverified, Assert.Single(unverified.Errors));
            Assert.Equal(before, Companies());
        }
    }

    // ---- reading Google's answer ----

    [Fact]
    public void TheGoogleIdentity_ReadsGoogleOwnEmailVerifiedClaim_AndNothingElseCountsAsVerified()
    {
        ClaimsPrincipal P(string? verified) => new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "sub-1"), new Claim(ClaimTypes.Email, "a@b.test"), new Claim(ClaimTypes.Name, "A B"), .. verified is null ? [] : new[] { new Claim("email_verified", verified) }], "Google"));

        Assert.True(GoogleLoginEndpoints.Identity(P("true"))!.EmailVerified);
        Assert.True(GoogleLoginEndpoints.Identity(P("True"))!.EmailVerified);
        Assert.False(GoogleLoginEndpoints.Identity(P("false"))!.EmailVerified);
        Assert.False(GoogleLoginEndpoints.Identity(P(null))!.EmailVerified);
        Assert.Null(GoogleLoginEndpoints.Identity(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "x")], "Google")))); // no email
    }

    [Fact]
    public void GoogleIsHidden_UntilBothTheClientIdAndSecretAreSet()
    {
        Assert.False(new GoogleLoginOptions().Enabled);
        Assert.False(new GoogleLoginOptions { ClientId = "id" }.Enabled);
        Assert.False(new GoogleLoginOptions { ClientId = "id", ClientSecret = " " }.Enabled);
        Assert.True(new GoogleLoginOptions { ClientId = "id", ClientSecret = "secret" }.Enabled);
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
        try { Directory.Delete(_vaultDir, recursive: true); } catch { /* best effort */ }
    }

    private sealed class Auth(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
    }

    private sealed class Env : IWebHostEnvironment
    {
        public Env(string root)
        {
            ContentRootPath = WebRootPath = root;
            Directory.CreateDirectory(root);
            ContentRootFileProvider = WebRootFileProvider = new NullFileProvider();
        }

        public string ApplicationName { get; set; } = "LosLms.Tests";
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; }
    }
}
