using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

/// <summary>
/// Slice 2 of the servicing ledger: the scheduler presents each due instalment once (unique_key = our id), the debit confirms through
/// Digio's GET, and ReceiptService is the only path to Paid. Digio is faked; the plan / transaction shapes come from the spec appendix
/// (no sandbox debit sample exists yet).
/// </summary>
public sealed class DigioCollectTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"collect-{Guid.NewGuid():N}.db");
    private readonly string _vaultDir = Path.Combine(Path.GetTempPath(), "los-vault-" + Guid.NewGuid().ToString("N"));
    private readonly DbContextOptions<LosDbContext> _options;
    private readonly ServiceProvider _provider;
    private readonly Fake _digio = new();
    private readonly DateTime _now = DateTime.UtcNow;
    private readonly DateOnly _today;

    public DigioCollectTests()
    {
        _today = DateOnly.FromDateTime(_now.AddHours(5.5));
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using (var db = Anonymous())
        {
            db.Database.EnsureCreated();
            db.Applications.Add(new Application { Id = "A1", CompanyId = 1 });
            db.SaveChanges();
            db.Parties.Add(new Party { ApplicationId = "A1", PartyType = "Applicant", FullName = "TEST APPLICANT" });
            db.EnachMandates.Add(new EnachMandate { ApplicationId = "A1", AccountNumber = "123456789012", DigioState = "Registered", Umrn = "UMRN0001", Status = "Registered" });
            db.DigioSettings.Add(new DigioSettings { CompanyId = 1, Enach = true, Debits = true, CorporateAccountSandbox = "9900001111", EnachConfigIdSandbox = "CFG-SBX" });
            db.SaveChanges();
            var app = db.Applications.IgnoreQueryFilters().Single();
            // first EMI due the day after tomorrow (inside the 2-day lead window), then monthly
            LoanAccountService.OpenAsync(db, app, 120_000m, 12m, 12, _today.AddDays(2), null, null, null).GetAwaiter().GetResult();
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
        services.AddSingleton<DigioStatusNotifier>();
        services.AddScoped<ApiBillingService>();
        services.AddScoped<DigioClient>();
        services.AddScoped<DigioCollect>();
        services.AddScoped<IBounceHandler, BounceService>();
        _provider = services.BuildServiceProvider();
        _provider.CreateScope().ServiceProvider.GetRequiredService<DigioClient>().SaveCredentials(1, new DigioCredentials("cid", "csecret", "wh", false));
        _digio.On("POST", "/v3/client/nach_debit/scheduled/register", _ => """{"id":"NSP1","status":"scheduled","upcoming_transaction":{"state":"SCHEDULED"}}""");
    }

    private DigioCollect Collect() => _provider.CreateScope().ServiceProvider.GetRequiredService<DigioCollect>();

    // ---- scheduler ----

    [Fact]
    public async Task Scheduler_PresentsEachDueInstalmentOnce_WithOurIdAsTheUniqueKey_AndARerunAddsNone()
    {
        var first = await Collect().PresentDueAsync(_now);
        var second = await Collect().PresentDueAsync(_now);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        var call = Assert.Single(_digio.Calls, c => c.Path == "/v3/client/nach_debit/scheduled/register");
        using var body = JsonDocument.Parse(call.Body);
        var root = body.RootElement;
        using var db = Anonymous();
        var p = db.Presentations.IgnoreQueryFilters().Single();
        Assert.Equal(p.Id, root.GetProperty("unique_key").GetString());
        Assert.Equal("UMRN0001", root.GetProperty("umrn").GetString());
        Assert.Equal("onetime", root.GetProperty("frequency").GetString());
        Assert.Equal("9900001111", root.GetProperty("corporate_account_number").GetString());
        var i1 = db.Instalments.IgnoreQueryFilters().Single(i => i.No == 1);
        Assert.Equal((long)(i1.Emi * 100m), root.GetProperty("amount_in_paise").GetInt64());
        Assert.Equal(i1.DueDate.ToString("yyyy-MM-dd"), root.GetProperty("settlement_date").GetString());
        Assert.Equal(("Sent", "NSP1", "Presented"), (p.Status, p.DigioPlanId, i1.Status));
    }

    [Fact]
    public async Task Scheduler_PresentsNothing_WhenTheMandateIsNotRegistered_TheSwitchIsOff_OrTheDateIsOutsideTheWindow()
    {
        using (var db = Anonymous()) { db.EnachMandates.IgnoreQueryFilters().Single().DigioState = "AuthSuccess"; db.SaveChanges(); }
        Assert.Equal(0, await Collect().PresentDueAsync(_now));

        using (var db = Anonymous()) { var m = db.EnachMandates.IgnoreQueryFilters().Single(); m.DigioState = "Registered"; db.DigioSettings.IgnoreQueryFilters().Single().Debits = false; db.SaveChanges(); }
        Assert.Equal(0, await Collect().PresentDueAsync(_now));

        using (var db = Anonymous()) { db.DigioSettings.IgnoreQueryFilters().Single().Debits = true; db.SaveChanges(); }
        Assert.Equal(0, await Collect().PresentDueAsync(_now.AddDays(-5))); // the EMI is more than 2 days away
        Assert.Empty(_digio.Calls);
    }

    [Fact]
    public async Task Scheduler_ResendsAPresentationDigioNeverAccepted_WithTheSameUniqueKey()
    {
        _digio.On("POST", "/v3/client/nach_debit/scheduled/register", _ => throw new HttpRequestException("down"));
        await Collect().PresentDueAsync(_now);
        string id;
        using (var db = Anonymous())
        {
            var p = db.Presentations.IgnoreQueryFilters().Single();
            id = p.Id;
            Assert.Equal(("Created", null), (p.Status, p.DigioPlanId));
            Assert.Equal("Due", db.Instalments.IgnoreQueryFilters().Single(i => i.No == 1).Status);
        }

        _digio.On("POST", "/v3/client/nach_debit/scheduled/register", _ => """{"id":"NSP9","status":"scheduled"}""");
        await Collect().PresentDueAsync(_now.AddHours(1));

        using var read = Anonymous();
        var again = read.Presentations.IgnoreQueryFilters().Single();
        Assert.Equal((id, "Sent", "NSP9"), (again.Id, again.Status, again.DigioPlanId));
        Assert.All(_digio.Calls.Where(c => c.Method == "POST"), c => Assert.Contains(id, c.Body)); // both tries carried the same unique_key
    }

    [Fact]
    public void Scheduler_RunsAt1800Ist()
    {
        var before = new DateTime(2026, 10, 28, 5, 0, 0, DateTimeKind.Utc); // 10:30 IST
        var after = new DateTime(2026, 10, 28, 13, 0, 0, DateTimeKind.Utc); // 18:30 IST

        Assert.Equal(TimeSpan.FromHours(7.5), CollectionScheduler.UntilNext(before));
        Assert.Equal(TimeSpan.FromHours(23.5), CollectionScheduler.UntilNext(after));
    }

    // ---- confirm / webhook / receipts ----

    [Fact]
    public async Task Success_ConfirmedByGet_MakesOneReceipt_AndAReplayMakesNoSecond()
    {
        await Collect().PresentDueAsync(_now);
        var id = PresentationId();
        _digio.On("GET", $"/v3/client/nach_debit/scheduled/by_reference_id/{id}", _ => Plan("PAYMENT_SUCCESS", utr: "UTR123"));

        Assert.True(await Collect().HandleEventAsync(1, Event("NACH.DEBIT.SUCCESS", "NSP1")));
        Assert.True(await Collect().HandleEventAsync(1, Event("NACH.DEBIT.SUCCESS", "NSP1")));   // redelivery
        Assert.True(await Collect().ConfirmAsync(1, id));                                          // and the reconciler as well

        using var db = Anonymous();
        var receipt = Assert.Single(db.Receipts.IgnoreQueryFilters().ToList());
        Assert.Equal($"LOS-RCPT-{_today.Year}-00001", receipt.ReceiptNo);
        Assert.Equal(("NACH", "UTR123", "Approved", id), (receipt.Source, receipt.Reference, receipt.Status, receipt.PresentationId));
        var i1 = db.Instalments.IgnoreQueryFilters().Single(i => i.No == 1);
        Assert.Equal(("Paid", i1.Emi), (i1.Status, i1.PaidAmount));
        Assert.Equal("Succeeded", db.Presentations.IgnoreQueryFilters().Single().Status);
    }

    [Fact]
    public async Task OutOfOrderEvents_NeverMoveAPresentationBackwards()
    {
        await Collect().PresentDueAsync(_now);
        var id = PresentationId();
        var path = $"/v3/client/nach_debit/scheduled/by_reference_id/{id}";

        _digio.On("GET", path, _ => Plan("PAYMENT_SUCCESS", utr: "U1"));
        await Collect().HandleEventAsync(1, Event("NACH.DEBIT.SUCCESS", "NSP1"));
        _digio.On("GET", path, _ => Plan("SUBMITTED_TO_BANK"));              // a late "submitted" event arrives after the result
        await Collect().HandleEventAsync(1, Event("NACH.DEBIT.TXN.SUBMITTEDTOBANK", "NSP1"));
        _digio.On("GET", path, _ => Plan("PAYMENT_FAILED", code: "51"));    // and even a contradicting one
        await Collect().ConfirmAsync(1, id);

        using var db = Anonymous();
        Assert.Equal("Succeeded", db.Presentations.IgnoreQueryFilters().Single().Status);
        Assert.Equal("Paid", db.Instalments.IgnoreQueryFilters().Single(i => i.No == 1).Status);
        Assert.Single(db.Receipts.IgnoreQueryFilters());
    }

    [Fact]
    public async Task InProgress_ThenFailure_BouncesTheInstalment_WithTheCode_AndNoReceipt()
    {
        await Collect().PresentDueAsync(_now);
        var id = PresentationId();
        var path = $"/v3/client/nach_debit/scheduled/by_reference_id/{id}";

        _digio.On("GET", path, _ => Plan("SUBMITTED_TO_BANK"));
        await Collect().ConfirmAsync(1, id);
        using (var db = Anonymous()) { Assert.Equal("InProgress", db.Presentations.IgnoreQueryFilters().Single().Status); }

        _digio.On("GET", path, _ => Plan("PAYMENT_FAILED", code: "51", reason: "Insufficient funds"));
        await Collect().ConfirmAsync(1, id);

        using var read = Anonymous();
        var p = read.Presentations.IgnoreQueryFilters().Single();
        Assert.Equal(("Failed", "51", "Insufficient funds"), (p.Status, p.FailureCode, p.FailureReason));
        Assert.Equal("Bounced", read.Instalments.IgnoreQueryFilters().Single(i => i.No == 1).Status);
        Assert.Empty(read.Receipts.IgnoreQueryFilters());
        Assert.Equal("Bounce", Assert.Single(read.LoanCharges.IgnoreQueryFilters()).Kind); // the bounce hook ran (slice 3)
    }

    [Fact]
    public async Task ABouncedInstalment_CanBePresentedAgain_AsAttemptTwo()
    {
        await Collect().PresentDueAsync(_now);
        var id = PresentationId();
        _digio.On("GET", $"/v3/client/nach_debit/scheduled/by_reference_id/{id}", _ => Plan("PAYMENT_FAILED", code: "04"));
        await Collect().ConfirmAsync(1, id);
        int instalmentId;
        using (var db = Anonymous()) { instalmentId = db.Instalments.IgnoreQueryFilters().Single(i => i.No == 1).Id; }
        _digio.On("POST", "/v3/client/nach_debit/scheduled/register", _ => """{"id":"NSP2","status":"scheduled"}""");

        Assert.True(await Collect().StartAsync(instalmentId, 2));
        Assert.False(await Collect().StartAsync(instalmentId, 2)); // not Bounced any more: nothing doubled

        using var read = Anonymous();
        Assert.Equal(new[] { 1, 2 }, read.Presentations.IgnoreQueryFilters().OrderBy(x => x.Attempt).Select(x => x.Attempt).ToArray());
        Assert.Equal("Presented", read.Instalments.IgnoreQueryFilters().Single(i => i.No == 1).Status);
    }

    [Fact]
    public async Task ADebitThatNeverReachedTheBank_LeavesTheInstalmentDue()
    {
        await Collect().PresentDueAsync(_now);
        var id = PresentationId();
        _digio.On("GET", $"/v3/client/nach_debit/scheduled/by_reference_id/{id}", _ => Plan("REJECT_SPO_BANK"));

        await Collect().ConfirmAsync(1, id);

        using var db = Anonymous();
        Assert.Equal("NotPresented", db.Presentations.IgnoreQueryFilters().Single().Status);
        Assert.Equal("Due", db.Instalments.IgnoreQueryFilters().Single(i => i.No == 1).Status);
    }

    [Fact]
    public async Task AnEventWeCannotPlace_IsIgnored_AndAFailedLookupThrowsSoTheInboxRetries()
    {
        await Collect().PresentDueAsync(_now);

        Assert.False(await Collect().HandleEventAsync(1, Event("NACH.DEBIT.SUCCESS", "NSP-UNKNOWN")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Collect().HandleEventAsync(1, Event("NACH.DEBIT.SUCCESS", "NSP1"))); // GET not programmed: Digio "did not answer"
    }

    [Fact]
    public async Task OpenPresentations_AreWhatTheReconcilerPolls_AndFinalOnesAreNot()
    {
        await Collect().PresentDueAsync(_now);
        var id = PresentationId();
        Assert.Equal([(1, id)], await Collect().OpenAsync());

        _digio.On("GET", $"/v3/client/nach_debit/scheduled/by_reference_id/{id}", _ => Plan("PAYMENT_SUCCESS", utr: "U"));
        await Collect().ConfirmAsync(1, id); // the missed webhook, recovered by the poll

        Assert.Empty(await Collect().OpenAsync());
    }

    // ---- receipts: the only path to Paid ----

    [Fact]
    public async Task ManualReceipt_CountsOnlyAfterAnAdminApproves_NeverTheRecorder()
    {
        using var db = Anonymous();
        var account = db.LoanAccounts.IgnoreQueryFilters().Single();
        var i2 = db.Instalments.IgnoreQueryFilters().Single(i => i.No == 2);
        var r = await ReceiptService.RecordManualAsync(db, account, i2.Id, "Cash", i2.Emi, _today, "cash book 7", "officer1");
        await db.SaveChangesAsync();
        Assert.Equal("Pending", r.Status);
        Assert.Equal("Due", db.Instalments.IgnoreQueryFilters().Single(i => i.No == 2).Status);

        Assert.NotNull(await ReceiptService.ApproveManualAsync(db, r.Id, "officer2", isElevated: false));        // not an Admin
        Assert.NotNull(await ReceiptService.ApproveManualAsync(db, r.Id, "officer1", isElevated: true));         // the recorder
        Assert.Null(await ReceiptService.ApproveManualAsync(db, r.Id, "admin1", isElevated: true));

        using var read = Anonymous();
        var paid = read.Instalments.IgnoreQueryFilters().Single(i => i.No == 2);
        Assert.Equal(("Paid", i2.Emi), (paid.Status, paid.PaidAmount));
        Assert.Equal(("Approved", "admin1"), (read.Receipts.IgnoreQueryFilters().Single().Status, read.Receipts.IgnoreQueryFilters().Single().ApprovedByUserId));
        Assert.NotNull(await ReceiptService.ApproveManualAsync(db, r.Id, "admin2", isElevated: true));             // already approved: not counted twice
    }

    [Fact]
    public async Task ReceiptNumbers_CountUpPerCompanyAndYear()
    {
        using var db = Anonymous();
        var account = db.LoanAccounts.IgnoreQueryFilters().Single();
        var a = await ReceiptService.RecordManualAsync(db, account, null, "Cheque", 100m, new DateOnly(2026, 11, 1), "c1", "u");
        await db.SaveChangesAsync();
        var b = await ReceiptService.RecordManualAsync(db, account, null, "Cheque", 100m, new DateOnly(2026, 11, 2), "c2", "u");
        var c = await ReceiptService.RecordManualAsync(db, account, null, "Cash", 100m, new DateOnly(2027, 1, 2), "c3", "u");
        await db.SaveChangesAsync();

        Assert.Equal(["LOS-RCPT-2026-00001", "LOS-RCPT-2026-00002", "LOS-RCPT-2027-00001"], new[] { a.ReceiptNo, b.ReceiptNo, c.ReceiptNo });
    }

    [Fact]
    public void NothingButReceiptService_AssignsPaidOrPartPaid()
    {
        var root = AppContext.BaseDirectory;
        while (root is not null && !Directory.Exists(Path.Combine(root, "LosLms", "Services"))) { root = Path.GetDirectoryName(root); }
        var assign = new Regex("""Status\s*=(?!=)\s*"(Paid|PartPaid)"|"Paid"\s*:\s*"PartPaid"|\?\s*"Paid"\s*:""");

        var offenders = Directory.EnumerateFiles(Path.Combine(root!, "LosLms"), "*.*", SearchOption.AllDirectories)
            .Where(f => (f.EndsWith(".cs") || f.EndsWith(".razor")) && !f.Contains(Path.Combine("Migrations", "")) && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) && Path.GetFileName(f) != "ReceiptService.cs")
            .Where(f => assign.IsMatch(File.ReadAllText(f))).ToList();

        Assert.Empty(offenders);
    }

    // ---- plumbing ----

    private string PresentationId()
    {
        using var db = Anonymous();
        return db.Presentations.IgnoreQueryFilters().Single().Id;
    }

    private static string Plan(string txnStatus, string? utr = null, string? code = null, string? reason = null) =>
        JsonSerializer.Serialize(new
        {
            id = "NSP1", umrn = "UMRN0001", status = "scheduled",
            last_transaction = new { id = "NPT1", umrn = "UMRN0001", state = "x", details = new { status = txnStatus, utr, failure_code = code, failure_reason = reason } },
        });

    private static JsonElement Event(string name, string planId) => JsonDocument.Parse(
        "{\"id\":\"wh-" + Guid.NewGuid().ToString("N")[..6] + "\",\"event\":\"" + name + "\",\"entities\":[\"nach_debit\"],\"payload\":{\"nach_debit\":{\"id\":\"" + planId + "\"}}}").RootElement.Clone();

    private LosDbContext Anonymous()
    {
        var tenant = new TenantContext(new NoUser(), new HttpContextAccessor(), new ActingCompanyStore());
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

    private sealed class Factory(DigioCollectTests owner) : IDbContextFactory<LosDbContext>
    {
        public LosDbContext CreateDbContext() => owner.Anonymous();
    }

    private sealed class NoUser : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
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

    private sealed record Call(string Method, string Path, string Body);

    private sealed class Fake : IHttpClientFactory
    {
        private readonly Dictionary<(string, string), Func<string, string>> _routes = [];
        public List<Call> Calls { get; } = [];

        public void On(string method, string path, Func<string, string> reply) { lock (_routes) { _routes[(method, path)] = reply; } }

        public HttpClient CreateClient(string name) => new(new Handler(this));

        private sealed class Handler(Fake owner) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = Uri.UnescapeDataString(request.RequestUri!.AbsolutePath);
                var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
                lock (owner.Calls) { owner.Calls.Add(new(request.Method.Method, path, body)); }
                Func<string, string>? reply;
                lock (owner._routes) { owner._routes.TryGetValue((request.Method.Method, path), out reply); }
                return reply is null
                    ? new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{}") }
                    : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(reply(body)) };
            }
        }
    }
}
