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
/// Digio Phase 3: multi-party sequential e-Sign (callback = our request id, agreement hash = fingerprint, cancel + re-send on change,
/// per-signer rows), the e-NACH state machine and its create / confirm path, and the release-funds gate. Digio is faked; the shapes the
/// fake returns come from the spec appendix (no sandbox samples captured yet).
/// </summary>
public sealed class DigioPhase3Tests : IDisposable
{
    private const string Key = "App_Data/uploads/A1/postsanction/agreement-1.pdf";
    private static readonly byte[] PdfV1 = Encoding.ASCII.GetBytes("%PDF-1.4 agreement v1");
    private static readonly byte[] PdfV2 = Encoding.ASCII.GetBytes("%PDF-1.4 agreement v2");

    private readonly string _file = Path.Combine(Path.GetTempPath(), $"digio-p3-{Guid.NewGuid():N}.db");
    private readonly string _vaultDir = Path.Combine(Path.GetTempPath(), "los-vault-" + Guid.NewGuid().ToString("N"));
    private readonly DbContextOptions<LosDbContext> _options;
    private readonly ServiceProvider _provider;
    private readonly Fake _digio = new();
    private readonly MemoryFiles _files = new();

    public DigioPhase3Tests()
    {
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using (var db = Anonymous())
        {
            db.Database.EnsureCreated();
            db.Applications.Add(new Application { Id = "A1", CompanyId = 1 });
            db.SaveChanges();
            db.Disbursements.Add(new Disbursement { ApplicationId = "A1", AgreementFilePath = Key });
            db.Parties.Add(new Party { ApplicationId = "A1", PartyType = "Applicant", FullName = "TEST APPLICANT", Mobile = "9000000001", Email = "a@example.com", DateOfBirth = new DateOnly(1990, 5, 1), Gender = "Male", Aadhaar = "123412341234" });
            db.Parties.Add(new Party { ApplicationId = "A1", PartyType = "CoApplicant", FullName = "TEST CO", Mobile = "9000000002" });
            db.DigioSettings.Add(new DigioSettings { CompanyId = 1, Esign = true, Enach = true, EnachConfigIdSandbox = "ENACH-SBX", EnachConfigIdProduction = "ENACH-PRD" });
            db.EnachMandates.Add(new EnachMandate { ApplicationId = "A1", AccountNumber = "123456789012", Ifsc = "HDFC0001234", ConfirmationAccepted = true });
            db.SaveChanges();
        }

        _files.Saved[Key] = PdfV1;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_options);
        services.AddSingleton<IDbContextFactory<LosDbContext>>(new Factory(this));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IWebHostEnvironment>(new FakeEnv(_vaultDir));
        services.AddSingleton<PasswordVault>();
        services.AddSingleton<IHttpClientFactory>(_digio);
        services.AddSingleton<IFileStore>(_files);
        services.AddSingleton(Options.Create(new ApiBillingOptions()));
        services.AddSingleton<DigioWebhookHealth>();
        services.AddSingleton<DigioBackgroundTasks>();
        services.AddSingleton<DigioStatusNotifier>();
        services.AddScoped<ApiBillingService>();
        services.AddScoped<DigioClient>();
        services.AddScoped<DigioEsignService>();
        services.AddScoped<DigioNachService>();
        services.AddScoped<DigioCollect>();
        services.AddScoped<IBounceHandler, BounceService>();
        services.AddScoped<DigioWebhookService>();
        _provider = services.BuildServiceProvider();
        _provider.GetRequiredService<DigioClient>().SaveCredentials(1, new DigioCredentials("cid", "csecret", "wh", false));

        _digio.On("POST", "/v2/client/document/uploadpdf", _ => Doc("DID1", "requested", "requested", "sequenced"));
        _digio.On("POST", "/v3/client/mandate/create_form", _ => """{"id":"ENA1","state":"partial","authentication_url":"https://digio.example/auth"}""");
        _digio.On("GET", "/v3/client/mandate/banks", _ => """[{"id":"HDFC","name":"HDFC Bank","ifscPrefix":"HDFC","apiMandateNetBanking":true,"apiMandateDebitCard":true,"apiMandateAadhaarCard":false,"esignMandate":true,"physicalMandate":true}]""");
    }

    // ---- e-Sign ----

    [Fact]
    public async Task TwoSignerFlow_IsSequential_CallbackIsOurRequestId_AndCompletesOnce()
    {
        using var scope = _provider.CreateScope();
        var esign = scope.ServiceProvider.GetRequiredService<DigioEsignService>();

        var sent = await esign.SendAsync(1, "A1", "u1", "SMS");

        Assert.True(sent.Ok, sent.Error);
        var call = Assert.Single(_digio.Calls, c => c.Path == "/v2/client/document/uploadpdf");
        using var body = JsonDocument.Parse(call.Body);
        var root = body.RootElement;
        Assert.True(root.GetProperty("sequential").GetBoolean());
        Assert.Equal(["9000000001", "9000000002"], root.GetProperty("signers").EnumerateArray().Select(s => s.GetProperty("identifier").GetString()).ToArray());
        Assert.Equal([1, 2], root.GetProperty("signers").EnumerateArray().Select(s => s.GetProperty("index").GetInt32()).ToArray());
        Assert.Equal("SMS", root.GetProperty("customer_notification_mode").GetString());
        Assert.False(root.TryGetProperty("signature_verification", out _)); // no KYC evidence yet: no rules
        var d = Disbursement();
        Assert.Equal("Sent", d.AgreementEsignStatus);
        Assert.Equal(root.GetProperty("callback").GetString(), d.EsignRequestId);
        Assert.Equal(DigioEsignService.Sha256(PdfV1), d.AgreementFingerprint);
        Assert.Equal(["requested", "sequenced"], Signers().Select(s => s.Status).ToArray());

        // First signer signs: still Sent, 1 of 2.
        _digio.On("GET", "/v2/client/document/DID1", _ => Doc("DID1", "requested", "signed", "requested"));
        await Webhook(scope, "doc.signed", "DID1", "requested");
        await _provider.GetRequiredService<DigioBackgroundTasks>().WhenIdleAsync();
        Assert.Equal("Sent", Disbursement().AgreementEsignStatus);
        Assert.Equal(["signed", "requested"], Signers().Select(s => s.Status).ToArray());

        // Second signs: Signed once, evidence written.
        _digio.On("GET", "/v2/client/document/DID1", _ => Doc("DID1", "completed", "signed", "signed"));
        _digio.Download = PdfV1;
        await Webhook(scope, "doc.signed", "DID1", "completed");
        await Webhook(scope, "doc.signed", "DID1", "completed");
        await _provider.GetRequiredService<DigioBackgroundTasks>().WhenIdleAsync();
        Assert.Equal("Signed", Disbursement().AgreementEsignStatus);
        using var db = Anonymous();
        Assert.Single(db.VerificationEvidence.IgnoreQueryFilters().Where(e => e.Subject == "AGREEMENT_SIGNED"));
    }

    [Fact]
    public async Task Send_IsIdempotent_ForTheSameAgreement()
    {
        using var scope = _provider.CreateScope();
        var esign = scope.ServiceProvider.GetRequiredService<DigioEsignService>();

        await esign.SendAsync(1, "A1", null, "SMS");
        var again = await esign.SendAsync(1, "A1", null, "SMS");

        Assert.True(again.Ok);
        Assert.True(again.AlreadySent);
        Assert.Single(_digio.Calls, c => c.Path == "/v2/client/document/uploadpdf");
    }

    [Fact]
    public async Task ChangedAgreement_CancelsTheOldRequest_ThenSendsTheNewOne()
    {
        using var scope = _provider.CreateScope();
        var esign = scope.ServiceProvider.GetRequiredService<DigioEsignService>();
        await esign.SendAsync(1, "A1", null, "SMS");

        _files.Saved[Key] = PdfV2; // the agreement was regenerated
        _digio.On("POST", "/v2/client/document/DID1/cancel", _ => """{"id":"DID1","agreement_status":"failed"}""");
        _digio.On("POST", "/v2/client/document/uploadpdf", _ => Doc("DID2", "requested", "requested", "sequenced"));
        var resent = await esign.SendAsync(1, "A1", null, "SMS");

        Assert.True(resent.Ok, resent.Error);
        Assert.Equal(["/v2/client/document/uploadpdf", "/v2/client/document/DID1/cancel", "/v2/client/document/uploadpdf"],
            _digio.Calls.Where(c => c.Method == "POST").Select(c => c.Path).Where(p => p.Contains("document")).ToArray());
        var d = Disbursement();
        Assert.Equal("DID2", d.EsignDocumentId);
        Assert.Equal(DigioEsignService.Sha256(PdfV2), d.AgreementFingerprint);
        Assert.All(Signers().Where(s => s.DocumentId == "DID1"), s => Assert.Equal("cancelled", s.Status));
    }

    [Fact]
    public async Task SignatureVerification_IsSentOnlyForPartiesWithKycEvidence()
    {
        using (var db = Anonymous())
        {
            var applicant = db.Parties.IgnoreQueryFilters().Single(p => p.PartyType == "Applicant");
            db.VerificationEvidence.Add(new VerificationEvidence { CompanyId = 1, ApplicationId = "A1", PartyId = applicant.Id, Subject = "AADHAAR", Verdict = "Verified", CreatedAt = DateTime.UtcNow });
            db.SaveChanges();
        }

        using var scope = _provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DigioEsignService>().SendAsync(1, "A1", null, "SMS");

        using var body = JsonDocument.Parse(_digio.Calls.Single(c => c.Path == "/v2/client/document/uploadpdf").Body);
        var verification = body.RootElement.GetProperty("signature_verification");
        Assert.True(verification.TryGetProperty("9000000001", out var rules));
        Assert.False(verification.TryGetProperty("9000000002", out _));
        var fields = rules.GetProperty("rules")[0].GetProperty("conditions").EnumerateArray().Select(c => c.GetProperty("field").GetString()).ToArray();
        Assert.Equal(["NAME", "YOB", "AADHAAR", "GENDER"], fields);
        Assert.Equal("1234", rules.GetProperty("rules")[0].GetProperty("conditions").EnumerateArray().Single(c => c.GetProperty("field").GetString() == "AADHAAR").GetProperty("value").GetString());
    }

    [Fact]
    public async Task ARejectedSigner_ShowsWhichRulesFailed_OnTheSignerRow()
    {
        using var scope = _provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DigioEsignService>().SendAsync(1, "A1", null, "SMS");

        _digio.On("GET", "/v2/client/document/DID1", _ => """
            {"id":"DID1","agreement_status":"requested","signing_parties":[
              {"identifier":"9000000001","status":"failed","signature_verification_response":{"ruleResults":[{"conditions":[{"field":"AADHAAR","result":"FAIL"},{"field":"YOB","result":"FAIL"},{"field":"NAME","result":"PASS"}]}]}},
              {"identifier":"9000000002","status":"sequenced"}]}
            """);
        await scope.ServiceProvider.GetRequiredService<DigioWebhookService>().ConfirmAsync(1, "DID1");

        var first = Signers().OrderBy(s => s.SignOrder).First();
        Assert.Equal("failed", first.Status);
        Assert.Equal("Signer checks failed: AADHAAR, YOB", first.FailureReason);
    }

    // ---- e-NACH state machine ----

    [Fact]
    public void Lifecycle_OnlyMovesForward_AndOutOfOrderEventsAreHarmless()
    {
        Assert.Equal("Registered", NachLifecycle.Next("Registered", "AuthSuccess"));      // late auth event after registration
        Assert.Equal("Registered", NachLifecycle.Next("Registered", "InBankProcessing"));
        Assert.Equal("Cancelled", NachLifecycle.Next("Registered", "Cancelled"));         // a registered mandate can be cancelled
        Assert.Equal("Rejected", NachLifecycle.Next("Rejected", "AuthSuccess"));          // a rejection is final
        Assert.Equal("InBankProcessing", NachLifecycle.Next("AuthSuccess", "InBankProcessing"));
        Assert.Equal("InBankProcessing", NachLifecycle.Next("InBankProcessing", "AuthSuccess")); // out of order: stays ahead
        Assert.Equal("AuthSuccess", NachLifecycle.Next("AuthFailed", "AuthSuccess"));     // the customer retried
        Assert.Equal("AuthSuccess", NachLifecycle.Next("AuthSuccess", "AuthFailed"));     // a late failure does not undo success
        Assert.Equal("Expired", NachLifecycle.Next("Requested", "Expired"));
        Assert.Equal("Registered", NachLifecycle.Map("auth_success", "REGISTER_SUCCESS"));
        Assert.Equal("Rejected", NachLifecycle.Map("auth_success", "nack_received"));
        Assert.Equal("InBankProcessing", NachLifecycle.Map("auth_success", "awaiting_ack"));
        Assert.Equal("Requested", NachLifecycle.Map("partial", null));
    }

    [Fact]
    public void Apply_WritesTheUmrnOnlyWhenRegistered_AndOnlyFromDigio()
    {
        var m = new EnachMandate { Umrn = "MANUAL" };
        using var auth = JsonDocument.Parse("""{"state":"auth_success","umrn":"UMRN-EARLY","bank_details":{"state":"awaiting_ack"}}""");
        NachLifecycle.Apply(m, auth.RootElement, DateTime.UtcNow);
        Assert.Equal("InBankProcessing", m.DigioState);
        Assert.Equal("MANUAL", m.Umrn); // not overwritten before Registered
        Assert.Equal("Pending", m.Status);

        using var reg = JsonDocument.Parse("""{"state":"auth_success","umrn":"UMRN0001","bank_details":{"state":"register_success"}}""");
        NachLifecycle.Apply(m, reg.RootElement, DateTime.UtcNow);
        Assert.Equal(("Registered", "UMRN0001", "Registered"), (m.DigioState, m.Umrn, m.Status));

        using var late = JsonDocument.Parse("""{"state":"auth_success","bank_details":{"state":"awaiting_ack"}}""");
        Assert.False(NachLifecycle.Apply(m, late.RootElement, DateTime.UtcNow) && m.DigioState != "Registered"); // replay: still Registered
        Assert.Equal("Registered", m.DigioState);

        var rejected = new EnachMandate();
        using var no = JsonDocument.Parse("""{"state":"auth_failed","npci_auth_reject_reason":"AP08 name mismatch","bank_details":{"state":"register_failed"}}""");
        NachLifecycle.Apply(rejected, no.RootElement, DateTime.UtcNow);
        Assert.Equal(("Rejected", "Rejected", "AP08 name mismatch"), (rejected.DigioState, rejected.Status, rejected.RejectReason));
    }

    // ---- e-NACH create / confirm ----

    [Fact]
    public async Task Create_IsIdempotent_AndUsesTheEnvironmentsConfigIdAndTheSettingsTerms()
    {
        using var scope = _provider.CreateScope();
        var nach = scope.ServiceProvider.GetRequiredService<DigioNachService>();
        var terms = new NachTerms(12345m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)));

        var first = await nach.CreateAsync(1, "A1", DigioNachService.EnachTab, "u1", "API", false, terms, "TEST APPLICANT", "9000000001", null);
        var second = await nach.CreateAsync(1, "A1", DigioNachService.EnachTab, "u1", "API", false, terms, "TEST APPLICANT", "9000000001", null);

        Assert.True(first.Ok, first.Error);
        Assert.True(second.Existing);
        var call = Assert.Single(_digio.Calls, c => c.Path == "/v3/client/mandate/create_form");
        using var body = JsonDocument.Parse(call.Body);
        var root = body.RootElement;
        var data = root.GetProperty("mandate_data");
        Assert.Equal("ENACH-SBX", root.GetProperty("corporate_config_id").GetString()); // sandbox keys -> sandbox id
        Assert.Equal("Adhoc", data.GetProperty("frequency").GetString());
        Assert.Equal(24700m, data.GetProperty("maximum_amount").GetDecimal()); // 12345 x 2 = 24690, rounded up to the next Rs 100
        Assert.Equal(12345m, data.GetProperty("collection_amount").GetDecimal());
        Assert.Equal("https://digio.example/auth", first.AuthUrl);
    }

    [Fact]
    public async Task Create_NeverSendsTheSandboxConfigIdToProduction()
    {
        _provider.GetRequiredService<DigioClient>().SaveCredentials(1, new DigioCredentials("cid", "csecret", "wh", true));
        using (var db = Anonymous())
        {
            db.DigioSettings.IgnoreQueryFilters().Single().EnachConfigIdProduction = null;
            db.SaveChanges();
        }

        using var scope = _provider.CreateScope();
        var terms = new NachTerms(5000m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)));
        var r = await scope.ServiceProvider.GetRequiredService<DigioNachService>().CreateAsync(1, "A1", DigioNachService.EnachTab, null, "API", false, terms, "N", "9000000001", null);

        Assert.False(r.Ok);
        Assert.Contains("config id", r.Error);
        Assert.DoesNotContain(_digio.Calls, c => c.Path == "/v3/client/mandate/create_form");
    }

    [Fact]
    public async Task Create_BlocksAFirstEmiDateThatIsNotTomorrowOrLater()
    {
        using var scope = _provider.CreateScope();
        var nach = scope.ServiceProvider.GetRequiredService<DigioNachService>();
        var past = new NachTerms(5000m, DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)));

        var r = await nach.CreateAsync(1, "A1", DigioNachService.EnachTab, null, "API", false, past, "N", "9000000001", null);

        Assert.False(r.Ok); // today is not "tomorrow or later"
        Assert.Contains("tomorrow", r.Error);
        Assert.DoesNotContain(_digio.Calls, c => c.Path == "/v3/client/mandate/create_form");
    }

    [Fact]
    public async Task MandateWebhook_IsConfirmedWithDigio_AndAReplayedOlderEventChangesNothing()
    {
        using var scope = _provider.CreateScope();
        var nach = scope.ServiceProvider.GetRequiredService<DigioNachService>();
        var terms = new NachTerms(5000m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)));
        await nach.CreateAsync(1, "A1", DigioNachService.EnachTab, null, "API", false, terms, "N", "9000000001", null);
        var webhook = scope.ServiceProvider.GetRequiredService<DigioWebhookService>();

        _digio.On("GET", "/v3/client/mandate/ENA1", _ => """{"id":"ENA1","state":"auth_success","umrn":"UMRN9","bank_details":{"state":"register_success"}}""");
        Assert.True(await webhook.HandleAsync(1, MandateEvent("APIMNDT.DESTACCEPT", "ENA1")));
        Assert.Equal(("Registered", "UMRN9", "Registered"), Mandate());

        _digio.On("GET", "/v3/client/mandate/ENA1", _ => """{"id":"ENA1","state":"auth_success","bank_details":{"state":"transfer_success"}}""");
        await webhook.HandleAsync(1, MandateEvent("APIMNDT.AUTHSUCCESS", "ENA1")); // arrives late
        Assert.Equal(("Registered", "UMRN9", "Registered"), Mandate());
    }

    // ---- the release gate ----

    [Fact]
    public async Task ReleaseGate_NeedsSignedAndRegistered_SandboxNeverCountsForProduction_AndABypassClears()
    {
        using (var db = Seeded())
        {
            Assert.False((await DigioReleaseGate.CheckAsync(db, "A1", false)).CanAdvance);

            db.Disbursements.IgnoreQueryFilters().Single().AgreementEsignStatus = "Signed";
            await db.SaveChangesAsync();
            var mandateOnly = await DigioReleaseGate.CheckAsync(db, "A1", false);
            Assert.False(mandateOnly.CanAdvance);
            Assert.Contains("mandate", mandateOnly.BlockedReason);
            Assert.DoesNotContain("agreement", mandateOnly.BlockedReason);

            var m = db.EnachMandates.IgnoreQueryFilters().Single();
            m.DigioState = NachLifecycle.Registered;
            m.DigioRequestId = "R1";
            db.DigioRequests.Add(new DigioRequest { Id = "R1", CompanyId = 1, ApplicationId = "A1", Environment = "Sandbox", Product = "MANDATE", RequestedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
            Assert.True((await DigioReleaseGate.CheckAsync(db, "A1", false)).CanAdvance);

            var prod = await DigioReleaseGate.CheckAsync(db, "A1", true); // sandbox mandate, production company
            Assert.False(prod.CanAdvance);
            Assert.Equal(AdminRequest.DigioReleaseBypass, prod.RequestType);

            await DigioReleaseGate.LogOverrideAsync(db, "A1", null, prod.BlockedReason!);
            Assert.True((await DigioReleaseGate.CheckAsync(db, "A1", true)).CanAdvance); // the logged approved request clears it
            Assert.Single(db.AdminRequests.IgnoreQueryFilters(), r => r.RequestType == AdminRequest.DigioReleaseBypass && r.Status == AdminRequest.Approved);
        }
    }

    [Fact]
    public async Task ReleaseGate_IsClear_WhenTheCompanyUsesNeitherProduct()
    {
        using var db = Seeded();
        var settings = db.DigioSettings.Single();
        settings.Esign = settings.Enach = false;
        await db.SaveChangesAsync();

        Assert.True((await DigioReleaseGate.CheckAsync(db, "A1", true)).CanAdvance);
    }

    [Fact]
    public async Task ABackgroundScope_WithNoCircuit_IsAnAnonymousTenant_NotACrash()
    {
        var tenant = new TenantContext(new Throwing(), new HttpContextAccessor(), new ActingCompanyStore());

        await tenant.EnsureLoadedAsync(); // used to throw "Do not call GetAuthenticationStateAsync outside of the DI scope"

        Assert.False(tenant.HasUser);
        Assert.False(tenant.IsSuperAdmin);
    }

    // ---- plumbing ----

    private sealed class Throwing : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            throw new InvalidOperationException("Do not call GetAuthenticationStateAsync outside of the DI scope for a Razor component.");
    }

    private static string Doc(string id, string status, params string[] signerStatuses) =>
        JsonSerializer.Serialize(new
        {
            id, agreement_status = status,
            signing_parties = signerStatuses.Select((s, i) => new { identifier = "900000000" + (i + 1), status = s, name = "S" + i }),
        });

    private static string MandateEvent(string evt, string id) =>
        "{\"id\":\"wh-" + Guid.NewGuid().ToString("N")[..6] + "\",\"event\":\"" + evt + "\",\"entities\":[\"api_mandate\"],\"payload\":{\"api_mandate\":{\"id\":\"" + id + "\"}}}";

    private async Task Webhook(IServiceScope scope, string evt, string docId, string status) =>
        await scope.ServiceProvider.GetRequiredService<DigioWebhookService>().HandleAsync(1,
            "{\"id\":\"wh1\",\"event\":\"" + evt + "\",\"entities\":[\"document\"],\"payload\":{\"document\":{\"id\":\"" + docId + "\",\"agreement_status\":\"" + status + "\"}}}");

    private Disbursement Disbursement()
    {
        using var db = Anonymous();
        return db.Disbursements.IgnoreQueryFilters().AsNoTracking().Single();
    }

    private List<EsignSigner> Signers()
    {
        using var db = Anonymous();
        return db.EsignSigners.IgnoreQueryFilters().AsNoTracking().OrderBy(s => s.Id).ToList();
    }

    private (string? State, string? Umrn, string Status) Mandate()
    {
        using var db = Anonymous();
        var m = db.EnachMandates.IgnoreQueryFilters().AsNoTracking().Single();
        return (m.DigioState, m.Umrn, m.Status);
    }

    /// <summary>A signed-in context that sees every company (what the page's user context does for its own).</summary>
    private LosDbContext Seeded() => new(_options, TenantContext.ForSeeding());

    private LosDbContext Anonymous()
    {
        var tenant = new TenantContext(new NoUser(), new HttpContextAccessor(), new ActingCompanyStore());
        tenant.EnsureLoadedAsync().GetAwaiter().GetResult();
        return new LosDbContext(_options, tenant);
    }

    public void Dispose()
    {
        _provider.GetRequiredService<DigioBackgroundTasks>().WhenIdleAsync().GetAwaiter().GetResult();
        _provider.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_file);
        try { Directory.Delete(_vaultDir, recursive: true); } catch { /* best effort */ }
    }

    private sealed class Factory(DigioPhase3Tests owner) : IDbContextFactory<LosDbContext>
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

    private sealed class MemoryFiles : IFileStore
    {
        public Dictionary<string, byte[]> Saved { get; } = [];

        public Task SaveAsync(string key, byte[] bytes)
        {
            lock (Saved) { Saved[FileKey.Normalize(key)] = bytes; }
            return Task.CompletedTask;
        }

        public Task<Stream?> OpenReadAsync(string key) =>
            Task.FromResult<Stream?>(Saved.TryGetValue(FileKey.Normalize(key), out var b) ? new MemoryStream(b) : null);
    }

    private sealed record Call(string Method, string Path, string Body);

    /// <summary>Digio, faked: programmable per (method, path), and every call recorded with its body.</summary>
    private sealed class Fake : IHttpClientFactory
    {
        private readonly Dictionary<(string, string), Func<string, string>> _routes = [];
        public List<Call> Calls { get; } = [];
        public byte[]? Download { get; set; }

        public void On(string method, string path, Func<string, string> reply) { lock (_routes) { _routes[(method, path)] = reply; } }

        public HttpClient CreateClient(string name) => new(new Handler(this));

        private sealed class Handler(Fake owner) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri!.AbsolutePath;
                var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
                lock (owner.Calls) { owner.Calls.Add(new(request.Method.Method, path, body)); }

                if (path.StartsWith("/v2/client/document/download"))
                {
                    return owner.Download is { } bytes ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) } : new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                Func<string, string>? reply;
                lock (owner._routes) { owner._routes.TryGetValue((request.Method.Method, path), out reply); }
                return reply is null
                    ? new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{}") }
                    : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(reply(body)) };
            }
        }
    }
}
