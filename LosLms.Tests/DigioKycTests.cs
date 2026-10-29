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

/// <summary>Stage 1 KYC on Digio: PAN, AML, masking, DigiLocker, name match and the evidence gate. A fake handler stands in for Digio; responses mirror docs/digio/samples.</summary>
public sealed class DigioKycTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"digio-kyc-{Guid.NewGuid():N}.db");
    private readonly string _vaultDir = Path.Combine(Path.GetTempPath(), "los-vault-" + Guid.NewGuid().ToString("N"));
    private readonly DbContextOptions<LosDbContext> _options;
    private readonly ServiceProvider _provider;
    private readonly FakeDigio _digio = new();
    private readonly MemoryFiles _files = new();

    public DigioKycTests()
    {
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using (var db = Anonymous())
        {
            db.Database.EnsureCreated();
            db.Applications.Add(new Application { Id = "A1", CompanyId = 1 });
            db.SaveChanges();
            db.Parties.Add(new Party { ApplicationId = "A1", PartyType = "Applicant", FullName = "Raj Kumar", Pan = "ABCPE1234F", DateOfBirth = new DateOnly(1990, 1, 1) });
            db.DigioSettings.Add(new DigioSettings { CompanyId = 1, PanVerify = true, DigiLocker = true, AadhaarMask = true, Aml = true });
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
        services.AddScoped<DigioKyc>();
        services.AddScoped<DigioEsignService>();
        services.AddScoped<DigioNachService>();
        services.AddScoped<DigioCollect>();
        services.AddScoped<IBounceHandler, BounceService>();
        services.AddScoped<DigioWebhookService>();
        services.AddSingleton<DigioWebhookHealth>();
        services.AddSingleton<DigioBackgroundTasks>();
        services.AddSingleton<DigioStatusNotifier>();
        services.AddSingleton<IFileStore>(_files);
        services.AddSingleton<DigioReconcileTrigger>();
        services.AddScoped<DigioReconciler>();
        _provider = services.BuildServiceProvider();
        _provider.CreateScope().ServiceProvider.GetRequiredService<DigioClient>().SaveCredentials(1, new DigioCredentials("cid", "csecret", "wh", false));
    }

    private DigioKyc Kyc() => _provider.CreateScope().ServiceProvider.GetRequiredService<DigioKyc>();

    private int PartyId()
    {
        using var db = Anonymous();
        return db.Parties.IgnoreQueryFilters().Single().Id;
    }

    // ---- PAN ----

    [Fact]
    public async Task Pan_NameAndDobMatch_IsVerified_AndSetsTheFlagFromEvidence()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"status":"valid","name_as_per_pan_match":true,"date_of_birth_match":true,"category":"individual","aadhaar_seeding_status":"y"}""");

        var o = await Kyc().VerifyPanAsync(1, "A1", PartyId(), "abcpe1234f", "Raj Kumar", new DateOnly(1990, 1, 1), true, "u1");

        Assert.Equal("Verified", o.Verdict);
        using var db = Anonymous();
        Assert.True(db.Parties.IgnoreQueryFilters().Single().PanVerified);
        Assert.Equal("Verified", db.VerificationEvidence.IgnoreQueryFilters().Single().Verdict);
        Assert.Contains("text v2", db.DigioRequests.IgnoreQueryFilters().Single().Consent); // consent is recorded on every request, nothing is ticked
    }

    [Fact]
    public async Task Pan_NameMismatch_IsMismatch_NotVerified()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"status":"valid","name_as_per_pan_match":false,"date_of_birth_match":false,"category":"individual"}""");

        var o = await Kyc().VerifyPanAsync(1, "A1", PartyId(), "ABCPE1234F", "Raj Kumar", null, true, null);

        Assert.Equal("Mismatch", o.Verdict);
        using var db = Anonymous();
        Assert.False(db.Parties.IgnoreQueryFilters().Single().PanVerified);
    }

    [Fact]
    public async Task Pan_NonIndividualCategory_OnAnIndividual_IsMismatch()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"status":"valid","name_as_per_pan_match":true,"category":"company"}""");

        Assert.Equal("Mismatch", (await Kyc().VerifyPanAsync(1, "A1", PartyId(), "ABCPE1234F", "Raj Kumar", null, true, null)).Verdict);
    }

    [Fact]
    public async Task Pan_SameInputsAgain_ReusesTheVerdict_WithoutAnotherCall()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"status":"valid","name_as_per_pan_match":true,"category":"individual"}""");
        var id = PartyId();

        await Kyc().VerifyPanAsync(1, "A1", id, "ABCPE1234F", "Raj Kumar", null, true, null);
        await Kyc().VerifyPanAsync(1, "A1", id, "ABCPE1234F", "Raj Kumar", null, true, null);

        Assert.Single(_digio.Calls);
    }

    [Fact]
    public async Task Pan_BadFormat_OrSwitchedOff_CallsNothing()
    {
        Assert.Null((await Kyc().VerifyPanAsync(1, "A1", PartyId(), "BAD", "Raj Kumar", null, true, null)).Verdict);
        using (var db = Anonymous())
        {
            db.DigioSettings.IgnoreQueryFilters().Single().PanVerify = false;
            db.SaveChanges();
        }

        Assert.Contains("switched off", (await Kyc().VerifyPanAsync(1, "A1", PartyId(), "ABCPE1234F", "Raj Kumar", null, true, null)).Message);
        Assert.Empty(_digio.Calls);
    }

    [Fact]
    public async Task Pan_Digio200WithOnlyAnErrorMessage_IsFailedWithDigiosReason_NotUnavailable()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"error_message":"PAN not found","id":"X1"}""");

        var o = await Kyc().VerifyPanAsync(1, "A1", PartyId(), "ABCPE1234F", "Raj Kumar", null, true, null);

        Assert.Equal("Failed", o.Verdict);
        Assert.Contains("PAN not found", o.Message);
    }

    [Fact]
    public async Task Pan_StatusInvalid_IsFailed_AndAReplyWithNoStatusOrReasonNamesItsFieldsOnly()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"status":"INVALID"}""");
        Assert.Equal("Failed", (await Kyc().VerifyPanAsync(1, "A1", PartyId(), "ABCPE1234F", "Raj Kumar", null, true, null)).Verdict);

        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"pan_status":"E","holder":"SECRET NAME"}""");
        var o = await Kyc().VerifyPanAsync(1, "A1", PartyId(), "ABCPE1234G", "Raj Kumar", null, true, null);

        Assert.Equal("Unavailable", o.Verdict);
        Assert.Contains("pan_status, holder", o.Message);
        Assert.DoesNotContain("SECRET", o.Message);
    }

    // ---- name match ----

    [Theory]
    [InlineData("Raj Kumar Sharma", "SHARMA R K", 100)]
    [InlineData("Raj Kumar Sharma", "Raj Sharma", 67)]
    [InlineData("Mr. Rajesh Kumar", "Rajesh Kumer", 100)]
    [InlineData("Raj Kumar", "Priya Singh", 0)]
    [InlineData("", "Priya", 0)]
    public void NameScore_IsOrderFreeAndForgivesInitialsAndOneTypo(string a, string b, int expected) =>
        Assert.Equal(expected, DigioKyc.NameScore(a, b));

    // ---- AML ----

    [Fact]
    public async Task Aml_Alerts_AreMismatch_AndAnAdminCanAcceptThemWithANote()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"risk_level":"LOW","found":true,"total_alerts":2,"high_severity_alerts":0,"top_match_score":88.5}""");
        var id = PartyId();

        var o = await Kyc().ScreenAmlAsync(1, "A1", id, "Raj Kumar", "ABCPE1234F", null);

        Assert.Equal("Mismatch", o.Verdict);
        Assert.False(await Kyc().AcceptAmlAlertsAsync(1, "A1", id, " ", "admin"));
        Assert.True(await Kyc().AcceptAmlAlertsAsync(1, "A1", id, "Different person, DOB differs", "admin"));
        using var db = Anonymous();
        Assert.Equal("AcceptedMismatch", db.VerificationEvidence.IgnoreQueryFilters().OrderBy(e => e.Id).Last().Verdict);
    }

    [Fact]
    public async Task Aml_NoAlerts_IsVerified()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"risk_level":"LOW","found":false,"total_alerts":0}""");

        Assert.Equal("Verified", (await Kyc().ScreenAmlAsync(1, "A1", PartyId(), "Raj Kumar", null, null)).Verdict);
    }

    [Fact]
    public async Task Aml_NoAlertCount_IsUnavailable_NeverClear()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"risk_level":"LOW"}""");

        Assert.Equal("Unavailable", (await Kyc().ScreenAmlAsync(1, "A1", PartyId(), "Raj Kumar", null, null)).Verdict);
    }

    [Fact]
    public async Task Aml_Digio404_IsReportedAsNotEnabled_AndRecordsNoEvidence()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.NotFound, """{"code":1024,"message":"Requested resource not found"}""");

        var o = await Kyc().ScreenAmlAsync(1, "A1", PartyId(), "Raj Kumar", null, null);

        Assert.True(o.NotEnabled);
        Assert.Contains("Not enabled", o.Message);
        using var db = Anonymous();
        Assert.Empty(db.VerificationEvidence.IgnoreQueryFilters());
    }

    // ---- masking ----

    [Fact]
    public async Task Mask_ReturnsTheMaskedFile_AndSendsPngInDigiosWords()
    {
        string? sent = null;
        _digio.On = (_, _, body) =>
        {
            sent = body;
            return (HttpStatusCode.OK, """{"masked_output":"TUFTS0VE","details":{"masking_done_count":1,"already_masked_count":0}}""");
        };

        var (masked, _) = await Kyc().MaskAadhaarAsync(1, "A1", PartyId(), [1, 2, 3], "image/png", "a.png", null);

        Assert.Equal(Convert.FromBase64String("TUFTS0VE"), masked);
        Assert.Contains("\"data_content_type\":\"PNG\"", sent);
    }

    [Fact]
    public async Task Mask_NothingToMask_IsRefused()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"error_message":"No ID card detected in the image","details":{"already_masked_count":0,"masking_done_count":0}}""");

        var (masked, message) = await Kyc().MaskAadhaarAsync(1, "A1", PartyId(), [1], "image/png", "a.png", null);

        Assert.Null(masked);
        Assert.Contains("No Aadhaar number found", message);
    }

    [Fact]
    public async Task Mask_DigioDown_RefusesTheUpload_SoNoUnmaskedOriginalIsKept()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.InternalServerError, "{}");

        var (masked, message) = await Kyc().MaskAadhaarAsync(1, "A1", PartyId(), [1], "image/png", "a.png", null);

        Assert.Null(masked);
        Assert.Contains("not saved", message);
    }

    [Fact]
    public async Task Mask_RejectsOversizeAndWrongType_WithoutCalling()
    {
        var id = PartyId();
        Assert.Null((await Kyc().MaskAadhaarAsync(1, "A1", id, new byte[1_000_001], "image/png", "a.png", null)).Masked);
        Assert.Null((await Kyc().MaskAadhaarAsync(1, "A1", id, [1], "image/gif", "a.gif", null)).Masked);
        Assert.Empty(_digio.Calls);
    }

    // ---- DigiLocker ----

    private const string Requested = """{"id":"KID1","status":"requested","actions":[{"type":"digilocker","status":"requested"}]}""";
    private const string Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
    private static string DoneWith(string pan) => """{"id":"KID1","status":"approved","actions":[{"type":"digilocker","status":"approved","details":{"aadhaar":{"id_number":"xxxxxxxx9813","name":"RAJ KUMAR","dob":"1990-01-01","gender":"M","father_name":"SURESH KUMAR","image":"__PNG__","current_address_details":{"address":"1 Main Road","locality_or_post_office":"Sadar","district_or_city":"Pune","state":"Maharashtra","pincode":"400001"}},"pan":{"id_number":"__PAN__","name":"RAJ KUMAR"}}}]}""".Replace("__PNG__", Png).Replace("__PAN__", pan);
    private static readonly string Done = DoneWith("ABCPE1234F");

    [Fact]
    public async Task DigiLocker_Start_RecordsTheRequestWithConsentChannelAndMaskedMobile()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"id":"KID1","status":"requested","access_token":{"id":"GWT1"}}""");

        var o = await Kyc().StartDigiLockerAsync(1, "A1", PartyId(), "Raj Kumar", "9876543210", true, "https://x.test", "u1");

        Assert.Equal("Sent", o.Verdict);
        Assert.Contains("/KID1/", o.Url);
        Assert.Contains("redirect_url=", o.Url);
        using var db = Anonymous();
        var row = db.DigioRequests.IgnoreQueryFilters().Single();
        Assert.Equal("WaitingForCustomer", row.Status);
        Assert.Equal("OnDevice", row.CustomerChannel);
        Assert.Equal("XXXXXX3210", row.NotifiedTo);
        Assert.Contains("text v2", row.Consent);
        Assert.Contains("DigiLocker in-journey consent", row.Consent);
    }

    [Fact]
    public async Task DigiLocker_StillRequested_Waits_ThenCompletesOnce_FillsBlanksOnly_AndMetersOnce()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"id":"KID1","status":"requested","access_token":{"id":"GWT1"}}""");
        var start = await Kyc().StartDigiLockerAsync(1, "A1", PartyId(), "Raj Kumar", "9876543210", false, "https://x.test", null);

        _digio.On = (m, _, _) => m == HttpMethod.Post ? (HttpStatusCode.OK, Requested) : (HttpStatusCode.MethodNotAllowed, "{}"); // the real endpoint is POST-only
        Assert.Contains("Waiting", (await Kyc().CompleteDigiLockerAsync(1, start.RequestId!)).Message);

        _digio.On = (m, _, _) => m == HttpMethod.Post ? (HttpStatusCode.OK, Done) : (HttpStatusCode.MethodNotAllowed, "{}"); // the real endpoint is POST-only
        Assert.Equal("Verified", (await Kyc().CompleteDigiLockerAsync(1, start.RequestId!)).Verdict);
        await Kyc().CompleteDigiLockerAsync(1, start.RequestId!); // replay: a no-op

        using var db = Anonymous();
        var party = db.Parties.IgnoreQueryFilters().Single();
        Assert.True(party.AadhaarVerified);
        Assert.Equal("Raj Kumar", party.FullName);   // the officer's value is kept
        Assert.Equal("1 Main Road", party.Address1); // a blank is filled
        Assert.Equal(("Sadar", "Pune", "Maharashtra", "400001"), (party.Address2, party.City, party.State, party.PinCode));
        Assert.Equal(("Male", "SURESH KUMAR"), (party.Gender, party.FatherSpouseName));
        Assert.EndsWith(".png", party.PhotoPath);
        Assert.Single(_files.Saved);
        Assert.True(party.PanVerified);
        Assert.Equal("Verified", db.VerificationEvidence.IgnoreQueryFilters().Single(e => e.Subject == "PAN").Verdict); // from the same DigiLocker answer
        Assert.DoesNotContain(_digio.Calls, c => c.Contains("fetch_id_data"));                                        // no separate PAN call or charge
        Assert.Equal(1, db.ApiUsageLogs.IgnoreQueryFilters().Count(l => l.ApiCode == DigioRates.DigiLockerFetch && l.IsSuccess));
        Assert.Equal(1, db.VerificationEvidence.IgnoreQueryFilters().Count(e => e.Subject == "AADHAAR"));
    }

    [Fact]
    public async Task DigiLocker_IfDigioAnswers405ToPost_TheStatusIsReadWithGet()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"id":"KID1","status":"requested"}""");
        var start = await Kyc().StartDigiLockerAsync(1, "A1", PartyId(), "Raj Kumar", "9876543210", false, "https://x.test", null);

        _digio.On = (m, _, _) => m == HttpMethod.Get ? (HttpStatusCode.OK, Done) : (HttpStatusCode.MethodNotAllowed, """{"message":"Method not allowed"}""");

        Assert.Equal("Verified", (await Kyc().CompleteDigiLockerAsync(1, start.RequestId!)).Verdict);
    }

    [Fact]
    public async Task DigiLocker_PanInTheReplyThatDiffersFromTheOneOnFile_IsMismatch_AndAnExistingPhotoIsKept()
    {
        using (var db = Anonymous())
        {
            db.Parties.IgnoreQueryFilters().Single().PhotoPath = "App_Data/uploads/A1/Applicant/live.jpg";
            db.SaveChanges();
        }

        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"id":"KID1","status":"requested"}""");
        var start = await Kyc().StartDigiLockerAsync(1, "A1", PartyId(), "Raj Kumar", "9876543210", false, "https://x.test", null);
        _digio.On = (m, _, _) => m == HttpMethod.Post ? (HttpStatusCode.OK, DoneWith("ZZZPE9999Z")) : (HttpStatusCode.MethodNotAllowed, "{}");

        await Kyc().CompleteDigiLockerAsync(1, start.RequestId!);

        using var read = Anonymous();
        Assert.Equal("Mismatch", read.VerificationEvidence.IgnoreQueryFilters().Single(e => e.Subject == "PAN").Verdict);
        Assert.False(read.Parties.IgnoreQueryFilters().Single().PanVerified);
        Assert.Equal("App_Data/uploads/A1/Applicant/live.jpg", read.Parties.IgnoreQueryFilters().Single().PhotoPath);
        Assert.Empty(_files.Saved);
    }

    [Fact]
    public async Task DigiLocker_Expired_ClosesTheRequest_WithoutEvidence()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"id":"KID1","status":"requested"}""");
        var start = await Kyc().StartDigiLockerAsync(1, "A1", PartyId(), "Raj Kumar", "9876543210", false, "https://x.test", null);

        _digio.On = (m, _, _) => m == HttpMethod.Post ? (HttpStatusCode.OK, """{"id":"KID1","status":"expired"}""") : (HttpStatusCode.MethodNotAllowed, "{}");
        await Kyc().CompleteDigiLockerAsync(1, start.RequestId!);

        using var db = Anonymous();
        Assert.Equal("Expired", db.DigioRequests.IgnoreQueryFilters().Single().Status);
        Assert.Empty(db.VerificationEvidence.IgnoreQueryFilters());
    }

    [Fact]
    public async Task DigiLocker_ReturnHint_AndTheReconciler_CompleteItWithoutTheOfficerRefreshing()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"id":"KID1","status":"requested"}""");
        var start = await Kyc().StartDigiLockerAsync(1, "A1", PartyId(), "Raj Kumar", "9876543210", false, "https://x.test", null);
        _digio.On = (m, _, _) => m == HttpMethod.Post ? (HttpStatusCode.OK, Done) : (HttpStatusCode.MethodNotAllowed, "{}"); // the real endpoint is POST-only

        var reconciler = _provider.CreateScope().ServiceProvider.GetRequiredService<DigioReconciler>();
        Assert.Equal(1, await reconciler.ReconcileAsync(DateTime.UtcNow));

        using var db = Anonymous();
        Assert.Equal("Succeeded", db.DigioRequests.IgnoreQueryFilters().Single().Status);
        Assert.True(db.Parties.IgnoreQueryFilters().Single().AadhaarVerified);
        Assert.Single(db.ApiUsageLogs.IgnoreQueryFilters().Where(l => l.ApiCode == DigioRates.DigiLockerFetch));
        await _provider.GetRequiredService<DigioReconcileTrigger>().RequestReconcileAsync(start.RequestId!); // already done: a no-op
        Assert.Single(db.ApiUsageLogs.IgnoreQueryFilters().Where(l => l.ApiCode == DigioRates.DigiLockerFetch));
    }

    // ---- the gate ----

    private async Task<GateResult> Gate(bool production = false)
    {
        // An officer of company 1 (the gate runs inside a signed-in session, unlike the webhook-style contexts above).
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "officer"), new Claim(TenantContext.CompanyIdClaim, "1")], "test"));
        var tenant = new TenantContext(new Staff(user), new HttpContextAccessor(), new ActingCompanyStore());
        await tenant.EnsureLoadedAsync();
        using var db = new LosDbContext(_options, tenant);
        return await KycEvidenceCheck.GateAsync(db, "A1", production);
    }

    [Fact]
    public async Task Gate_NothingSwitchedOn_RequiresNothing()
    {
        using (var db = Anonymous())
        {
            var s = db.DigioSettings.IgnoreQueryFilters().Single();
            s.PanVerify = s.DigiLocker = s.Aml = false;
            db.SaveChanges();
        }

        Assert.True((await Gate()).CanAdvance);
    }

    [Fact]
    public async Task Gate_MissingChecks_BlockWithAKycBypassRequest_AndCurrentEvidenceClearsIt()
    {
        var blocked = await Gate();
        Assert.False(blocked.CanAdvance);
        Assert.Equal(AdminRequest.KycBypass, blocked.RequestType);
        Assert.Contains("PAN", blocked.BlockedReason);

        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"status":"valid","name_as_per_pan_match":true,"date_of_birth_match":true,"category":"individual"}""");
        var id = PartyId();
        await Kyc().VerifyPanAsync(1, "A1", id, "ABCPE1234F", "Raj Kumar", new DateOnly(1990, 1, 1), true, null);
        using (var db = Anonymous())
        {
            var now = DateTime.UtcNow;
            db.VerificationEvidence.Add(new VerificationEvidence { CompanyId = 1, ApplicationId = "A1", PartyId = id, Subject = "AADHAAR", Verdict = "Verified", Environment = "Sandbox", CreatedAt = now });
            db.VerificationEvidence.Add(new VerificationEvidence
            {
                CompanyId = 1, ApplicationId = "A1", PartyId = id, Subject = "AML", Verdict = "Verified", Environment = "Sandbox", CreatedAt = now,
                InputFingerprint = KycEvidenceCheck.AmlFingerprint(db.Parties.IgnoreQueryFilters().Single()),
            });
            db.SaveChanges();
        }

        Assert.True((await Gate()).CanAdvance);
        Assert.False((await Gate(production: true)).CanAdvance); // sandbox evidence never satisfies production
    }

    [Fact]
    public async Task Gate_EditingThePan_MakesTheVerdictStale()
    {
        _digio.On = (_, _, _) => (HttpStatusCode.OK, """{"status":"valid","name_as_per_pan_match":true,"date_of_birth_match":true,"category":"individual"}""");
        var id = PartyId();
        using (var db = Anonymous())
        {
            var s = db.DigioSettings.IgnoreQueryFilters().Single();
            s.DigiLocker = s.Aml = false;
            db.SaveChanges();
        }

        await Kyc().VerifyPanAsync(1, "A1", id, "ABCPE1234F", "Raj Kumar", new DateOnly(1990, 1, 1), true, null);
        Assert.True((await Gate()).CanAdvance);

        using (var db = Anonymous())
        {
            db.Parties.IgnoreQueryFilters().Single().Pan = "ABCPE9999F";
            db.SaveChanges();
        }

        Assert.False((await Gate()).CanAdvance);
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

    private sealed class Factory(DigioKycTests owner) : IDbContextFactory<LosDbContext>
    {
        public LosDbContext CreateDbContext() => owner.Anonymous();
    }

    private sealed class MemoryFiles : IFileStore
    {
        public Dictionary<string, byte[]> Saved { get; } = [];

        public Task SaveAsync(string key, byte[] bytes)
        {
            Saved[key] = bytes;
            return Task.CompletedTask;
        }

        public Task<Stream?> OpenReadAsync(string key) => Task.FromResult<Stream?>(null);
    }

    private sealed class Staff(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
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

    private sealed class FakeDigio : IHttpClientFactory
    {
        public Func<HttpMethod, string, string, (HttpStatusCode, string)> On { get; set; } = (_, _, _) => (HttpStatusCode.OK, "{}");
        public List<string> Calls { get; } = [];

        public HttpClient CreateClient(string name) => new(new Handler(this));

        private sealed class Handler(FakeDigio owner) : HttpMessageHandler
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
