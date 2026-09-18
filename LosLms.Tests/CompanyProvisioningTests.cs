using System.Security.Claims;
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
/// Proves the self-service provisioning path: creating a company gives its owning admin the Admin role
/// only (never SuperAdmin), scoped to a fresh company id, and two provisioned companies are isolated —
/// the "sign in and see only your own data" guarantee — while a duplicate email or a weak password is
/// rejected without leaving an orphan tenant behind.
/// </summary>
public sealed class CompanyProvisioningTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<LosDbContext> _options;
    private readonly ServiceProvider _provider;
    private readonly string _vaultDir;

    public CompanyProvisioningTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite(_connection).Options;

        using (var db = NewContext(SuperAdmin()))
        {
            db.Database.EnsureCreated(); // schema + HasData (seed company 1)
        }

        _vaultDir = Path.Combine(Path.GetTempPath(), "los-vault-" + Guid.NewGuid().ToString("N"));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_options);

        // Identity's stores resolve LosDbContext from DI; an anonymous tenant lets the user store see and
        // create across companies (HasUser=false), and StampTenant leaves the explicit CompanyId intact.
        services.AddScoped(_ => new LosDbContext(_options, AnonymousTenant()));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<LosDbContext>();

        services.AddSingleton<IWebHostEnvironment>(new FakeEnvironment(_vaultDir));
        services.AddSingleton<PasswordVault>();
        services.AddScoped<CompanyProvisioningService>();

        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { TenantContext.StaffRole, TenantContext.AdminRole, TenantContext.SuperAdminRole })
        {
            roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
        }
    }

    [Fact]
    public async Task Provision_CreatesAdminScopedToNewCompany_NotSuperAdmin()
    {
        using var scope = _provider.CreateScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<CompanyProvisioningService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var result = await provisioning.ProvisionCompanyAsync(
            new CompanyProfile("Acme Finance", "owner@acme.test", "9000000001", "1 MG Road"),
            new AdminAccount("Acme Owner", "owner@acme.test", "Str0ng@Pass1"));

        Assert.True(result.Succeeded);
        Assert.NotEqual(LosDbContext.SeedCompanyId, result.CompanyId); // a brand-new company, not seed id 1

        var user = await userManager.FindByEmailAsync("owner@acme.test");
        Assert.NotNull(user);
        Assert.Equal(result.CompanyId, user!.CompanyId);
        Assert.True(await userManager.IsInRoleAsync(user, TenantContext.AdminRole));
        Assert.False(await userManager.IsInRoleAsync(user, TenantContext.SuperAdminRole));
    }

    [Fact]
    public async Task Provision_SeedsDropdownDefaults_ForTheNewCompanyOnly()
    {
        using var scope = _provider.CreateScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<CompanyProvisioningService>();

        var a = await provisioning.ProvisionCompanyAsync(
            new CompanyProfile("Alpha", "a@alpha.test", "9000000002", "Addr A"),
            new AdminAccount("Alpha Admin", "a@alpha.test", "Str0ng@Pass1"));
        var b = await provisioning.ProvisionCompanyAsync(
            new CompanyProfile("Beta", "b@beta.test", "9000000003", "Addr B"),
            new AdminAccount("Beta Admin", "b@beta.test", "Str0ng@Pass2"));

        Assert.True(a.Succeeded);
        Assert.True(b.Succeeded);

        // Company A, seen through its own scoped context, sees only its own lookup rows — never Beta's.
        using var db = NewContext(CompanyUser(a.CompanyId));
        var lookups = db.LookupValues.ToList();
        Assert.NotEmpty(lookups);
        Assert.All(lookups, l => Assert.Equal(a.CompanyId, l.CompanyId));
    }

    [Fact]
    public async Task Provision_AssignsADistinctCodeToEachCompany()
    {
        using var scope = _provider.CreateScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<CompanyProvisioningService>();

        // Same name on purpose — the codes must still differ (the second can't reuse the first's).
        var a = await provisioning.ProvisionCompanyAsync(
            new CompanyProfile("Kishore Finance", "k1@kis.test", "9000000010", "Addr"),
            new AdminAccount("K1", "k1@kis.test", "Str0ng@Pass1"));
        var b = await provisioning.ProvisionCompanyAsync(
            new CompanyProfile("Kishore Finance", "k2@kis.test", "9000000011", "Addr"),
            new AdminAccount("K2", "k2@kis.test", "Str0ng@Pass2"));

        Assert.True(a.Succeeded);
        Assert.True(b.Succeeded);

        using var db = NewContext(SuperAdmin());
        var codeA = db.Companies.Where(c => c.Id == a.CompanyId).Select(c => c.Code).Single();
        var codeB = db.Companies.Where(c => c.Id == b.CompanyId).Select(c => c.Code).Single();

        Assert.Equal("KIS", codeA);       // name-derived
        Assert.Equal(3, codeB!.Length);
        Assert.NotEqual(codeA, codeB);    // unique despite the identical name
    }

    [Fact]
    public async Task Provision_RejectsDuplicateEmail_WithoutCreatingCompany()
    {
        using var scope = _provider.CreateScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<CompanyProvisioningService>();

        var first = await provisioning.ProvisionCompanyAsync(
            new CompanyProfile("First", "dup@x.test", "9000000004", "Addr"),
            new AdminAccount("First Admin", "dup@x.test", "Str0ng@Pass1"));
        Assert.True(first.Succeeded);

        var companiesBefore = CompanyCount();

        var second = await provisioning.ProvisionCompanyAsync(
            new CompanyProfile("Second", "dup@x.test", "9000000005", "Addr"),
            new AdminAccount("Second Admin", "dup@x.test", "Str0ng@Pass2"));

        Assert.False(second.Succeeded);
        Assert.NotEmpty(second.Errors);
        Assert.Equal(companiesBefore, CompanyCount()); // no orphan company was created
    }

    [Fact]
    public async Task Provision_WeakPassword_RollsBackTheCompany()
    {
        using var scope = _provider.CreateScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<CompanyProvisioningService>();

        var before = CompanyCount();

        var result = await provisioning.ProvisionCompanyAsync(
            new CompanyProfile("Weak", "weak@x.test", "9000000006", "Addr"),
            new AdminAccount("Weak Admin", "weak@x.test", "short")); // fails the password policy

        Assert.False(result.Succeeded);
        Assert.Equal(before, CompanyCount()); // the company created before the admin failed was rolled back
    }

    private int CompanyCount()
    {
        using var db = NewContext(SuperAdmin());
        return db.Companies.IgnoreQueryFilters().Count();
    }

    private LosDbContext NewContext(ClaimsPrincipal user)
    {
        var tenant = new TenantContext(new FakeAuthStateProvider(user), new HttpContextAccessor(), new ActingCompanyStore());
        tenant.EnsureLoadedAsync().GetAwaiter().GetResult();
        return new LosDbContext(_options, tenant);
    }

    private static TenantContext AnonymousTenant()
    {
        var tenant = new TenantContext(
            new FakeAuthStateProvider(new ClaimsPrincipal(new ClaimsIdentity())), new HttpContextAccessor(), new ActingCompanyStore());
        tenant.EnsureLoadedAsync().GetAwaiter().GetResult();
        return tenant;
    }

    private static ClaimsPrincipal SuperAdmin() => new(new ClaimsIdentity(
        new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "super"),
            new Claim(ClaimTypes.Role, TenantContext.SuperAdminRole),
        },
        authenticationType: "TestAuth"));

    private static ClaimsPrincipal CompanyUser(int companyId) => new(new ClaimsIdentity(
        new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-" + companyId),
            new Claim(ClaimTypes.Role, TenantContext.AdminRole),
            new Claim(TenantContext.CompanyIdClaim, companyId.ToString()),
        },
        authenticationType: "TestAuth"));

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
        try { Directory.Delete(_vaultDir, recursive: true); } catch { /* best effort */ }
    }

    private sealed class FakeAuthStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(user));
    }

    private sealed class FakeEnvironment : IWebHostEnvironment
    {
        public FakeEnvironment(string contentRoot)
        {
            ContentRootPath = contentRoot;
            Directory.CreateDirectory(contentRoot);
            ContentRootFileProvider = new NullFileProvider();
            WebRootPath = contentRoot;
            WebRootFileProvider = new NullFileProvider();
        }

        public string ApplicationName { get; set; } = "LosLms.Tests";
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; }
    }
}
