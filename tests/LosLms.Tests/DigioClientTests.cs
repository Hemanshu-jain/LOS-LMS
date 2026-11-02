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

/// <summary>The Digio client's contract: gates, the request log written before the call, idempotency, error mapping, retries. A fake handler stands in for Digio.</summary>
public sealed class DigioClientTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"digio-cl-{Guid.NewGuid():N}.db");
    private readonly string _vaultDir = Path.Combine(Path.GetTempPath(), "los-vault-" + Guid.NewGuid().ToString("N"));
    private readonly DbContextOptions<LosDbContext> _options;
    private readonly ServiceProvider _provider;
    private readonly FakeDigio _digio = new();

    public DigioClientTests()
    {
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using (var db = Anonymous())
        {
            db.Database.EnsureCreated();
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
        _provider = services.BuildServiceProvider();
    }

    private DigioClient Client(bool configured = true, bool production = false)
    {
        var client = _provider.CreateScope().ServiceProvider.GetRequiredService<DigioClient>();
        if (configured)
        {
            client.SaveCredentials(1, new DigioCredentials("cid", "csecret", "wh", production));
        }

        return client;
    }

    private static DigioCall Tracked(string? requestId = null) =>
        new(1, HttpMethod.Post, "/v3/client/kyc/pan/verify", new { x = 1 }, Product: "KYC_ID", Operation: "PAN_VERIFY", ApplicationId: "A1", RequestId: requestId, Free: "test call");

    [Fact]
    public async Task NoKeys_SendsNothing_AndWritesNoRow()
    {
        var r = await Client(configured: false).SendAsync(Tracked());

        Assert.Equal(DigioStatus.NotConfigured, r.Status);
        Assert.Equal(0, _digio.Calls);
        Assert.Empty(Rows());
    }

    [Fact]
    public async Task TrackedCall_WritesTheSendingRowBeforeTheCallLeaves_ThenCompletesIt()
    {
        string? statusDuringCall = null;
        _digio.OnSend = (_, _) =>
        {
            statusDuringCall = Rows().SingleOrDefault()?.Status;
            return (HttpStatusCode.OK, """{"id":"KID1","status":"success","secret_field":"PII"}""");
        };

        var r = await Client().SendAsync(Tracked());

        Assert.Equal("Sending", statusDuringCall);
        var row = Assert.Single(Rows());
        Assert.Equal(r.RequestId, row.Id);
        Assert.Equal(32, row.Id.Length);
        Assert.Equal(("Succeeded", "KID1", "success", "Sandbox"), (row.Status, row.DigioId, row.ProviderStatus, row.Environment));
        Assert.NotNull(row.CompletedAt);
        Assert.NotNull(row.ResultJson);
        Assert.DoesNotContain("PII", row.ResultJson); // stored sealed, never plain
        Assert.Contains("PII", _provider.GetRequiredService<PasswordVault>().Unseal(row.ResultJson!));
    }

    [Fact]
    public async Task RetryWithTheSameRequestId_ReusesTheRow()
    {
        var client = Client();
        _digio.OnSend = (_, _) => (HttpStatusCode.BadRequest, """{"code":"INVALID_PAN","message":"bad"}""");
        var first = await client.SendAsync(Tracked());
        _digio.OnSend = (_, _) => (HttpStatusCode.OK, """{"id":"KID2"}""");
        await client.SendAsync(Tracked(first.RequestId));

        var row = Assert.Single(Rows());
        Assert.Equal(("Succeeded", "KID2", null), (row.Status, row.DigioId, row.ErrorCode));
    }

    [Fact]
    public async Task Failure_IsMappedToPlainWords_NeverTheRawBody()
    {
        _digio.OnSend = (_, _) => (HttpStatusCode.Unauthorized, """{"code":"BAD_KEY","message":"internal secret details"}""");

        var r = await Client().SendAsync(Tracked());

        Assert.Equal(DigioStatus.Failed, r.Status);
        Assert.DoesNotContain("internal", r.Error);
        Assert.Contains("keys", r.Error);
        Assert.Equal("BAD_KEY", r.ErrorCode);
        Assert.Equal("Failed", Assert.Single(Rows()).Status);
    }

    [Fact]
    public async Task ProductionKeys_GoToTheProductionHost_AndAreRecordedAsProduction()
    {
        var r = await Client(production: true).SendAsync(Tracked());

        Assert.Equal("api.digio.in", _digio.LastHost);
        Assert.Equal("Production", r.Environment);
        Assert.Equal("Production", Assert.Single(Rows()).Environment);
    }

    [Fact]
    public async Task Get_IsRetriedOnceOnANetworkError_Post_IsNot()
    {
        _digio.OnSend = (_, _) => throw new HttpRequestException("down");
        var client = Client();

        var get = await client.SendAsync(new DigioCall(1, HttpMethod.Get, "/x", Free: "test call"));
        Assert.Equal(DigioStatus.Failed, get.Status);
        Assert.Equal(2, _digio.Calls);

        _digio.Calls = 0;
        await client.SendAsync(Tracked());
        Assert.Equal(1, _digio.Calls);
        Assert.Equal("Failed", Assert.Single(Rows()).Status);
    }

    [Fact]
    public async Task ACallWithNoBillingDeclaration_IsRefused_AndNothingIsSent()
    {
        DigioResult? result = null;
        try { result = await Client().SendAsync(new DigioCall(1, HttpMethod.Get, "/x")); }
        catch (InvalidOperationException) { /* a debug build throws instead, so it is found the day it is written */ }

        Assert.True(result is null || result.Status == DigioStatus.Failed);
        Assert.Equal(0, _digio.Calls);
    }

    [Fact]
    public async Task ACodeWithNoActivePrice_IsRefused_AndNothingIsSent()
    {
        var r = await Client().SendAsync(new DigioCall(1, HttpMethod.Post, "/client/v1/user/screen", new { }, ApiCode: DigioRates.AmlScreen)); // not in the quote, so not seeded

        Assert.Equal(DigioStatus.Failed, r.Status);
        Assert.Contains("Price not set for AML_SCREEN", r.Error);
        Assert.Equal(0, _digio.Calls);
        using var db = Anonymous();
        Assert.Empty(db.ApiUsageLogs.IgnoreQueryFilters());
    }

    [Fact]
    public async Task APricedCode_IsSent_AndMeteredOnSuccess()
    {
        var r = await Client().SendAsync(new DigioCall(1, HttpMethod.Post, "/client/v4/apis/verify/vehicle_rc", new { }, ApiCode: DigioRates.VehicleRc));

        Assert.Equal(DigioStatus.Ok, r.Status);
        using var db = Anonymous();
        var log = Assert.Single(db.ApiUsageLogs.IgnoreQueryFilters());
        Assert.Equal((DigioRates.VehicleRc, 2.00m, true), (log.ApiCode, log.UnitRate, log.IsSuccess));
    }

    [Fact]
    public async Task UntrackedCall_WritesNoRow()
    {
        var r = await Client().GetAsync(1, null, "/x", null, "test call");

        Assert.Equal(DigioStatus.Ok, r.Status);
        Assert.Null(r.RequestId);
        Assert.Empty(Rows());
    }

    private List<DigioRequest> Rows()
    {
        using var db = Anonymous();
        return db.DigioRequests.IgnoreQueryFilters().AsNoTracking().ToList();
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

    private sealed class Factory(DigioClientTests owner) : IDbContextFactory<LosDbContext>
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

    private sealed class FakeDigio : IHttpClientFactory
    {
        public Func<HttpMethod, string, (HttpStatusCode, string)> OnSend { get; set; } = (_, _) => (HttpStatusCode.OK, "{}");
        public int Calls;
        public string? LastHost;

        public HttpClient CreateClient(string name) => new(new Handler(this));

        private sealed class Handler(FakeDigio owner) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                owner.Calls++;
                owner.LastHost = request.RequestUri!.Host;
                var (code, body) = owner.OnSend(request.Method, request.RequestUri.PathAndQuery);
                return Task.FromResult(new HttpResponseMessage(code) { Content = new StringContent(body, Encoding.UTF8) });
            }
        }
    }
}
