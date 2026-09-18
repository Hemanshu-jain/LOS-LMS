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
/// Proves company isolation is enforced by the DbContext's global query filters, not merely by the
/// convention that screens resolve their Application first. Two companies are seeded with overlapping
/// data; a company-1 user must never see company-2 rows through ANY DbSet — including child and
/// grandchild tables queried directly.
/// </summary>
public sealed class TenantIsolationTests : IDisposable
{
    // Company 1 is the row the model seeds via HasData (LosDbContext.SeedCompanyId); company 2 is added
    // here. A1/A2 are one application per company, each with its own children.
    private const int CompanyA = LosDbContext.SeedCompanyId; // 1
    private const int CompanyB = 2;

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<LosDbContext> _options;

    public TenantIsolationTests()
    {
        // A single kept-open in-memory connection: the schema and data live only as long as it is open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<LosDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = NewContext(SuperAdmin());
        db.Database.EnsureCreated(); // builds the schema and applies HasData (company 1, lookups)
        Seed(db);
    }

    [Fact]
    public void CompanyUser_SeesOnlyOwnRows_AcrossParentChildAndGrandchild()
    {
        using var db = NewContext(CompanyUser(CompanyA));

        Assert.Equal(new[] { "A1" }, db.Applications.Select(a => a.Id).ToArray());
        Assert.All(db.Applications.ToList(), a => Assert.Equal(CompanyA, a.CompanyId));

        // Children queried DIRECTLY, never through Applications — the whole point of the filters.
        Assert.Equal(new[] { "A1" }, db.Parties.Select(p => p.ApplicationId).ToArray());
        Assert.Equal(new[] { "A1" }, db.BankDetails.Select(b => b.ApplicationId).ToArray());
        Assert.Equal(new[] { "A1" }, db.ChecklistDocuments.Select(c => c.ApplicationId).ToArray());

        // Grandchild (DocumentRemark -> ChecklistDocument -> Application): exactly company A's one remark.
        Assert.Single(db.DocumentRemarks.ToList());
        Assert.Equal("A1-remark", db.DocumentRemarks.Single().Text);
    }

    [Fact]
    public void OtherCompanyUser_SeesTheOtherSide()
    {
        using var db = NewContext(CompanyUser(CompanyB));

        Assert.Equal(new[] { "A2" }, db.Applications.Select(a => a.Id).ToArray());
        Assert.Equal(new[] { "A2" }, db.Parties.Select(p => p.ApplicationId).ToArray());
        Assert.Equal("A2-remark", db.DocumentRemarks.Single().Text);
    }

    [Fact]
    public void SuperAdmin_SeesEveryCompany()
    {
        using var db = NewContext(SuperAdmin());

        Assert.Equal(2, db.Applications.Count());
        Assert.Equal(2, db.Parties.Count());
        Assert.Equal(2, db.DocumentRemarks.Count());
    }

    [Fact]
    public void SuperAdmin_ActingAsCompany_IsScopedToThatCompanyOnly()
    {
        var store = new ActingCompanyStore();
        store.Set("super", CompanyB); // the vendor picked company B in the company picker

        var tenant = new TenantContext(new FakeAuthStateProvider(SuperAdmin()), new HttpContextAccessor(), store);
        tenant.EnsureLoadedAsync().GetAwaiter().GetResult();
        using var db = new LosDbContext(_options, tenant);

        // Now scoped exactly like a company-B user — company A's rows are invisible, not merged in.
        Assert.Equal(new[] { "A2" }, db.Applications.Select(a => a.Id).ToArray());
        Assert.Equal("A2-remark", db.DocumentRemarks.Single().Text);
    }

    /// <summary>
    /// The durable backstop: every tenant-owned table (anything with an ApplicationId, plus the
    /// DocumentRemark grandchild) must carry a query filter. A newly added child table that forgets one
    /// fails here rather than silently leaking cross-tenant data in production.
    /// </summary>
    [Fact]
    public void EveryTenantScopedEntity_HasAQueryFilter()
    {
        using var db = NewContext(SuperAdmin());

        var missing = db.Model.GetEntityTypes()
            .Where(e => e.FindProperty("ApplicationId") is not null || e.ClrType == typeof(DocumentRemark))
            .Where(e => e.GetQueryFilter() is null)
            .Select(e => e.ClrType.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(missing.Count == 0,
            "Tenant-scoped entities missing a company query filter (add one in LosDbContext.ConfigureTenancy): "
            + string.Join(", ", missing));
    }

    private void Seed(LosDbContext db)
    {
        db.Companies.Add(new Company { Id = CompanyB, Name = "Company B" });
        db.Applications.Add(new Application { Id = "A1", CompanyId = CompanyA });
        db.Applications.Add(new Application { Id = "A2", CompanyId = CompanyB });
        db.SaveChanges();

        db.Parties.Add(new Party { ApplicationId = "A1", PartyType = "Applicant" });
        db.Parties.Add(new Party { ApplicationId = "A2", PartyType = "Applicant" });
        db.BankDetails.Add(new BankDetail { ApplicationId = "A1" });
        db.BankDetails.Add(new BankDetail { ApplicationId = "A2" });

        var docA = new ChecklistDocument { ApplicationId = "A1" };
        var docB = new ChecklistDocument { ApplicationId = "A2" };
        db.ChecklistDocuments.AddRange(docA, docB);
        db.SaveChanges();

        db.DocumentRemarks.Add(new DocumentRemark { DocumentId = docA.Id, Text = "A1-remark" });
        db.DocumentRemarks.Add(new DocumentRemark { DocumentId = docB.Id, Text = "A2-remark" });
        db.SaveChanges();
    }

    private LosDbContext NewContext(ClaimsPrincipal user)
    {
        var tenant = new TenantContext(new FakeAuthStateProvider(user), new HttpContextAccessor(), new ActingCompanyStore());
        // The real factory calls this before handing out a context; the ctor snapshots the result.
        tenant.EnsureLoadedAsync().GetAwaiter().GetResult();
        return new LosDbContext(_options, tenant);
    }

    private static ClaimsPrincipal CompanyUser(int companyId) => Principal(
        new Claim(ClaimTypes.NameIdentifier, $"user-{companyId}"),
        new Claim(ClaimTypes.Role, TenantContext.AdminRole),
        new Claim(TenantContext.CompanyIdClaim, companyId.ToString()));

    private static ClaimsPrincipal SuperAdmin() => Principal(
        new Claim(ClaimTypes.NameIdentifier, "super"),
        new Claim(ClaimTypes.Role, TenantContext.SuperAdminRole));

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "TestAuth")); // non-null type => IsAuthenticated

    public void Dispose() => _connection.Dispose();

    private sealed class FakeAuthStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(user));
    }
}
