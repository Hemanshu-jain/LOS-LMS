using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LosLms.Tests;

/// <summary>
/// The whole partner story in one test, over real HTTP with the REAL PartnerBearer scheme and policies
/// (AddPartnerAuth), the real /oauth endpoints and the real /api/v1 endpoints: register a desktop client,
/// device flow, Admin approves, push a loan, read it back, read disbursals, refresh, disconnect, 401.
/// Only the database (SQLite) and the pipeline order (copied from Program.cs) are stand-ins.
/// </summary>
public sealed class PartnerE2eTests : IAsyncLifetime
{
    private const string Admin = "admin-1";
    private const string Both = "loans:write loans:read";

    private SqliteConnection _connection = null!;
    private DbContextOptions<LosDbContext> _options = null!;
    private WebApplication _app = null!;
    private HttpClient _http = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite(_connection).Options;
        using (var db = NewContext())
        {
            db.Database.EnsureCreated();
            db.Roles.Add(new IdentityRole { Id = "r-admin", Name = TenantContext.AdminRole, NormalizedName = "ADMIN" });
            db.Users.Add(new ApplicationUser
            {
                Id = Admin, UserName = Admin, NormalizedUserName = "ADMIN-1", Email = "admin@x.test",
                DisplayName = "Company Admin", CompanyId = LosDbContext.SeedCompanyId, IsActive = true,
            });
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = Admin, RoleId = "r-admin" });
            db.SaveChanges();
        }

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<IDbContextFactory<LosDbContext>>(new TestFactory(this));
        builder.Services.AddScoped<PartnerOAuthService>();
        builder.Services.AddPartnerAuth(); // the real scheme + loans:read / loans:write policies

        _app = builder.Build();
        _app.UsePartnerOAuth();   // same order as Program.cs: /oauth first, then the /api/v1 JSON wrapper, then auth
        _app.UseJsonErrors();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapPartnerLoanApi();
        await _app.StartAsync();

        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        _http = new HttpClient { BaseAddress = new Uri(address) };
    }

    public async Task DisposeAsync()
    {
        _http.Dispose();
        await _app.DisposeAsync();
        _connection.Dispose();
    }

    [Fact]
    public async Task Connect_Push_Read_Refresh_Disconnect_ThenEveryRefusalIsInTheErrorShape()
    {
        var clientId = await WithService(s => s.CreateClientAsync("Paavan Software", Admin, DateTime.UtcNow));

        // 1. Device flow: start, still pending, Admin allows, tokens.
        var tokens = await ConnectAsync(clientId, Both);
        Assert.StartsWith("lct_", tokens.GetProperty("access_token").GetString());
        var access = tokens.GetProperty("access_token").GetString()!;
        var refresh = tokens.GetProperty("refresh_token").GetString()!;
        Assert.Equal(Both, tokens.GetProperty("scope").GetString());

        // 2. Push one loan with the Bearer token, then read it back.
        var push = await Api(HttpMethod.Post, "/api/v1/loans", access, JsonContent.Create(new
        {
            loans = new[] { new { accountNumber = "E2E-1", customerName = "Asha Patil", outstandingAmount = 1234.5m, status = "Live" } },
        }));
        Assert.Equal(HttpStatusCode.OK, push.StatusCode);
        Assert.Equal(1, (await Json(push)).GetProperty("created").GetInt32());

        var one = await Json(await Api(HttpMethod.Get, "/api/v1/loans/E2E-1", access));
        Assert.True(one.GetProperty("exists").GetBoolean());
        Assert.Equal("Asha Patil", one.GetProperty("loan").GetProperty("customerName").GetString());

        // 3. Disbursals: the company's own disbursed application, via the same token.
        SeedDisbursed("LN-E2E-1", LosDbContext.SeedCompanyId);
        var disb = await Json(await Api(HttpMethod.Get, "/api/v1/disbursals?since=2026-09-01", access));
        Assert.Equal(1, disb.GetProperty("total").GetInt32());
        Assert.Equal("LN-E2E-1", disb.GetProperty("items")[0].GetProperty("applicationId").GetString());

        // 4. Refresh: a NEW access token that works.
        var refreshed = await Json(await Form("/oauth/token", ("grant_type", "refresh_token"), ("refresh_token", refresh), ("client_id", clientId)));
        var access2 = refreshed.GetProperty("access_token").GetString()!;
        Assert.NotEqual(access, access2);
        var again = await Api(HttpMethod.Get, "/api/v1/loans/E2E-1", access2);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);

        // 5. Disconnect (what the company Admin's Connected apps button does): the next call is a 401.
        var connections = await WithService(s => s.ListConnectionsAsync(LosDbContext.SeedCompanyId));
        var connection = Assert.Single(connections);
        Assert.Equal("Paavan Software", connection.AppName);
        Assert.True(await WithService(s => s.DisconnectAsync(connection.Id, LosDbContext.SeedCompanyId, DateTime.UtcNow)));

        await AssertErrorShape(await Api(HttpMethod.Get, "/api/v1/loans/E2E-1", access2), HttpStatusCode.Unauthorized, "unauthorized");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await Form("/oauth/token", ("grant_type", "refresh_token"), ("refresh_token", refreshed.GetProperty("refresh_token").GetString()!), ("client_id", clientId))).StatusCode);

        // 6. Other refusals on /api/v1 go through the same wrapper: no header, junk token, wrong scope.
        await AssertErrorShape(await Api(HttpMethod.Get, "/api/v1/loans", null), HttpStatusCode.Unauthorized, "unauthorized");
        await AssertErrorShape(await Api(HttpMethod.Get, "/api/v1/loans", "lct_not-a-real-token"), HttpStatusCode.Unauthorized, "unauthorized");

        var readOnly = (await ConnectAsync(clientId, "loans:read")).GetProperty("access_token").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await Api(HttpMethod.Get, "/api/v1/loans", readOnly)).StatusCode);
        await AssertErrorShape(
            await Api(HttpMethod.Post, "/api/v1/loans", readOnly, JsonContent.Create(new { loans = new[] { new { accountNumber = "X", customerName = "Y" } } })),
            HttpStatusCode.Forbidden, "insufficient_scope");
    }

    // ---- helpers ----------------------------------------------------------------------------------

    /// <summary>The device flow end to end over HTTP; the Admin's Allow is A's service call (what /link does).</summary>
    private async Task<JsonElement> ConnectAsync(string clientId, string scope)
    {
        var start = await Form("/oauth/device_authorization", ("client_id", clientId), ("scope", scope));
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var s = await Json(start);
        var device = s.GetProperty("device_code").GetString()!;
        Assert.EndsWith("/link", s.GetProperty("verification_uri").GetString());

        var pending = await Form("/oauth/token", ("grant_type", PartnerOAuthService.DeviceGrant), ("device_code", device), ("client_id", clientId));
        Assert.Equal(HttpStatusCode.BadRequest, pending.StatusCode);
        Assert.Equal("authorization_pending", (await Json(pending)).GetProperty("error").GetString());

        Assert.Equal(DecideResult.Done, await WithService(svc => svc.DecideAsync(s.GetProperty("user_code").GetString(), true, Admin, DateTime.UtcNow)));

        var token = await Form("/oauth/token", ("grant_type", PartnerOAuthService.DeviceGrant), ("device_code", device), ("client_id", clientId));
        Assert.Equal(HttpStatusCode.OK, token.StatusCode);
        return await Json(token);
    }

    private static async Task AssertErrorShape(HttpResponseMessage r, HttpStatusCode status, string error)
    {
        Assert.Equal(status, r.StatusCode);
        Assert.Contains("json", r.Content.Headers.ContentType?.MediaType);
        var body = await Json(r);
        Assert.Equal(error, body.GetProperty("error").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("message").GetString()));
        Assert.Equal(JsonValueKind.Array, body.GetProperty("details").ValueKind);
    }

    private Task<HttpResponseMessage> Form(string path, params (string Key, string Value)[] fields) =>
        _http.PostAsync(path, new FormUrlEncodedContent(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value))));

    private Task<HttpResponseMessage> Api(HttpMethod method, string path, string? bearer, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = content };
        if (bearer is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        return _http.SendAsync(request);
    }

    private static async Task<JsonElement> Json(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;

    private async Task<T> WithService<T>(Func<PartnerOAuthService, Task<T>> call)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        return await call(scope.ServiceProvider.GetRequiredService<PartnerOAuthService>());
    }

    private void SeedDisbursed(string id, int company)
    {
        using var db = NewContext();
        db.Applications.Add(new Application
        {
            Id = id, CompanyId = company, CustomerName = "E2E Customer", LoanAmount = 100000m, Roi = 12m, Tenure = 24,
            Disbursed = true, DisbursalDate = new DateOnly(2026, 9, 15), FirstEmiDate = new DateOnly(2026, 10, 10), EmiDayOfMonth = 10,
        });
        db.SaveChanges();
    }

    private LosDbContext NewContext()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "super"), new Claim(ClaimTypes.Role, TenantContext.SuperAdminRole) }, "TestAuth"));
        var tenant = new TenantContext(new FakeAuth(principal), new HttpContextAccessor(), new ActingCompanyStore());
        tenant.EnsureLoadedAsync().GetAwaiter().GetResult();
        return new LosDbContext(_options, tenant);
    }

    private sealed class TestFactory(PartnerE2eTests owner) : IDbContextFactory<LosDbContext>
    {
        public LosDbContext CreateDbContext() => owner.NewContext();
    }

    private sealed class FakeAuth(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
    }
}
