using System.Security.Claims;
using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace LosLms.Tests;

/// <summary>The owner batch of 30 Oct: remember-me cookie lifetimes, the API usage breakdown / log, and the loan-account backfill.</summary>
public sealed class OwnerBatchTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"batch-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<LosDbContext> _options;
    private static readonly DateTime Now = DateTime.UtcNow;

    public OwnerBatchTests()
    {
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using var db = Ctx();
        db.Database.EnsureCreated();
    }

    private LosDbContext Ctx() => new(_options, TenantContext.ForSeeding());

    // ---- 1. remember me ----

    [Fact]
    public async Task RememberMe_IssuesA7DayCookie_ThatSlides_AndASessionSignInStaysAtLeast8Hours()
    {
        var options = new CookieAuthenticationOptions();
        AuthCookie.Configure(options);
        Assert.True(options.SlidingExpiration);
        Assert.True(options.ExpireTimeSpan >= TimeSpan.FromHours(8));
        Assert.Equal(TimeSpan.FromDays(7), AuthCookie.RememberedFor);

        var scheme = new AuthenticationScheme("Identity.Application", null, typeof(CookieAuthenticationHandler));
        CookieSigningInContext Context(bool persistent) => new(new DefaultHttpContext(), scheme, options, new ClaimsPrincipal(new ClaimsIdentity("x")),
            new AuthenticationProperties { IsPersistent = persistent, ExpiresUtc = DateTimeOffset.UtcNow.Add(options.ExpireTimeSpan) }, new CookieBuilder().Build(new DefaultHttpContext()));

        var remembered = Context(true);
        await options.Events.OnSigningIn(remembered);
        Assert.InRange(remembered.Properties.ExpiresUtc!.Value - DateTimeOffset.UtcNow, TimeSpan.FromDays(7) - TimeSpan.FromMinutes(1), TimeSpan.FromDays(7));

        var session = Context(false);
        var before = session.Properties.ExpiresUtc;
        await options.Events.OnSigningIn(session);
        Assert.Equal(before, session.Properties.ExpiresUtc); // not remembered: unchanged
    }

    // ---- 5. API usage: names, breakdown = summary, CSV ----

    [Fact]
    public async Task UsageLog_NamesEachCall_BreakdownAddsUpToTheSummary_AndTheCsvDefusesFormulas()
    {
        using (var db = Ctx())
        {
            db.Applications.Add(new Application { Id = "A1", CompanyId = 1 });
            db.Applications.Add(new Application { Id = "A2", CompanyId = 1 });
            db.SaveChanges();
            db.Parties.Add(new Party { ApplicationId = "A1", PartyType = "Applicant", FullName = "=cmd Raj Kumar" });
            db.SaveChanges();
            var party = db.Parties.Single().Id;
            db.Users.Add(new ApplicationUser { Id = "u1", UserName = "o@x", Email = "o@x", DisplayName = "Officer One", CompanyId = 1 });
            db.DigioRequests.Add(new DigioRequest { Id = "r-pan", CompanyId = 1, ApplicationId = "A1", PartyId = party, Product = "KYC_ID", Operation = "PAN_VERIFY", DigioId = "PANID", RequestedByUserId = "u1", RequestedAt = Now });
            db.DigioRequests.Add(new DigioRequest { Id = "r-dl", CompanyId = 1, ApplicationId = "A1", PartyId = party, Product = "KYC_ID", Operation = "DIGILOCKER_AADHAAR_PAN", DigioId = "KID1", RequestedByUserId = "u1", RequestedAt = Now });
            db.DigioRequests.Add(new DigioRequest { Id = "r-fail", CompanyId = 1, ApplicationId = "A1", PartyId = party, Product = "KYC_ID", Operation = "PAN_VERIFY", Status = "Failed", RequestedByUserId = "u1", RequestedAt = Now.AddSeconds(-5) });
            db.EsignSigners.Add(new EsignSigner { CompanyId = 1, ApplicationId = "A2", DocumentId = "DID1", PartyType = "Applicant", Name = "Sita Devi", Identifier = "9000000001", SignOrder = 1 });
            db.SaveChanges();
            var signer = db.EsignSigners.Single().Id;
            db.LoanAccounts.Add(new LoanAccount { CompanyId = 1, ApplicationId = "A2", LoanAccountNo = "A2", Principal = 1, Roi = 1, Tenure = 1, FirstEmiDate = new DateOnly(2026, 11, 5), EmiDay = 5, OpenedAt = Now });
            db.SaveChanges();
            var acc = db.LoanAccounts.Single().Id;
            db.Instalments.Add(new Instalment { CompanyId = 1, LoanAccountId = acc, No = 3, DueDate = new DateOnly(2026, 12, 5), Emi = 1 });
            db.SaveChanges();
            db.Presentations.Add(new Presentation { Id = "p1", CompanyId = 1, InstalmentId = db.Instalments.Single().Id, Attempt = 2, DigioPlanId = "NSP9", SettlementDate = new DateOnly(2026, 12, 5) });
            db.ApiUsageLogs.AddRange(
                Log("KYC_ID_VERIFY", "A1", "PANID", true, 1.5m), Log("KYC_DIGILOCKER", "A1", "KID1:aadhaar", true, 2m), Log("KYC_DIGILOCKER", "A1", "KID1:pan", true, 2m),
                Log("KYC_ID_VERIFY", "A1", null, false, 1.5m),                                     // a failed call: no provider id, paired with the failed request
                Log("ESIGN_AADHAAR", "A2", $"DID1:{signer}", true, 10m), Log("MANDATE_DEBIT", "A2", "NSP9", true, 2m),
                Log("KYC_ID_VERIFY", null, "X", true, 1.5m));                                       // a call for no application
            db.SaveChanges();
        }

        var factory = new Factory(this);
        var report = new ApiUsageReport(factory);
        var entries = await report.EntriesAsync(null, DateOnly.FromDateTime(Now.AddHours(5.5)));

        var byWhat = entries.ToDictionary(e => e.Id, e => e);
        Assert.Contains(entries, e => e.What == "PAN check (standalone)" && e.User == "Officer One" && e.Party == "=cmd Raj Kumar" && e.Success);
        Assert.Contains(entries, e => e.What == "Aadhaar via DigiLocker");
        Assert.Contains(entries, e => e.What == "PAN via DigiLocker");
        Assert.All(entries.Where(e => e.ApiCode == "KYC_DIGILOCKER"), e => Assert.Equal("Aadhaar + PAN via DigiLocker (2 documents)", e.Group));
        Assert.Contains(entries, e => e.What == "e-Sign: signer (Sita Devi)");
        Assert.Contains(entries, e => e.What == "EMI debit attempt 2 (EMI 3)");
        var failed = Assert.Single(entries, e => !e.Success);
        Assert.Equal((0m, "Officer One", "=cmd Raj Kumar"), (failed.Amount, failed.User, failed.Party));

        // The breakdown adds up to the monthly summary's subtotal, row for row.
        var summary = await new ApiBillingService(factory, Options.Create(new ApiBillingOptions())).UsageForMonthAsync(null, DateOnly.FromDateTime(Now.AddHours(5.5)));
        var byApp = ApiUsageReport.ByApplication(entries);
        Assert.Equal(summary.Sum(l => l.Amount), byApp.Sum(a => a.Total));
        Assert.Equal(1.5m + 2m + 2m, byApp.Single(a => a.ApplicationId == "A1").Total);
        Assert.Null(byApp[^1].ApplicationId); // no-application bucket last

        var csv = ApiUsageReport.Csv(entries, id => "Acme, Ltd");
        Assert.StartsWith("Date (IST),Company,Application,Party,What,User,Amount (Rs),Result,Code\n", csv);
        Assert.Contains("\"Acme, Ltd\"", csv);
        Assert.Contains("'=cmd Raj Kumar", csv);          // formula-looking text is defused
        Assert.DoesNotContain(",=cmd", csv);
    }

    // ---- 6. backfill ----

    [Fact]
    public async Task Backfill_OpensAccountsForDisbursedLoansWithoutOne_IsIdempotent_AndNeverTouchesExistingOnes()
    {
        using (var db = Ctx())
        {
            // A1: disbursed, complete. A2: disbursed, no EMI date. A3: not disbursed. A4: disbursed, already has an account with a paid EMI.
            foreach (var id in new[] { "A1", "A2", "A3", "A4" })
            {
                db.Applications.Add(new Application { Id = id, CompanyId = 1, Disbursed = id != "A3" });
            }

            db.SaveChanges();
            foreach (var id in new[] { "A1", "A2", "A4" })
            {
                db.ApprovalDecisions.Add(new ApprovalDecision { ApplicationId = id, SanctionedAmount = 600_000m, SanctionedRoi = 12m, SanctionedTenure = 12 });
            }

            db.Disbursements.Add(new Disbursement { ApplicationId = "A1", FirstEmiDate = new DateOnly(2026, 10, 5), EmiDayOfMonth = 5 });
            db.Disbursements.Add(new Disbursement { ApplicationId = "A2" });
            db.LoanAccounts.Add(new LoanAccount { CompanyId = 1, ApplicationId = "A4", LoanAccountNo = "A4", Principal = 1, Roi = 1, Tenure = 1, FirstEmiDate = new DateOnly(2026, 5, 5), EmiDay = 5, OpenedAt = Now });
            db.SaveChanges();
            db.Instalments.Add(new Instalment { CompanyId = 1, LoanAccountId = db.LoanAccounts.Single().Id, No = 1, DueDate = new DateOnly(2026, 5, 5), Emi = 100, Status = "Paid", PaidAmount = 100 });
            db.SaveChanges();
        }

        BackfillResult first, second;
        using (var db = Ctx()) { first = await LoanBackfill.RunAsync(db); }
        using (var db = Ctx()) { second = await LoanBackfill.RunAsync(db); }

        Assert.Equal(1, first.Created);
        Assert.Equal(1, first.AlreadyHad);
        Assert.Equal(["A2: no first EMI date"], first.Skipped);
        Assert.Equal(0, second.Created); // idempotent
        Assert.Equal(2, second.AlreadyHad);

        using var read = Ctx();
        Assert.Equal(["A1", "A4"], read.LoanAccounts.Select(a => a.ApplicationId).OrderBy(x => x).ToArray());
        var a1 = read.LoanAccounts.Single(a => a.ApplicationId == "A1");
        Assert.Equal((600_000m, 12, 5), (a1.Principal, a1.Tenure, a1.EmiDay));
        Assert.Equal(LoanMath.Schedule(600_000m, 12m, 12, new DateOnly(2026, 10, 5)).Count, read.Instalments.Count(i => i.LoanAccountId == a1.Id));
        Assert.All(read.Instalments.Where(i => i.LoanAccountId == a1.Id), i => Assert.Equal("Due", i.Status)); // nothing is marked paid by a backfill
        var a4 = read.LoanAccounts.Single(a => a.ApplicationId == "A4");
        Assert.Equal(1, read.Instalments.Count(i => i.LoanAccountId == a4.Id)); // the existing account is exactly as it was
        Assert.Equal("Paid", read.Instalments.Single(i => i.LoanAccountId == a4.Id).Status);
    }

    // ---- plumbing ----

    private static ApiUsageLog Log(string code, string? app, string? providerRef, bool ok, decimal rate) =>
        new() { CompanyId = 1, ApiCode = code, ApplicationId = app, ProviderRef = providerRef, IsSuccess = ok, UnitRate = rate, CreatedAt = Now };

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_file);
    }

    private sealed class Factory(OwnerBatchTests owner) : IDbContextFactory<LosDbContext>
    {
        public LosDbContext CreateDbContext() => owner.Ctx();
    }
}
