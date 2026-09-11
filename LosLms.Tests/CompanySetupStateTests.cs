using System.Security.Claims;
using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LosLms.Tests;

/// <summary>
/// The first-run gate now demands a full company profile — name, contact email, phone and registered
/// address — plus a branch, so each company is identifiable at the vendor's back end before the app
/// unlocks. These prove a half-filled profile stays locked and a complete one unlocks (and stamps).
/// </summary>
public sealed class CompanySetupStateTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<LosDbContext> _options;

    public CompanySetupStateTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite(_connection).Options;

        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    [Fact]
    public async Task NameAndBranchOnly_IsNotComplete_WithoutContactDetails()
    {
        var id = Seed(name: "Acme", email: null, phone: null, address: null, withBranch: true);

        using var db = NewContext();
        Assert.False(await CompanySetupState.IsCompleteAsync(db, id));
    }

    [Fact]
    public async Task FullProfileButNoBranch_IsNotComplete()
    {
        var id = Seed(name: "Acme", email: "a@acme.test", phone: "999", address: "1 St", withBranch: false);

        using var db = NewContext();
        Assert.False(await CompanySetupState.IsCompleteAsync(db, id));
    }

    [Fact]
    public async Task FullProfileAndBranch_IsComplete_AndStamps()
    {
        var id = Seed(name: "Acme", email: "a@acme.test", phone: "999", address: "1 St", withBranch: true);

        using var db = NewContext();
        Assert.True(await CompanySetupState.IsCompleteAsync(db, id));

        // Completion is stamped so it persists and is never re-evaluated.
        var stamped = await db.Companies.IgnoreQueryFilters().FirstAsync(c => c.Id == id);
        Assert.NotNull(stamped.SetupCompletedAt);
    }

    private int Seed(string name, string? email, string? phone, string? address, bool withBranch)
    {
        using var db = NewContext();
        var company = new Company
        {
            Name = name,
            ContactEmail = email,
            ContactPhone = phone,
            Address = address,
        };
        db.Companies.Add(company);
        db.SaveChanges();

        if (withBranch)
        {
            db.Branches.Add(new Branch { CompanyId = company.Id, Name = "Head Office" });
            db.SaveChanges();
        }

        return company.Id;
    }

    // SuperAdmin (unscoped) context: sees and writes every company, exactly as the middleware's call does.
    private LosDbContext NewContext()
    {
        var superAdmin = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "super"),
                new Claim(ClaimTypes.Role, TenantContext.SuperAdminRole),
            },
            authenticationType: "TestAuth"));

        var tenant = new TenantContext(new FakeAuthStateProvider(superAdmin), new HttpContextAccessor());
        tenant.EnsureLoadedAsync().GetAwaiter().GetResult();
        return new LosDbContext(_options, tenant);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class FakeAuthStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(user));
    }
}
