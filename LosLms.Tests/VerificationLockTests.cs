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

/// <summary>
/// The re-verify lock (owner rule: no accidental API over-use): a check with a result on file is refused on the SERVER with nothing sent to
/// Digio, until an Admin allows ONE re-check with a reason (audited). Covers every Digio check.
/// </summary>
public sealed class VerificationLockTests : IDisposable
{
    private const string Pan = "ABCPE1234F";
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"lock-{Guid.NewGuid():N}.db");
    private readonly string _vaultDir = Path.Combine(Path.GetTempPath(), "los-vault-" + Guid.NewGuid().ToString("N"));
    private readonly DbContextOptions<LosDbContext> _options;
    private readonly ServiceProvider _provider;
    private readonly Fake _digio = new();

    public VerificationLockTests()
    {
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using (var db = Anonymous())
        {
            db.Database.EnsureCreated();
            db.Applications.Add(new Application { Id = "A1", CompanyId = 1 });
            db.SaveChanges();
            db.Parties.Add(new Party { ApplicationId = "A1", PartyType = "Applicant", FullName = "Raj Kumar", Pan = Pan, DateOfBirth = new DateOnly(1990, 1, 1), Mobile = "9876543210" });
            db.DigioSettings.Add(new DigioSettings { CompanyId = 1, PanVerify = true, DigiLocker = true, AadhaarMask = true, Aml = true, Rc = true, Kyb = true });
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
        services.AddSingleton<IFileStore, NoFiles>();
        services.AddScoped<DigioKyc>();
        services.AddScoped<DigioAsset>();
        _provider = services.BuildServiceProvider();
        _provider.CreateScope().ServiceProvider.GetRequiredService<DigioClient>().SaveCredentials(1, new DigioCredentials("cid", "csecret", "wh", false));
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"status":"valid","name_as_per_pan_match":true,"date_of_birth_match":true,"category":"individual"}""");
    }

    private DigioKyc Kyc() => _provider.CreateScope().ServiceProvider.GetRequiredService<DigioKyc>();

    private DigioAsset Asset() => _provider.CreateScope().ServiceProvider.GetRequiredService<DigioAsset>();

    private int PartyId()
    {
        using var db = Anonymous();
        return db.Parties.IgnoreQueryFilters().Single().Id;
    }

    private Task<KycOutcome> RunPan() => Kyc().VerifyPanAsync(1, "A1", PartyId(), Pan, "Raj Kumar", new DateOnly(1990, 1, 1), true, "u1");

    // ---- PAN: refused, no HTTP; admin grant = one run ----

    [Fact]
    public async Task Verified_IsRefusedOnTheServer_AndNothingIsSentToDigio()
    {
        await RunPan();
        var again = await Kyc().VerifyPanAsync(1, "A1", PartyId(), Pan, "Raj Kumar Changed", new DateOnly(1990, 1, 1), true, "u1"); // different inputs: not the cached shortcut

        Assert.Null(again.Verdict);
        Assert.Equal(VerificationLock.Refusal, again.Message);
        Assert.Single(_digio.Calls);
        using var db = Anonymous();
        var s = await VerificationLock.StatusAsync(db, 1, "A1", PartyId(), "PAN");
        Assert.True(s.Locked);
        Assert.StartsWith("Verified on ", s.Reason);
    }

    [Fact]
    public async Task AdminGrant_AllowsExactlyOneRun_ThenItLocksAgain_AndIsAudited()
    {
        await RunPan();
        var party = PartyId();
        using (var admin = new LosDbContext(_options, TenantContext.ForSeeding()))
        {
            Assert.Null(await VerificationLock.GrantAsync(admin, 1, "A1", party, "PAN", "customer corrected the name spelling", "admin1", "Admin One"));
            Assert.Contains("already allowed", await VerificationLock.GrantAsync(admin, 1, "A1", party, "PAN", "again please", "admin1", "Admin One"));
        }

        using (var db = Anonymous()) { Assert.False((await VerificationLock.StatusAsync(db, 1, "A1", party, "PAN")).Locked); }

        var rerun = await Kyc().VerifyPanAsync(1, "A1", party, Pan, "Raj Kumar Corrected", new DateOnly(1990, 1, 1), true, "u1");
        Assert.Equal("Verified", rerun.Verdict);
        Assert.Equal(2, _digio.Calls.Count);

        var third = await Kyc().VerifyPanAsync(1, "A1", party, Pan, "Raj Kumar Again", new DateOnly(1990, 1, 1), true, "u1");
        Assert.Equal(VerificationLock.Refusal, third.Message);
        Assert.Equal(2, _digio.Calls.Count);

        using var read = Anonymous();
        var grant = read.VerificationRechecks.IgnoreQueryFilters().Single();
        Assert.Equal(("PAN", "customer corrected the name spelling", "admin1", "Admin One"), (grant.Subject, grant.Reason, grant.GrantedByUserId, grant.GrantedByName));
        Assert.NotNull(grant.UsedAt);
        Assert.True((await VerificationLock.StatusAsync(read, 1, "A1", party, "PAN")).Locked);
    }

    [Fact]
    public async Task OnlyAnAdmin_WithAReason_CanGrant_AndOnlyForSomethingThatIsLocked()
    {
        var party = PartyId();
        using (var admin = new LosDbContext(_options, TenantContext.ForSeeding()))
        {
            Assert.Contains("nothing to re-check", await VerificationLock.GrantAsync(admin, 1, "A1", party, "PAN", "a good reason here", "a", null));
            await RunPan();
            Assert.Contains("reason", await VerificationLock.GrantAsync(admin, 1, "A1", party, "PAN", " ok ", "a", null));
        }

        using var officer = Anonymous(); // not signed in as an Admin
        Assert.Contains("Only an Admin", await VerificationLock.GrantAsync(officer, 1, "A1", party, "PAN", "a good reason here", "u1", null));
        Assert.Empty(officer.VerificationRechecks.IgnoreQueryFilters());
    }

    [Fact]
    public async Task TwoClicksCannotShareOneGrant()
    {
        await RunPan();
        var party = PartyId();
        using (var admin = new LosDbContext(_options, TenantContext.ForSeeding()))
        {
            await VerificationLock.GrantAsync(admin, 1, "A1", party, "PAN", "customer changed the name", "admin1", null);
        }

        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(async _ =>
        {
            using var db = Anonymous();
            return await VerificationLock.TryBeginAsync(db, 1, "A1", party, "PAN");
        }));

        Assert.Equal(1, results.Count(r => r));
    }

    [Fact]
    public async Task FailedOrUnavailableChecks_DoNotLock_SoTheyCanBeRetried()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"status":"invalid"}""");
        var first = await RunPan();
        Assert.Equal("Failed", first.Verdict);

        var second = await Kyc().VerifyPanAsync(1, "A1", PartyId(), Pan, "Raj Kumar Fixed", new DateOnly(1990, 1, 1), true, "u1");

        Assert.NotEqual(VerificationLock.Refusal, second.Message);
        Assert.Equal(2, _digio.Calls.Count);
    }

    // ---- PAN-only after DigiLocker; DigiLocker waiting ----

    [Fact]
    public async Task PanOnly_IsRefusedAfterDigiLockerHasVerifiedThePan()
    {
        Seed(new VerificationEvidence { PartyId = PartyId(), Subject = "PAN", Verdict = "Verified", Summary = "From DigiLocker", DigioRequestId = "dl1" });

        var o = await RunPan();

        Assert.Equal(VerificationLock.Refusal, o.Message);
        Assert.Empty(_digio.Calls);
    }

    [Fact]
    public async Task DigiLocker_AWaitingRequestBlocksANewOne_AndSoDoesAFinishedAadhaar()
    {
        var party = PartyId();
        using (var db = Anonymous())
        {
            db.DigioRequests.Add(new DigioRequest { Id = "r1", CompanyId = 1, ApplicationId = "A1", PartyId = party, Product = "KYC_ID", Operation = "DIGILOCKER_AADHAAR_PAN", Status = "WaitingForCustomer", RequestedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(3) });
            db.SaveChanges();
        }

        var waiting = await Kyc().StartDigiLockerAsync(1, "A1", party, "Raj Kumar", "9876543210", false, "http://x/", "u1");
        Assert.Contains("already waiting", waiting.Message);

        using (var db = Anonymous()) { db.DigioRequests.IgnoreQueryFilters().Single().Status = "Cancelled"; db.SaveChanges(); }
        Seed(new VerificationEvidence { PartyId = party, Subject = "AADHAAR", Verdict = "Verified" });
        var done = await Kyc().StartDigiLockerAsync(1, "A1", party, "Raj Kumar", "9876543210", false, "http://x/", "u1");

        Assert.Equal(VerificationLock.Refusal, done.Message);
        Assert.Empty(_digio.Calls);
    }

    [Fact]
    public async Task DigiLocker_AnExpiredWaitingRequest_NoLongerBlocks()
    {
        var party = PartyId();
        using (var db = Anonymous())
        {
            db.DigioRequests.Add(new DigioRequest { Id = "r1", CompanyId = 1, ApplicationId = "A1", PartyId = party, Product = "KYC_ID", Operation = "DIGILOCKER_AADHAAR_PAN", Status = "WaitingForCustomer", RequestedAt = DateTime.UtcNow.AddDays(-9), ExpiresAt = DateTime.UtcNow.AddDays(-2) });
            db.SaveChanges();
        }
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"id":"KID1"}""");

        var o = await Kyc().StartDigiLockerAsync(1, "A1", party, "Raj Kumar", "9876543210", false, "http://x/", "u1");

        Assert.Equal("Sent", o.Verdict);
        Assert.Single(_digio.Calls);
    }

    // ---- AML, masking, RC, KYB, bank ----

    [Fact]
    public async Task Aml_Masking_Rc_AndKyb_AreAllLockedOnceDone_WithNothingSent()
    {
        var party = PartyId();
        _digio.On = (_, path, _) => path.Contains("/client/v1/user/screen") ? (HttpStatusCode.OK, """{"total_alerts":0,"risk_level":"LOW"}""")
            : path.Contains("aadhaar/mask") ? (HttpStatusCode.OK, "{\"masked_output\":\"" + Convert.ToBase64String([1, 2, 3]) + "\",\"details\":{\"masking_done_count\":1}}")
            : path.Contains("vehicle_rc") ? (HttpStatusCode.OK, """{"vehicle_details":{"rc_number":"MH12AB1234","rc_status":"ACTIVE"},"user_details":{"owner_name":"X"},"finance_details":{"financed":false}}""")
            : (HttpStatusCode.OK, """{"legal_name":"TEST FIRM","gstin_status":"Active"}""");

        Assert.Equal("Verified", (await Kyc().ScreenAmlAsync(1, "A1", party, "Raj Kumar", Pan, "u")).Verdict);
        Assert.NotNull((await Kyc().MaskAadhaarAsync(1, "A1", party, [1], "image/png", "a.png", "u")).Masked);
        Assert.NotNull((await Asset().VerifyRcAsync(1, "A1", new RcInputs("MH12AB1234", null, null, null, "X", "L", new DateOnly(2026, 10, 30)), "u")).Response);
        Assert.Equal("Verified", (await Asset().VerifyBusinessAsync(1, "A1", "27AAAAA0939A1ZV", null, "Test Firm", "u")).Results.Single().Verdict);
        var sent = _digio.Calls.Count;
        Assert.Equal(4, sent);

        Assert.Equal(VerificationLock.Refusal, (await Kyc().ScreenAmlAsync(1, "A1", party, "Raj Kumar 2", Pan, "u")).Message);
        var mask = await Kyc().MaskAadhaarAsync(1, "A1", party, [1], "image/png", "a.png", "u");
        Assert.Equal((null, VerificationLock.Refusal), (mask.Masked, mask.Message));
        Assert.Equal(VerificationLock.Refusal, (await Asset().VerifyRcAsync(1, "A1", new RcInputs("MH12AB1234", "NEWCHASSIS", null, null, "X", "L", new DateOnly(2026, 10, 30)), "u")).Outcome.Message);
        Assert.Contains("already verified", (await Asset().VerifyBusinessAsync(1, "A1", "27AAAAA0939A1ZV", null, "Test Firm", "u")).Results.Single().Message);
        Assert.Equal(sent, _digio.Calls.Count);

        // CIN was never checked, so it still runs while GSTIN stays locked.
        var both = await Asset().VerifyBusinessAsync(1, "A1", "27AAAAA0939A1ZV", "U12345MH2012PTC123456", "Test Firm", "u");
        Assert.Equal(2, both.Results.Count);
        Assert.Contains("already verified", both.Results[0].Message);
        Assert.Equal(sent + 1, _digio.Calls.Count);
    }

    [Fact]
    public async Task BankPennyDrop_AnExistingResultOnTheBankRow_CountsAsDone_AndOneGrantReopensIt()
    {
        using (var db = Anonymous())
        {
            db.BankDetails.Add(new BankDetail { ApplicationId = "A1", PennyDropStatus = "Matched", PennyDropCheckedAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc) });
            db.SaveChanges();
        }

        using (var db = Anonymous())
        {
            var s = await VerificationLock.StatusAsync(db, 1, "A1", null, "BANK_ACCOUNT");
            Assert.True(s.Locked);
            Assert.False(await VerificationLock.TryBeginAsync(db, 1, "A1", null, "BANK_ACCOUNT"));
        }

        using (var admin = new LosDbContext(_options, TenantContext.ForSeeding()))
        {
            Assert.Null(await VerificationLock.GrantAsync(admin, 1, "A1", null, "BANK_ACCOUNT", "customer changed the account", "admin1", null));
        }

        using var again = Anonymous();
        Assert.True(await VerificationLock.TryBeginAsync(again, 1, "A1", null, "BANK_ACCOUNT"));
        Assert.False(await VerificationLock.TryBeginAsync(again, 1, "A1", null, "BANK_ACCOUNT"));
    }

    // ---- plumbing ----

    private void Seed(VerificationEvidence e)
    {
        using var db = Anonymous();
        e.CompanyId = 1;
        e.ApplicationId = "A1";
        e.CreatedAt = DateTime.UtcNow;
        db.VerificationEvidence.Add(e);
        db.SaveChanges();
    }

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

    private sealed class NoFiles : IFileStore
    {
        public Task SaveAsync(string key, byte[] bytes) => Task.CompletedTask;

        public Task<Stream?> OpenReadAsync(string key) => Task.FromResult<Stream?>(null);
    }

    private sealed class Factory(VerificationLockTests owner) : IDbContextFactory<LosDbContext>
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

    private sealed class Fake : IHttpClientFactory
    {
        public Func<HttpMethod, string, string, (HttpStatusCode, string)> On { get; set; } = (_, _, _) => (HttpStatusCode.OK, "{}");
        public List<string> Calls { get; } = [];

        public HttpClient CreateClient(string name) => new(new Handler(this));

        private sealed class Handler(Fake owner) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
                lock (owner.Calls) { owner.Calls.Add(request.RequestUri!.PathAndQuery); }
                var (code, text) = owner.On(request.Method, request.RequestUri!.PathAndQuery, body);
                return new HttpResponseMessage(code) { Content = new StringContent(text, Encoding.UTF8) };
            }
        }
    }
}
