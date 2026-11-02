using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LosLms.Tests;

public sealed class ClassificationListsTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"cls-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<LosDbContext> _options;

    public ClassificationListsTests()
    {
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using var db = new LosDbContext(_options, TenantContext.ForSeeding());
        db.Database.EnsureCreated();
    }

    [Fact]
    public async Task EveryCompany_IsSeededWithTheListsThatUsedToBeHardcoded()
    {
        using var db = new LosDbContext(_options, TenantContext.ForSeeding());
        await LookupSeeder.EnsureDefaultsAsync(db, 1);

        var sub = await db.LookupValues.Where(l => l.CompanyId == 1 && l.Kind == LookupKind.PslSub).Select(l => l.Name).ToListAsync();
        var partners = await db.LookupValues.Where(l => l.CompanyId == 1 && l.Kind == LookupKind.CoLendingPartner).Select(l => l.Name).ToListAsync();
        var uses = await db.LookupValues.Where(l => l.CompanyId == 1 && l.Kind == LookupKind.EndUse).Select(l => l.Name).ToListAsync();

        Assert.Equal(new[] { "Agriculture", "MSME", "Other", "Transport Sector - CV" }, sub.Order().ToArray());
        Assert.Contains("None", partners);
        Assert.Contains("HDFC Bank", partners);
        Assert.Contains("Working capital", uses);
    }

    [Fact]
    public async Task Seeding_DoesNotOverwriteAListACompanyHasAlreadyEdited()
    {
        using var db = new LosDbContext(_options, TenantContext.ForSeeding());
        db.LookupValues.Add(new LookupValue { CompanyId = 1, Kind = LookupKind.EndUse, Name = "Tipper purchase" });
        await db.SaveChangesAsync();

        await LookupSeeder.EnsureDefaultsAsync(db, 1);

        Assert.Equal(new[] { "Tipper purchase" }, await db.LookupValues.Where(l => l.Kind == LookupKind.EndUse).Select(l => l.Name).ToListAsync());
    }

    [Fact]
    public void TheCam_PrintsTheBorrowerClassification()
    {
        var app = new Application { Id = "A1", LoanAmount = 500_000m, Roi = 14m, Tenure = 24 };
        var cam = new CamCostBreakdown { LastRecalculatedAt = DateTime.UtcNow, AppliedExShowroomCost = 600_000m };
        var without = CamPdf.Build(app, cam);
        var with = CamPdf.Build(app, cam, new Classification
        {
            Psl = Classification.PslYes, PslSub = "Transport Sector - CV", CoLendingPartner = "HDFC Bank", EndUse = "Fleet expansion",
            RiskSharing = 20m, PrioritySectorAmount = 500_000m,
        });

        Assert.NotEmpty(without);
        Assert.NotEmpty(with);
        Assert.NotEqual(without.Length, with.Length); // the classification rows add content to the memo
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_file);
    }
}
