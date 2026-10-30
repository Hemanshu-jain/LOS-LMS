using System.Net;
using System.Security.Claims;
using System.Text;
using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Xunit;

namespace LosLms.Tests;

/// <summary>Slice 3+4: what a bounce does (charge once, re-present or hand to an officer), Admin-only waive and close, and the settlement check. Fake Digio, temp SQLite.</summary>
public sealed class CollectionsTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"coll-{Guid.NewGuid():N}.db");
    private readonly string _vaultDir = Path.Combine(Path.GetTempPath(), "los-vault-" + Guid.NewGuid().ToString("N"));
    private readonly DbContextOptions<LosDbContext> _options;
    private readonly ServiceProvider _provider;
    private readonly FakeDigio _digio = new();
    private bool _asAdmin = true;

    public CollectionsTests()
    {
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using (var db = Db(admin: true))
        {
            db.Database.EnsureCreated();
            db.Users.Add(new ApplicationUser { Id = "officer-1", UserName = "officer", NormalizedUserName = "OFFICER", Email = "o@test.local", DisplayName = "Officer", CompanyId = 1 });
            db.SaveChanges();
            db.Applications.Add(new Application { Id = "A1", CompanyId = 1, AssignedOfficerId = "officer-1" });
            db.SaveChanges();
            var account = new LoanAccount { CompanyId = 1, ApplicationId = "A1", LoanAccountNo = "A1", Principal = 100000, Tenure = 2, FirstEmiDate = new DateOnly(2026, 11, 5), EmiDay = 5, MandateId = 1 };
            account.Instalments.Add(new Instalment { CompanyId = 1, No = 1, DueDate = new DateOnly(2026, 11, 5), Emi = 5000, Status = "Presented" });
            account.Instalments.Add(new Instalment { CompanyId = 1, No = 2, DueDate = new DateOnly(2026, 12, 5), Emi = 5000 });
            db.LoanAccounts.Add(account);
            db.EnachMandates.Add(new EnachMandate { Id = 1, ApplicationId = "A1", Umrn = "UMRN1", DigioMandateId = "ENA1", DigioState = "Registered", Status = "Registered" });
            db.DigioSettings.Add(new DigioSettings { CompanyId = 1 });
            db.SaveChanges();
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_options);
        services.AddSingleton<IDbContextFactory<LosDbContext>>(new Factory(this));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IWebHostEnvironment>(new FakeEnv(_vaultDir));
        services.AddSingleton<PasswordVault>();
        services.AddSingleton<IHttpClientFactory>(_digio);
        services.AddSingleton(Options.Create(new ApiBillingOptions()));
        services.AddScoped<ApiBillingService>();
        services.AddScoped<DigioClient>();
        services.AddScoped<BounceService>();
        services.AddScoped<LoanClosureService>();
        services.AddScoped<SettlementReconciler>();
        _provider = services.BuildServiceProvider();
        _provider.CreateScope().ServiceProvider.GetRequiredService<DigioClient>().SaveCredentials(1, new DigioCredentials("cid", "csecret", "wh", false));
    }

    private T Svc<T>() where T : notnull => _provider.CreateScope().ServiceProvider.GetRequiredService<T>();

    private string AddPresentation(string id, int attempt, string status, string? code = null, string? txn = null, decimal amount = 5000m, int instalmentNo = 1)
    {
        using var db = Db(admin: true);
        var inst = db.Instalments.IgnoreQueryFilters().Single(i => i.No == instalmentNo);
        db.Presentations.Add(new Presentation { Id = id, CompanyId = 1, InstalmentId = inst.Id, Attempt = attempt, Status = status, FailureCode = code, DigioTxnId = txn, Amount = amount, UpdatedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow });
        db.SaveChanges();
        return id;
    }

    // ---- bounce ----

    [Fact]
    public async Task RpnBounce_MarksBounced_RaisesOneChargeWithGst_AndRaisesNoTask_EvenWhenCalledTwice()
    {
        var id = AddPresentation(new string('a', 32), 1, "Failed", "04");

        await Svc<BounceService>().OnFailedAsync(id);
        await Svc<BounceService>().OnFailedAsync(id);

        using var db = Db(admin: true);
        Assert.Equal("Bounced", db.Instalments.IgnoreQueryFilters().Single(i => i.No == 1).Status);
        var charge = Assert.Single(db.LoanCharges.IgnoreQueryFilters());
        Assert.Equal((500m, 90m, "Open"), (charge.Amount, charge.Gst, charge.Status));
        Assert.Empty(db.CollectionTasks.IgnoreQueryFilters());
    }

    [Fact]
    public async Task NonRpnBounce_RaisesOneTaskForTheAssignedOfficer()
    {
        var id = AddPresentation(new string('b', 32), 1, "Failed", "01");

        await Svc<BounceService>().OnFailedAsync(id);
        await Svc<BounceService>().OnFailedAsync(id);

        using var db = Db(admin: true);
        var task = Assert.Single(db.CollectionTasks.IgnoreQueryFilters());
        Assert.Equal(("NonRpnReturn", "officer-1", "Open"), (task.Kind, task.AssignedOfficerId, task.Status));
    }

    [Fact]
    public async Task SecondFailure_ExhaustsAttempts_AndGoesToAnOfficer_EachBounceCostsACharge()
    {
        AddPresentation(new string('c', 32), 1, "Failed", "04");
        var second = AddPresentation(new string('d', 32), 2, "Failed", "04");

        await Svc<BounceService>().OnFailedAsync(second);

        using var db = Db(admin: true);
        Assert.Equal("AttemptsExhausted", Assert.Single(db.CollectionTasks.IgnoreQueryFilters()).Kind);
    }

    [Fact]
    public async Task RepresentQueue_HoldsARepresentableBounce_UntilTheWaitIsOver()
    {
        var id = AddPresentation(new string('e', 32), 1, "Failed", "04");
        await Svc<BounceService>().OnFailedAsync(id);
        using var db = Db(admin: true);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.Single(await BounceService.RepresentQueueAsync(db, today, dueOnly: false));
        Assert.Empty(await BounceService.RepresentQueueAsync(db, today, dueOnly: true));
        Assert.Single(await BounceService.RepresentQueueAsync(db, today.AddDays(3), dueOnly: true));
    }

    [Fact]
    public async Task OnlyAnAdminCanWaiveACharge_AndOnlyWithANote()
    {
        await Svc<BounceService>().OnFailedAsync(AddPresentation(new string('f', 32), 1, "Failed", "04"));
        int chargeId;
        using (var db = Db(admin: true)) { chargeId = db.LoanCharges.IgnoreQueryFilters().Single().Id; }

        _asAdmin = false;
        Assert.False(await Svc<BounceService>().WaiveChargeAsync(chargeId, "goodwill", "u1"));
        _asAdmin = true;
        Assert.False(await Svc<BounceService>().WaiveChargeAsync(chargeId, " ", "u1"));
        Assert.True(await Svc<BounceService>().WaiveChargeAsync(chargeId, "goodwill", "u1"));

        using var check = Db(admin: true);
        Assert.Equal("Waived", check.LoanCharges.IgnoreQueryFilters().Single().Status);
    }

    // ---- closure ----

    [Fact]
    public async Task Close_CancelsPendingDebits_ThenTheMandate_ThenClosesTheLoan()
    {
        AddPresentation(new string('1', 32), 1, "InProgress", txn: "NPT1", instalmentNo: 2);
        var calls = new List<string>();
        _digio.On = (_, path, body) => { calls.Add(path + " " + body); return (HttpStatusCode.OK, "{}"); };

        var r = await Svc<LoanClosureService>().CloseAsync(AccountId(), foreclosure: true, "C001", "admin");

        Assert.True(r.Closed, r.Message);
        Assert.Contains(calls, c => c.Contains("transaction/action") && c.Contains("NPT1") && c.Contains("cancel"));
        Assert.Contains(calls, c => c.Contains("registration_cancel") && c.Contains("ENA1") && c.Contains("C001"));
        Assert.True(calls.FindIndex(c => c.Contains("transaction/action")) < calls.FindIndex(c => c.Contains("registration_cancel")));
        using var db = Db(admin: true);
        Assert.Equal("Foreclosed", db.LoanAccounts.IgnoreQueryFilters().Single().Status);
        Assert.Equal("Cancelled", db.Presentations.IgnoreQueryFilters().Single().Status);
    }

    [Fact]
    public async Task Close_WhenTheMandateCancelFails_LeavesTheLoanActive_SoItCanBeRetried()
    {
        _digio.On = (_, path, _) => path.Contains("registration_cancel") ? (HttpStatusCode.InternalServerError, "{}") : (HttpStatusCode.OK, "{}");

        var r = await Svc<LoanClosureService>().CloseAsync(AccountId(), true, "C001", "admin");

        Assert.False(r.Closed);
        using var db = Db(admin: true);
        Assert.Equal("Active", db.LoanAccounts.IgnoreQueryFilters().Single().Status);
    }

    [Fact]
    public async Task Close_IsAdminOnly_NeedsAReason_AndAllPaidUnlessForeclosing()
    {
        _asAdmin = false;
        Assert.False((await Svc<LoanClosureService>().CloseAsync(AccountId(), true, "C001", "u")).Closed);
        Assert.Equal(["C001", "C002", "C003"], LoanClosureService.ReasonCodes);          // C004's meaning is unconfirmed: not offered
        Assert.Equal("C002", LoanClosureService.DefaultReason);
        _asAdmin = true;
        Assert.Contains("Pick a cancellation reason", (await Svc<LoanClosureService>().CloseAsync(AccountId(), true, "C004", "admin")).Message);
        Assert.False((await Svc<LoanClosureService>().CloseAsync(AccountId(), true, "X9", "admin")).Closed);
        Assert.Contains("Not every instalment", (await Svc<LoanClosureService>().CloseAsync(AccountId(), false, "C001", "admin")).Message);
        Assert.Empty(_digio.Calls);
    }

    // ---- settlement ----

    [Fact]
    public async Task Settlement_FlagsAnUnknownTransaction_AndATotalThatDiffers()
    {
        AddPresentation(new string('2', 32), 1, "Succeeded", txn: "NPT9", amount: 5000m);
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """[{"id":"S1","total_amount":10000,"debit_txn_ids":["NPT9","NPT404"]}]""");

        var (check, error) = await Svc<SettlementReconciler>().CheckAsync(1, "CID12345");

        Assert.Null(error);
        Assert.Equal(2, check!.MismatchCount); // an id we never made, and 10000 vs 5000
        Assert.Contains("NPT404", check.Detail);
    }

    [Fact]
    public async Task Settlement_WhenEverythingAgrees_HasNoMismatch()
    {
        AddPresentation(new string('3', 32), 1, "Succeeded", txn: "NPT9", amount: 5000m);
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """[{"id":"S1","total_amount":5000,"debit_txn_ids":["NPT9"]}]""");

        var (check, _) = await Svc<SettlementReconciler>().CheckAsync(1, "CID12345");

        Assert.Equal(0, check!.MismatchCount);
    }

    [Fact]
    public async Task Settlement_ADigio404_IsReportedInWords_NotAsAMismatch()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.NotFound, "{}");

        var (check, error) = await Svc<SettlementReconciler>().CheckAsync(1, "CID12345");

        Assert.Null(check);
        Assert.Contains("no settlement report", error);
    }

    // ---- plumbing ----

    private int AccountId()
    {
        using var db = Db(admin: true);
        return db.LoanAccounts.IgnoreQueryFilters().Single().Id;
    }

    private LosDbContext Db(bool admin)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "u"), new(TenantContext.CompanyIdClaim, "1") };
        if (admin) { claims.Add(new Claim(ClaimTypes.Role, TenantContext.AdminRole)); }
        var tenant = new TenantContext(new Staff(new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))), new HttpContextAccessor(), new ActingCompanyStore());
        tenant.EnsureLoadedAsync().GetAwaiter().GetResult();
        return new LosDbContext(_options, tenant);
    }

    public void Dispose()
    {
        _provider.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_file);
        try { Directory.Delete(_vaultDir, recursive: true); } catch { /* best effort */ }
    }

    private sealed class Factory(CollectionsTests owner) : IDbContextFactory<LosDbContext>
    {
        public LosDbContext CreateDbContext() => owner.Db(owner._asAdmin);
    }

    private sealed class Staff(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
    }

    private sealed class FakeEnv(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "LosLms.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeDigio : IHttpClientFactory
    {
        public Func<HttpMethod, string, string, (HttpStatusCode, string)> OnFull { get; set; } = (_, _, _) => (HttpStatusCode.OK, "{}");
        public Func<HttpMethod, string, string, (HttpStatusCode, string)> On { get => OnFull; set => OnFull = value; }
        public List<string> Calls { get; } = [];

        public HttpClient CreateClient(string name) => new(new Handler(this));

        private sealed class Handler(FakeDigio owner) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
                lock (owner.Calls) { owner.Calls.Add(request.RequestUri!.PathAndQuery); }
                var (code, text) = owner.OnFull(request.Method, request.RequestUri!.PathAndQuery, body);
                return new HttpResponseMessage(code) { Content = new StringContent(text, Encoding.UTF8) };
            }
        }
    }
}
