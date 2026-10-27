using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
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
/// What a Digio webhook may do. The endpoint always answers 200; only a verified body is processed; an agreement becomes
/// Signed only when every party has signed, once, with one credit metered and the signed PDF kept. No live network: a fake
/// handler stands in for Digio. A temp-file SQLite database (not in-memory) so concurrent deliveries really use two connections.
/// </summary>
public sealed class DigioWebhookTests : IDisposable
{
    private const string Secret = "wh-secret";
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.4 signed agreement");

    private readonly string _file = Path.Combine(Path.GetTempPath(), $"digio-wh-{Guid.NewGuid():N}.db");
    private readonly string _vaultDir = Path.Combine(Path.GetTempPath(), "los-vault-" + Guid.NewGuid().ToString("N"));
    private readonly DbContextOptions<LosDbContext> _options;
    private readonly ServiceProvider _provider;
    private readonly FakeDigio _digio = new();
    private readonly MemoryFiles _files = new();

    public DigioWebhookTests()
    {
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using (var db = Anonymous())
        {
            db.Database.EnsureCreated();
            db.Companies.Add(new Company { Id = 2, Name = "Other" });
            db.Applications.Add(new Application { Id = "A1", CompanyId = 1 });
            db.SaveChanges();
            db.Disbursements.Add(new Disbursement { ApplicationId = "A1", AgreementEsignStatus = "Sent", EsignDocumentId = "DID123" });
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
        services.AddSingleton<IFileStore>(_files);
        services.AddSingleton(Options.Create(new ApiBillingOptions()));
        services.AddSingleton<DigioWebhookHealth>();
        services.AddScoped<ApiBillingService>();
        services.AddScoped<DigioClient>();
        services.AddScoped<DigioWebhookService>();
        _provider = services.BuildServiceProvider();

        _provider.GetRequiredService<DigioClient>().SaveCredentials(1, new DigioCredentials("cid", "csecret", Secret, false));
    }

    // ---- the transition ----

    [Fact]
    public async Task Completed_MarksSignedOnce_MetersOnce_AndKeepsTheSignedPdf()
    {
        using var scope = _provider.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<DigioWebhookService>();
        _digio.OnDownload = () => Pdf;

        await svc.HandleAsync(1, Payload("doc.signed", "DID123", "completed"));
        await svc.HandleAsync(1, Payload("doc.signed", "DID123", "completed")); // redelivery
        await svc.HandleAsync(1, Payload("doc.sign.failed", "DID123"));         // never un-signs

        var d = Disbursement();
        Assert.Equal("Signed", d.AgreementEsignStatus);
        Assert.Equal(1, UsageRows());
        Assert.Equal("App_Data/uploads/A1/postsanction/signed-DID123.pdf", d.SignedAgreementFilePath);
        Assert.Equal(Pdf, _files.Saved["App_Data/uploads/A1/postsanction/signed-DID123.pdf"]);
        Assert.Equal(1, _digio.Downloads); // not fetched again
    }

    [Fact]
    public async Task ConcurrentDeliveries_OnlyOneWins_SoTheCreditIsMeteredOnce()
    {
        _digio.OnDownload = () => Pdf;
        await Task.WhenAll(Enumerable.Range(0, 6).Select(async _ =>
        {
            using var scope = _provider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<DigioWebhookService>().HandleAsync(1, Payload("doc.signed", "DID123", "completed"));
        }));

        Assert.Equal("Signed", Disbursement().AgreementEsignStatus);
        Assert.Equal(1, UsageRows());
    }

    [Fact]
    public async Task APartialSignature_IsNotSigned()
    {
        using var scope = _provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DigioWebhookService>().HandleAsync(1, Payload("doc.signed", "DID123", "requested"));

        Assert.Equal("Sent", Disbursement().AgreementEsignStatus);
        Assert.Equal(0, UsageRows());
    }

    [Fact]
    public async Task NoStatusInThePayload_IsConfirmedWithDigio_InTheBackground()
    {
        _digio.OnGet = _ => """{"id":"DID123","agreement_status":"completed"}""";
        _digio.OnDownload = () => Pdf;
        using var scope = _provider.CreateScope();

        await scope.ServiceProvider.GetRequiredService<DigioWebhookService>().HandleAsync(1, Payload("doc.signed", "DID123", null));

        await Eventually(() => Disbursement().AgreementEsignStatus == "Signed");
        Assert.Equal(1, UsageRows());
    }

    [Fact]
    public async Task NoStatus_WhenDigioSaysStillRequested_StaysSent()
    {
        _digio.OnGet = _ => """{"id":"DID123","agreement_status":"requested"}""";
        using var scope = _provider.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<DigioWebhookService>();

        await svc.ConfirmAsync(1, "DID123");

        Assert.Equal("Sent", Disbursement().AgreementEsignStatus);
        Assert.Equal(1, _digio.Gets);
    }

    [Fact]
    public async Task ASignedAgreementWhoseDownloadFailed_CanBeFetchedLater()
    {
        using var scope = _provider.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<DigioWebhookService>();
        _digio.OnDownload = () => null; // Digio not ready

        await svc.HandleAsync(1, Payload("doc.signed", "DID123", "completed"));
        Assert.Equal("Signed", Disbursement().AgreementEsignStatus);
        Assert.Null(Disbursement().SignedAgreementFilePath);

        _digio.OnDownload = () => Pdf;
        Assert.True(await svc.FetchSignedCopyAsync(1, "A1"));
        Assert.NotNull(Disbursement().SignedAgreementFilePath);
        Assert.False(await svc.FetchSignedCopyAsync(2, "A1")); // another company's id finds nothing
    }

    [Fact]
    public async Task FailedAndRejected_KeepTheReason_WithoutPersonalData()
    {
        using var scope = _provider.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<DigioWebhookService>();

        await svc.HandleAsync(1, """{"id":"w1","event":"doc.sign.failed","payload":{"document":{"id":"DID123","error_code":"ESP-123","error_message":"UIDAI Server Down","signer_identifier":"someone@x.in"}}}""");
        Assert.Equal(("Failed", "ESP-123: UIDAI Server Down"), (Disbursement().AgreementEsignStatus, Disbursement().EsignFailureReason));

        await svc.HandleAsync(1, """
            {"id":"w2","event":"doc.sign.rejected","payload":{"document":{"id":"DID123","error_code":null,"error_message":null,
             "signer_identifier":"someone@x.in","validation_results":{"overallResult":"FAIL","ruleResults":[
              {"rule_id":1,"result":"FAIL","conditions":[{"field":"GENDER","result":"PASS"},{"field":"AADHAAR","result":"FAIL","actual_value":"7142"}]},
              {"rule_id":2,"result":"PASS","conditions":[{"field":"YOB","result":"FAIL"},{"field":"NAME","result":"PASS"}]}]}}}}
            """);
        var d = Disbursement();
        Assert.Equal("Rejected", d.AgreementEsignStatus);
        Assert.Equal("Signer checks failed: AADHAAR, YOB", d.EsignFailureReason);
        Assert.DoesNotContain("7142", d.EsignFailureReason);
    }

    [Fact]
    public async Task AnotherCompany_UnknownDocument_UnknownEvent_ChangeNothing()
    {
        using var scope = _provider.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<DigioWebhookService>();

        await svc.HandleAsync(2, Payload("doc.signed", "DID123", "completed"));
        await svc.HandleAsync(1, Payload("doc.signed", "NOPE", "completed"));
        await svc.HandleAsync(1, """{"event":"kyc.something"}""");
        await svc.HandleAsync(1, """{"event":"doc.signed","payload":{"other":1}}""");

        Assert.Equal("Sent", Disbursement().AgreementEsignStatus);
        Assert.Equal(0, UsageRows());
    }

    // ---- the endpoint: always 200, only a verified body is processed ----

    [Fact]
    public async Task Endpoint_AlwaysAnswers200_AndOnlyAVerifiedBodyIsProcessed()
    {
        _digio.OnDownload = () => Pdf;
        var good = Payload("doc.signed", "DID123", "completed");
        var health = _provider.GetRequiredService<DigioWebhookHealth>();

        Assert.Equal(200, await Post("abc", good, Sign(good)));                          // unreadable company id
        Assert.Equal(200, await Post("1", good, null));                                  // no signature
        Assert.Equal(200, await Post("1", good, Sign("something else")));                // wrong signature
        Assert.Contains("Signature", health.For(1).Problem);
        Assert.Equal(200, await Post("77", good, Sign(good)));                           // company with no Digio keys
        Assert.Equal(200, await Post("1", new string('x', DigioWebhookEndpoint.MaxBodyBytes + 5), Sign("x"))); // oversize, no Content-Length
        Assert.Contains("too large", health.For(1).Problem);
        Assert.Equal("Sent", Disbursement().AgreementEsignStatus);                       // none of the above did anything

        const string broken = "{not json";
        Assert.Equal(200, await Post("1", broken, Sign(broken)));                        // verified but unparseable
        Assert.Contains("could not be processed", health.For(1).Problem);
        Assert.Equal("Sent", Disbursement().AgreementEsignStatus);

        Assert.Equal(200, await Post("1", good, Sign(good)));                            // the real one
        Assert.Equal("Signed", Disbursement().AgreementEsignStatus);
        Assert.NotNull(health.For(1).Received);
    }

    // ---- plumbing ----

    private async Task<int> Post(string company, string body, string? signature)
    {
        var http = new DefaultHttpContext { RequestServices = _provider.CreateScope().ServiceProvider };
        http.Request.Method = "POST";
        http.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body)); // no Content-Length: like a chunked request
        http.Response.Body = new MemoryStream();
        if (signature is not null) { http.Request.Headers["X-Digio-Checksum"] = signature; }

        await DigioWebhookEndpoint.HandleAsync(http, company);
        return http.Response.StatusCode;
    }

    private static string Sign(string body) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

    private static string Payload(string evt, string docId, string? agreementStatus = "completed")
    {
        var status = agreementStatus is null ? "" : ",\"agreement_status\":\"" + agreementStatus + "\"";
        return "{\"id\":\"wh1\",\"event\":\"" + evt + "\",\"entities\":[\"document\"],\"payload\":{\"document\":{\"id\":\"" + docId + "\"" + status + "}}}";
    }

    private Disbursement Disbursement()
    {
        using var db = Anonymous();
        return db.Disbursements.IgnoreQueryFilters().AsNoTracking().Single();
    }

    private int UsageRows()
    {
        using var db = Anonymous();
        return db.ApiUsageLogs.IgnoreQueryFilters().Count(l => l.ApiCode == DigioRates.ESignAadhaar && l.IsSuccess);
    }

    private static async Task Eventually(Func<bool> condition)
    {
        for (var i = 0; i < 80 && !condition(); i++) { await Task.Delay(50); }
        Assert.True(condition(), "the background confirmation did not complete in time");
    }

    /// <summary>A webhook has no user, exactly how the real endpoint runs.</summary>
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

    private sealed class Factory(DigioWebhookTests owner) : IDbContextFactory<LosDbContext>
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

    /// <summary>Digio, faked: JSON GETs, the binary download, and the factory that hands out a client wired to them.</summary>
    private sealed class FakeDigio : IHttpClientFactory
    {
        public Func<string, string> OnGet { get; set; } = _ => "{}";
        public Func<byte[]?> OnDownload { get; set; } = () => null;
        public int Gets;
        public int Downloads;

        public HttpClient CreateClient(string name) => new(new Handler(this));

        private sealed class Handler(FakeDigio owner) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri!.PathAndQuery;
                if (path.StartsWith("/v2/client/document/download"))
                {
                    Interlocked.Increment(ref owner.Downloads);
                    return Task.FromResult(owner.OnDownload() is { } bytes
                        ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) }
                        : new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("""{"message":"not ready"}""") });
                }

                Interlocked.Increment(ref owner.Gets);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(owner.OnGet(path)) });
            }
        }
    }
}
