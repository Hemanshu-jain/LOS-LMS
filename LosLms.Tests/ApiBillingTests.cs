using System.Security.Claims;
using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace LosLms.Tests;

/// <summary>
/// Provider-API metering and postpaid billing: only successful calls bill, months close into one
/// invoice each, and API access pauses only once a bill is unpaid past its grace date.
/// </summary>
public sealed class ApiBillingTests : IDisposable
{
    private const int Company = LosDbContext.SeedCompanyId;
    private const int OtherCompany = 2;
    private const int Grace = 7;

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<LosDbContext> _options;
    private readonly ApiBillingService _svc;

    public ApiBillingTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite(_connection).Options;

        using var db = NewContext(SuperAdmin());
        db.Database.EnsureCreated();
        db.Companies.Add(new Company { Id = OtherCompany, Name = "Company B" });
        db.SaveChanges();

        _svc = new ApiBillingService(new TestFactory(this), Options.Create(new ApiBillingOptions { GraceDays = Grace, GstPct = 18m }));
    }

    [Fact]
    public async Task Usage_BillsOnlySuccessfulCalls_AtTheSnapshotRate()
    {
        var t = Utc(2026, 9, 10);
        await _svc.RecordAsync(Company, DigioRates.DigiLockerFetch, true, nowUtc: t);
        await _svc.RecordAsync(Company, DigioRates.DigiLockerFetch, true, nowUtc: t);
        await _svc.RecordAsync(Company, DigioRates.DigiLockerFetch, false, nowUtc: t);
        await _svc.RecordAsync(Company, DigioRates.PennyDrop, true, nowUtc: t);

        var lines = await _svc.UsageForMonthAsync(Company, new DateOnly(2026, 9, 1));

        var locker = Assert.Single(lines, l => l.ApiCode == DigioRates.DigiLockerFetch);
        Assert.Equal((2, 1, 4.00m), (locker.Calls, locker.Failed, locker.Amount));
        Assert.Equal(6.50m, lines.Sum(l => l.Amount)); // 2×2.00 + 1×2.50, the failed call is free
    }

    [Fact]
    public async Task Usage_MonthBoundaryIsIndiaTime()
    {
        // 19:00 UTC on 31 Aug is 00:30 IST on 1 Sep — it belongs to September's bill.
        await _svc.RecordAsync(Company, DigioRates.IdOcr, true, nowUtc: Utc(2026, 8, 31, 19));

        Assert.Empty(await _svc.UsageForMonthAsync(Company, new DateOnly(2026, 8, 1)));
        Assert.Single(await _svc.UsageForMonthAsync(Company, new DateOnly(2026, 9, 1)));
    }

    [Fact]
    public async Task Record_UnknownApiCode_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _svc.RecordAsync(Company, "NOPE", true));
    }

    [Fact]
    public async Task Invoice_RaisedOnlyForClosedMonths_AndIsIdempotent()
    {
        await _svc.RecordAsync(Company, DigioRates.ESignAadhaar, true, nowUtc: Utc(2026, 8, 15));
        await _svc.RecordAsync(Company, DigioRates.ESignAadhaar, true, nowUtc: Utc(2026, 9, 5));

        var now = Utc(2026, 9, 20);
        await _svc.EnsureInvoicesAsync(null, now);
        await _svc.EnsureInvoicesAsync(null, now);

        var inv = Assert.Single(await _svc.InvoicesAsync(Company)); // September is still running
        Assert.Equal(new DateOnly(2026, 8, 1), inv.PeriodStart);
        Assert.Equal(10.60m, inv.Subtotal);
        Assert.Equal(1.91m, inv.Gst); // 18% of 10.60, rounded
        Assert.Equal(new DateOnly(2026, 9, 1).AddDays(Grace), inv.DueDate);
    }

    [Fact]
    public async Task Gate_AllowsInGrace_PausesAfterDue_ResumesWhenPaid()
    {
        await _svc.RecordAsync(Company, DigioRates.BankStatementAnalyzer, true, nowUtc: Utc(2026, 8, 15));

        Assert.True((await _svc.CheckAccessAsync(Company, Utc(2026, 8, 20))).Allowed); // month not closed

        var inGrace = await _svc.CheckAccessAsync(Company, Utc(2026, 9, 5)); // bill raised, within grace
        Assert.True(inGrace.Allowed);
        Assert.NotNull(inGrace.DueDate);
        Assert.True(inGrace.AmountDue > 0);

        var paused = await _svc.CheckAccessAsync(Company, Utc(2026, 9, 9)); // past 1 Sep + 7 days
        Assert.False(paused.Allowed);
        Assert.Contains("paused", paused.Reason);

        var invoice = Assert.Single(await _svc.InvoicesAsync(Company));
        Assert.True(await _svc.MarkPaidAsync(invoice.Id, "UPI ref 123", Utc(2026, 9, 9)));
        Assert.True((await _svc.CheckAccessAsync(Company, Utc(2026, 9, 10))).Allowed);
        Assert.False(await _svc.MarkPaidAsync(invoice.Id, null)); // can't pay twice
    }

    [Fact]
    public async Task Gate_OneCompanyBeingOverdue_DoesNotPauseAnother()
    {
        await _svc.RecordAsync(Company, DigioRates.IdOcr, true, nowUtc: Utc(2026, 8, 15));

        Assert.False((await _svc.CheckAccessAsync(Company, Utc(2026, 9, 20))).Allowed);
        Assert.True((await _svc.CheckAccessAsync(OtherCompany, Utc(2026, 9, 20))).Allowed);
    }

    [Fact]
    public async Task CompanyUser_SeesOnlyOwnUsageAndInvoices()
    {
        await _svc.RecordAsync(Company, DigioRates.IdOcr, true, nowUtc: Utc(2026, 8, 15));
        await _svc.RecordAsync(OtherCompany, DigioRates.IdOcr, true, nowUtc: Utc(2026, 8, 15));
        await _svc.EnsureInvoicesAsync(null, Utc(2026, 9, 20));

        using var db = NewContext(CompanyUser(OtherCompany));
        Assert.All(db.ApiUsageLogs.ToList(), l => Assert.Equal(OtherCompany, l.CompanyId));
        Assert.Single(db.ApiUsageLogs.ToList());
        Assert.Single(db.ApiInvoices.ToList());
    }

    private static DateTime Utc(int y, int m, int d, int h = 12) => new(y, m, d, h, 0, 0, DateTimeKind.Utc);

    private LosDbContext NewContext(ClaimsPrincipal user)
    {
        var tenant = new TenantContext(new FakeAuthStateProvider(user), new HttpContextAccessor(), new ActingCompanyStore());
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

    private static ClaimsPrincipal Principal(params Claim[] claims) => new(new ClaimsIdentity(claims, "TestAuth"));

    public void Dispose() => _connection.Dispose();

    /// <summary>The service runs as the vendor here (sees all); scoping itself is covered by its own test.</summary>
    private sealed class TestFactory(ApiBillingTests owner) : IDbContextFactory<LosDbContext>
    {
        public LosDbContext CreateDbContext() => owner.NewContext(SuperAdmin());
    }

    private sealed class FakeAuthStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(user));
    }
}
