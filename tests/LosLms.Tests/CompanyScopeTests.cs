using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LosLms.Tests;

/// <summary>
/// A SuperAdmin who is not acting as a company has an OPEN query filter, so every lookup that relied on the filter alone saw all
/// companies at once. These run as that SuperAdmin (<see cref="TenantContext.ForSeeding"/>) with two companies holding the same
/// scheme / account number / vehicle, and prove each company only ever gets its own.
/// </summary>
public sealed class CompanyScopeTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"scope-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<LosDbContext> _options;

    public CompanyScopeTests()
    {
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using var db = Open();
        db.Database.EnsureCreated();
        db.Companies.Add(new Company { Id = 2, Name = "Company B" });
        db.SaveChanges();
    }

    private LosDbContext Open() => new(_options, TenantContext.ForSeeding());

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { File.Delete(_file); } catch (IOException) { }
    }

    [Fact]
    public async Task SchemeTemplates_ReturnOnlyThatCompanysCharges()
    {
        using var db = Open();
        db.SchemeChargeTemplates.AddRange(
            new SchemeChargeTemplate { CompanyId = 1, Scheme = "GOLD", Head = "Processing fee A", Value = 1m },
            new SchemeChargeTemplate { CompanyId = 2, Scheme = "GOLD", Head = "Processing fee B", Value = 2m });
        db.SaveChanges();

        var one = await SchemeTemplates.ForAsync(db, 1, "GOLD");
        var two = await SchemeTemplates.ForAsync(db, 2, "GOLD");

        Assert.Equal(["Processing fee A"], one.Select(t => t.Head));
        Assert.Equal(["Processing fee B"], two.Select(t => t.Head));
        Assert.Empty(await SchemeTemplates.ForAsync(db, 1, null));
    }

    [Fact]
    public async Task MigratedLoanImport_NeverTouchesAnotherCompanysLoan()
    {
        using var db = Open();
        db.MigratedLoans.Add(new MigratedLoan { CompanyId = 2, LoanAccountNo = "L-1", CustomerName = "B's customer", OutstandingAmount = 500m });
        db.SaveChanges();

        var n = await MigratedLoanImport.UpsertAsync(db, 1, [new MigratedLoanRow("l-1", "A's customer", null, null, null, null, 900m, null, null, null)]);

        Assert.Equal(1, n);
        var b = db.MigratedLoans.Single(m => m.CompanyId == 2);
        Assert.Equal("B's customer", b.CustomerName);
        Assert.Equal(500m, b.OutstandingAmount);
        var a = db.MigratedLoans.Single(m => m.CompanyId == 1);
        Assert.Equal("A's customer", a.CustomerName);

        // Re-importing for the same company updates its own row instead of adding another.
        await MigratedLoanImport.UpsertAsync(db, 1, [new MigratedLoanRow("L-1", "A renamed", null, null, null, null, 800m, null, null, null)]);
        Assert.Equal(2, db.MigratedLoans.Count());
        Assert.Equal("A renamed", db.MigratedLoans.Single(m => m.CompanyId == 1).CustomerName);
    }

    [Fact]
    public async Task VehicleCap_UsesTheApplicationsCompanyCatalog()
    {
        using var db = Open();
        db.VehicleLoanCaps.AddRange(
            new VehicleLoanCap { CompanyId = 1, Make = "Tata", Model = "Ace", Year = 2022, MaxLoanAmount = 100000m },
            new VehicleLoanCap { CompanyId = 2, Make = "Tata", Model = "Ace", Year = 2022, MaxLoanAmount = 500000m });
        db.SaveChanges();

        var a = await VehicleCapCheck.EvaluateAsync(db, "Vehicle", "Tata Ace", "2022", 300000m, 1);
        var b = await VehicleCapCheck.EvaluateAsync(db, "Vehicle", "Tata Ace", "2022", 300000m, 2);

        Assert.True(a.Blocked);
        Assert.Equal(100000m, a.Cap);
        Assert.False(b.Blocked);
    }
}
