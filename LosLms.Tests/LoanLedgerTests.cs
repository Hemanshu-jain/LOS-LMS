using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LosLms.Tests;

/// <summary>Slice 1 of the servicing ledger: the instalment snapshot equals the schedule, the account opens once, and DPD is computed.</summary>
public sealed class LoanLedgerTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"ledger-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<LosDbContext> _options;
    private static readonly DateOnly First = new(2026, 11, 5);

    public LoanLedgerTests()
    {
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using var db = Ctx();
        db.Database.EnsureCreated();
        db.Applications.Add(new Application { Id = "A1", CompanyId = 1 });
        db.SaveChanges();
    }

    private LosDbContext Ctx() => new(_options, TenantContext.ForSeeding());

    [Fact]
    public async Task Open_SnapshotsExactlyTheSchedule_IncludingOverrideAndRounding()
    {
        using var db = Ctx();
        var app = await db.Applications.SingleAsync();

        var account = await LoanAccountService.OpenAsync(db, app, 1_000_000m, 12m, 24, First, 5, firstEmiOverride: 50_000m, roundTo: 100m);
        await db.SaveChangesAsync();

        var expected = LoanMath.Schedule(1_000_000m, 12m, 24, First, 50_000m, 100m);
        using var read = Ctx();
        var rows = read.Instalments.OrderBy(i => i.No).ToList();
        Assert.Equal(expected.Count, rows.Count);
        Assert.Equal(expected.Select(r => (r.Month, r.Emi, r.Principal, r.Interest, r.DueDate)),
            rows.Select(r => (r.No, r.Emi, r.Principal, r.Interest, (DateOnly?)r.DueDate)));
        Assert.Equal(50_000m, rows[0].Emi); // the first-EMI override survives
        Assert.All(rows, r => Assert.Equal("Due", r.Status));
        Assert.Equal(("A1", "A1", 5), (account!.ApplicationId, read.LoanAccounts.Single().LoanAccountNo, read.LoanAccounts.Single().EmiDay));
    }

    [Fact]
    public async Task Open_HappensOnce_AReleaseTwiceAddsNothing()
    {
        using var db = Ctx();
        var app = await db.Applications.SingleAsync();
        await LoanAccountService.OpenAsync(db, app, 500_000m, 14m, 12, First, null, null, null);
        await db.SaveChangesAsync();

        using var again = Ctx();
        var second = await LoanAccountService.OpenAsync(again, await again.Applications.SingleAsync(), 500_000m, 14m, 12, First, null, null, null);
        await again.SaveChangesAsync();

        Assert.Null(second);
        using var read = Ctx();
        Assert.Equal(1, read.LoanAccounts.Count());
        Assert.Equal(12, read.Instalments.Count());
        Assert.Equal(First.Day, read.LoanAccounts.Single().EmiDay); // no EMI day given: the first EMI's day
    }

    [Fact]
    public async Task Open_WithoutTermsOrFirstDate_OpensNothing()
    {
        using var db = Ctx();
        var app = await db.Applications.SingleAsync();

        Assert.Null(await LoanAccountService.OpenAsync(db, app, 500_000m, 14m, 12, null, null, null, null));
        Assert.Null(await LoanAccountService.OpenAsync(db, app, 0m, 14m, 12, First, null, null, null));
        Assert.Null(await LoanAccountService.OpenAsync(db, app, 500_000m, 14m, 0, First, null, null, null));
    }

    [Fact]
    public void Standing_DpdIsTheOldestUnpaidPastDue_PaidAndWaivedNeverCount()
    {
        var today = new DateOnly(2026, 12, 20);
        var rows = new List<Instalment>
        {
            new() { No = 1, DueDate = new(2026, 11, 5), Emi = 100m, Principal = 70m, Status = "Paid", PaidAmount = 100m },
            new() { No = 2, DueDate = new(2026, 12, 5), Emi = 100m, Principal = 72m, Status = "Bounced" },          // 15 days late
            new() { No = 3, DueDate = new(2027, 1, 5), Emi = 100m, Principal = 74m, Status = "Due" },
            new() { No = 4, DueDate = new(2026, 10, 5), Emi = 100m, Principal = 68m, Status = "Waived" },
        };

        var s = LoanStanding.Compute(rows, today);

        Assert.Equal(15, s.DaysPastDue);
        Assert.Equal(100m, s.OverdueAmount);
        Assert.Equal(72m + 74m, s.OutstandingPrincipal);
        Assert.Equal(100m, s.TotalPaid);
        Assert.Equal(2, s.NextDue!.No);
    }

    [Fact]
    public void Standing_PartPaid_OwesTheRest_AndNothingLateMeansZeroDpd()
    {
        var today = new DateOnly(2026, 12, 10);
        var late = new List<Instalment> { new() { No = 1, DueDate = new(2026, 12, 5), Emi = 100m, Principal = 70m, Status = "PartPaid", PaidAmount = 40m } };
        var fine = new List<Instalment> { new() { No = 1, DueDate = new(2026, 12, 25), Emi = 100m, Principal = 70m, Status = "Due" } };

        Assert.Equal(60m, LoanStanding.Compute(late, today).OverdueAmount);
        Assert.Equal(5, LoanStanding.Compute(late, today).DaysPastDue);
        Assert.Equal(0, LoanStanding.Compute(fine, today).DaysPastDue);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_file);
    }
}
