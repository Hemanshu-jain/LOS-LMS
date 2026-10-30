using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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
/// Phase 4: Vehicle RC checks and business KYB (GSTIN / CIN). The RC answer is the captured sandbox sample (docs/digio/samples/vehicle-rc.json),
/// the GST "No records found" answer likewise. A GST/CIN success has no sample yet, so those answers are synthetic and the code is
/// tested to refuse a pass without a recognised Active status.
/// </summary>
public sealed class DigioAssetTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 10, 28);
    private static readonly string RcSample = Sample("vehicle-rc.json");
    private static readonly string GstSample = Sample("gst-verify.json");

    private readonly string _file = Path.Combine(Path.GetTempPath(), $"digio-asset-{Guid.NewGuid():N}.db");
    private readonly string _vaultDir = Path.Combine(Path.GetTempPath(), "los-vault-" + Guid.NewGuid().ToString("N"));
    private readonly DbContextOptions<LosDbContext> _options;
    private readonly ServiceProvider _provider;
    private readonly Fake _digio = new();

    public DigioAssetTests()
    {
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using (var db = Anonymous())
        {
            db.Database.EnsureCreated();
            db.Applications.Add(new Application { Id = "A1", CompanyId = 1 });
            db.DigioSettings.Add(new DigioSettings { CompanyId = 1, Rc = true, Kyb = true });
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
        services.AddScoped<DigioAsset>();
        _provider = services.BuildServiceProvider();
        _provider.CreateScope().ServiceProvider.GetRequiredService<DigioClient>().SaveCredentials(1, new DigioCredentials("cid", "csecret", "wh", false));
    }

    private DigioAsset Asset() => _provider.CreateScope().ServiceProvider.GetRequiredService<DigioAsset>();

    private static RcInputs Inputs(string? chassis = "DDFBEH11430", string? engine = "DDMBEH11521", string? insurer = null, string? applicant = "Test Name", string? lender = "Acme Finance") =>
        new("MH12AB1234", chassis, engine, insurer, applicant, lender, Today);

    // ---- RC ----

    [Fact]
    public void RcSample_GivesTheSixChecks_AndTheCancelledRcIsAProblem()
    {
        using var doc = JsonDocument.Parse(RcSample);

        var rows = RcChecks.Evaluate(doc.RootElement, Inputs()).ToDictionary(r => r.Label, r => r.State);

        Assert.Equal("ok", rows["Owner"]);          // TEST NAME = TEST NAME
        Assert.Equal("ok", rows["Chassis"]);
        Assert.Equal("ok", rows["Engine"]);
        Assert.Equal("ok", rows["Hypothecation"]);  // financed: false
        Assert.Equal("ok", rows["Insurance"]);      // valid to 2029-07-20
        Assert.Equal("bad", rows["RC status"]);     // "RC CANCEL"
        Assert.Equal("warn", rows["Fitness"]);      // fit up to 2013
        Assert.DoesNotContain("Registration", rows.Keys);
    }

    [Fact]
    public void Rc_ChassisAndEngineCompareTheLastFiveCharacters_SoAMaskedValueStillMatches()
    {
        using var doc = JsonDocument.Parse(RcSample);

        var masked = RcChecks.Evaluate(doc.RootElement, Inputs(chassis: "XXXXXXX11430", engine: "zzzzzz11521")).ToDictionary(r => r.Label, r => r.State);
        var wrong = RcChecks.Evaluate(doc.RootElement, Inputs(chassis: "DDFBEH99999")).ToDictionary(r => r.Label, r => r.State);

        Assert.Equal("ok", masked["Chassis"]);
        Assert.Equal("ok", masked["Engine"]);
        Assert.Equal("bad", wrong["Chassis"]);
    }

    [Fact]
    public void Rc_ExistingFinancierIsAProblem_UnlessItIsThisLender()
    {
        var other = Edit(RcSample, "financed", true, "financier", "STATE BANK OF INDIA");
        var us = Edit(RcSample, "financed", true, "financier", "Acme Finance Ltd");

        using var a = JsonDocument.Parse(other);
        using var b = JsonDocument.Parse(us);

        Assert.Equal("bad", RcChecks.Evaluate(a.RootElement, Inputs()).Single(r => r.Label == "Hypothecation").State);
        Assert.Equal("ok", RcChecks.Evaluate(b.RootElement, Inputs()).Single(r => r.Label == "Hypothecation").State);
    }

    [Fact]
    public void Rc_ExpiredInsurance_IsAProblem_AndADifferentOwnerOnlyAWarning()
    {
        var expired = Edit(RcSample, "financed", false, "insurance_upto", "2026-01-01");
        using var doc = JsonDocument.Parse(expired);

        var rows = RcChecks.Evaluate(doc.RootElement, Inputs(applicant: "Somebody Else")).ToDictionary(r => r.Label, r => r.State);

        Assert.Equal("bad", rows["Insurance"]);
        Assert.Equal("warn", rows["Owner"]); // a used vehicle's owner is the seller: look, don't fail
    }

    [Fact]
    public async Task VerifyRc_OnTheSample_RecordsMismatch_FillsBlanks_AndDoesNotCallTwiceForTheSameEntries()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, RcSample);

        var first = await Asset().VerifyRcAsync(1, "A1", Inputs(), "u1");
        var again = await Asset().VerifyRcAsync(1, "A1", Inputs(), "u1");

        Assert.Equal("Mismatch", first.Outcome.Verdict);   // the sample RC is cancelled
        Assert.Contains("RC status", first.Outcome.Message);
        Assert.Equal("BAJAJ AUTO LTD CALIBER", first.Fill!.MakeModel);
        Assert.Equal("1998", first.Fill.MfgYear);
        Assert.Equal("Mismatch", again.Outcome.Verdict);
        Assert.NotNull(again.Response);
        var call = Assert.Single(_digio.Calls);
        Assert.Equal("/client/v4/apis/verify/vehicle_rc", call.Path);
        using var body = JsonDocument.Parse(call.Body);
        Assert.Equal("MH12AB1234", body.RootElement.GetProperty("id").GetString());
        Assert.False(string.IsNullOrEmpty(body.RootElement.GetProperty("unique_request_id").GetString()));

        // A changed chassis is a different question, but the check is on file: refused until an Admin allows one re-check.
        var locked = await Asset().VerifyRcAsync(1, "A1", Inputs(chassis: "DDFBEH11999"), "u1");
        Assert.Equal(VerificationLock.Refusal, locked.Outcome.Message);
        Assert.Single(_digio.Calls);
        using (var admin = new LosDbContext(_options, TenantContext.ForSeeding()))
        {
            Assert.Null(await VerificationLock.GrantAsync(admin, 1, "A1", null, "VEHICLE_RC", "chassis was mistyped", "admin1", null));
        }

        await Asset().VerifyRcAsync(1, "A1", Inputs(chassis: "DDFBEH11999"), "u1");
        Assert.Equal(2, _digio.Calls.Count);

        using var db = Anonymous();
        Assert.All(db.VerificationEvidence.IgnoreQueryFilters().ToList(), e => Assert.Equal("VEHICLE_RC", e.Subject));
    }

    [Fact]
    public async Task VerifyRc_AllGreen_IsVerified()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, Edit(Edit(RcSample, "rc_status", "ACTIVE", "fit_up_to", "2030-01-01"), "financed", false));

        var r = await Asset().VerifyRcAsync(1, "A1", Inputs(), null);

        Assert.Equal("Verified", r.Outcome.Verdict);
    }

    [Fact]
    public async Task VerifyRc_SwitchedOff_NoRegistration_OrNoRecord_NeverCallsOrNeverVerifies()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"message":"nothing"}""");

        var none = await Asset().VerifyRcAsync(1, "A1", Inputs(), null);
        var blank = await Asset().VerifyRcAsync(1, "A1", Inputs() with { RegNo = " " }, null);
        using (var db = Anonymous()) { db.DigioSettings.IgnoreQueryFilters().Single().Rc = false; db.SaveChanges(); }
        var off = await Asset().VerifyRcAsync(1, "A1", Inputs(), null);

        Assert.Equal("Unavailable", none.Outcome.Verdict);
        Assert.Null(blank.Outcome.Verdict);
        Assert.Contains("switched off", off.Outcome.Message);
        Assert.Single(_digio.Calls);
    }

    // ---- KYB ----

    [Fact]
    public async Task Gst_NoRecordsFound_IsFailed_NotVerified_AndFillsNothing()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, GstSample);

        var r = await Asset().VerifyBusinessAsync(1, "A1", "27AAAAA0939A1ZV", null, "Test Firm", "u1");

        Assert.Equal("Failed", r.Results.Single().Verdict);
        Assert.Contains("not found", r.Results.Single().Message);
        Assert.Null(r.Data);
        Assert.Equal("/v3/client/kyc/fetch_id_data/GST", _digio.Calls.Single().Path);
        using var db = Anonymous();
        var e = db.VerificationEvidence.IgnoreQueryFilters().Single();
        Assert.Equal(("GSTIN", "Failed"), (e.Subject, e.Verdict));
    }

    [Fact]
    public async Task Gst_Active_WithTheSameName_IsVerified_AndHandsBackTheRegisterName()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"legal_name":"TEST FIRM PRIVATE LIMITED","gstin_status":"Active","registration_date":"2015-04-01"}""");

        var r = await Asset().VerifyBusinessAsync(1, "A1", "27AAAAA0939A1ZV", null, "Test Firm Private Limited", null);

        Assert.Equal("Verified", r.Results.Single().Verdict);
        Assert.Equal("TEST FIRM PRIVATE LIMITED", r.Data!.LegalName);
        Assert.Null(r.Data.IncorporationDate); // the GST registration date is not the incorporation date
    }

    [Theory]
    [InlineData("""{"legal_name":"TEST FIRM","gstin_status":"Cancelled"}""", "Mismatch")]
    [InlineData("""{"legal_name":"COMPLETELY OTHER TRADERS","gstin_status":"Active"}""", "Mismatch")]
    [InlineData("""{"legal_name":"TEST FIRM"}""", "Unavailable")]   // no recognisable status: never a pass
    public async Task Gst_NotActive_DifferentName_OrNoStatus_IsNeverVerified(string answer, string expected)
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, answer);

        var r = await Asset().VerifyBusinessAsync(1, "A1", "27AAAAA0939A1ZV", null, "Test Firm", null);

        Assert.Equal(expected, r.Results.Single().Verdict);
    }

    [Fact]
    public async Task Cin_Active_FillsTheIncorporationDate_AndBadFormatsCallNothing()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"company_name":"TEST FIRM LIMITED","company_status":"Active","date_of_incorporation":"2012-06-15"}""");

        var bad = await Asset().VerifyBusinessAsync(1, "A1", "123", "nope", "Test Firm", null);
        var ok = await Asset().VerifyBusinessAsync(1, "A1", null, "U12345MH2012PTC123456", "Test Firm Limited", null);

        Assert.All(bad.Results, x => Assert.Null(x.Verdict));
        Assert.Equal("Verified", ok.Results.Single().Verdict);
        Assert.Equal(new DateOnly(2012, 6, 15), ok.Data!.IncorporationDate);
        Assert.Equal("/v3/client/kyc/fetch_id_data/CIN", Assert.Single(_digio.Calls).Path);
    }

    [Fact]
    public void Apply_FillsBlanksOnly()
    {
        var firm = new Business { FirmName = "Typed By Hand" };
        var result = new BusinessKycResult(true, new BusinessKycData("REGISTER NAME", "Active", new DateOnly(2012, 6, 15), null));

        BusinessKycService.Apply(firm, result);
        var empty = new Business();
        BusinessKycService.Apply(empty, result);

        Assert.Equal("Typed By Hand", firm.FirmName);
        Assert.Equal(new DateOnly(2012, 6, 15), firm.IncorpDate);
        Assert.Equal("REGISTER NAME", empty.FirmName);
    }

    // ---- plumbing ----

    /// <summary>The captured body inside a sample file ("response" is the capture wrapper; the HTTP body is what is inside it).</summary>
    private static string Body(string sample) => JsonDocument.Parse(sample).RootElement.GetProperty("response").GetRawText();

    private static string Sample(string name)
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "docs", "digio", "samples")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        return Body(File.ReadAllText(Path.Combine(dir!, "docs", "digio", "samples", name)));
    }

    /// <summary>Sets leaf values by name anywhere in the sample (financed, rc_status, insurance_upto...), as a new body.</summary>
    private static string Edit(string json, params object[] pairs)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(json)!;
        for (var i = 0; i < pairs.Length; i += 2)
        {
            Set(node, (string)pairs[i], pairs[i + 1]);
        }

        return node.ToJsonString();
    }

    private static void Set(System.Text.Json.Nodes.JsonNode node, string name, object value)
    {
        if (node is System.Text.Json.Nodes.JsonObject o)
        {
            if (o.ContainsKey(name)) { o[name] = JsonSerializer.SerializeToNode(value); }
            foreach (var child in o.Select(p => p.Value).Where(v => v is not null).ToList()) { Set(child!, name, value); }
        }
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

    private sealed class Factory(DigioAssetTests owner) : IDbContextFactory<LosDbContext>
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

    private sealed record Call(string Path, string Body);

    private sealed class Fake : IHttpClientFactory
    {
        public Func<HttpMethod, string, string, (HttpStatusCode, string)> On { get; set; } = (_, _, _) => (HttpStatusCode.OK, "{}");
        public List<Call> Calls { get; } = [];

        public HttpClient CreateClient(string name) => new(new Handler(this));

        private sealed class Handler(Fake owner) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
                lock (owner.Calls) { owner.Calls.Add(new(request.RequestUri!.AbsolutePath, body)); }
                var (code, text) = owner.On(request.Method, request.RequestUri!.PathAndQuery, body);
                return new HttpResponseMessage(code) { Content = new StringContent(text, Encoding.UTF8) };
            }
        }
    }
}
