using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace LosLms.Tests;

/// <summary>
/// The partner data API end to end over real HTTP (Kestrel on a free port, SQLite in memory). Auth is a
/// TEST-ONLY handler that sets the claims the OAuth side will deliver (company_id, partner_name, scope);
/// the endpoints and policies under test are the real ones.
/// </summary>
public sealed class PartnerLoanApiTests : IAsyncLifetime
{
    private const int A = LosDbContext.SeedCompanyId;
    private const int B = 2;

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
            db.Companies.Add(new Company { Id = B, Name = "Company B" });
            db.SaveChanges();
        }

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<IDbContextFactory<LosDbContext>>(new TestFactory(this));
        builder.Services.AddAuthentication(TestAuth.Scheme).AddScheme<AuthenticationSchemeOptions, TestAuth>(TestAuth.Scheme, _ => { });
        builder.Services.AddAuthorization(o =>
        {
            foreach (var scope in new[] { PartnerLoanApi.ReadPolicy, PartnerLoanApi.WritePolicy })
            {
                o.AddPolicy(scope, p => p.AddAuthenticationSchemes(TestAuth.Scheme).RequireClaim("scope", scope));
            }
        });

        _app = builder.Build();
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

    // ---- upsert -----------------------------------------------------------------------------------

    [Fact]
    public async Task Upsert_ReportsCreatedUpdatedUnchanged()
    {
        var first = await Post(A, new[] { Loan("L-1", "Asha"), Loan("L-2", "Bhim") });
        var r1 = await Body(first);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal((2, 0, 0), (Count(r1, "created"), Count(r1, "updated"), Count(r1, "unchanged")));

        var again = await Body(await Post(A, new[] { Loan("L-1", "Asha"), Loan("L-2", "Bhim") }));
        Assert.Equal((0, 0, 2), (Count(again, "created"), Count(again, "updated"), Count(again, "unchanged")));

        var changed = await Body(await Post(A, new[] { Loan("L-1", "Asha", outstanding: 500m), Loan("L-2", "Bhim"), Loan("L-3", "Chitra") }));
        Assert.Equal((1, 1, 1), (Count(changed, "created"), Count(changed, "updated"), Count(changed, "unchanged")));
        Assert.Equal("updated", changed.GetProperty("results")[0].GetProperty("result").GetString());

        var one = await Body(await Get(A, "/api/v1/loans/L-1"));
        Assert.Equal(500m, one.GetProperty("loan").GetProperty("outstandingAmount").GetDecimal());
    }

    [Fact]
    public async Task Upsert_422_ListsEveryBadRow_AndSavesNothing()
    {
        var reply = await Post(A, new object[]
        {
            Loan("OK-1", "Fine"),
            new { accountNumber = "BAD-1", customerName = "", pan = "nope" },
            new { accountNumber = "OK-1", customerName = "Dup", disbursedOn = "30/09/2026", sanctionedAmount = -5 },
        });
        var body = await Body(reply);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, reply.StatusCode);
        Assert.Equal("validation_failed", body.GetProperty("error").GetString());
        var details = body.GetProperty("details").EnumerateArray()
            .Select(d => (d.GetProperty("row").GetInt32(), d.GetProperty("field").GetString())).ToList();
        Assert.Contains((2, "customerName"), details);
        Assert.Contains((2, "pan"), details);
        Assert.Contains((3, "accountNumber"), details);
        Assert.Contains((3, "disbursedOn"), details);
        Assert.Contains((3, "sanctionedAmount"), details);
        Assert.DoesNotContain(details, d => d.Item1 == 1);

        var list = await Body(await Get(A, "/api/v1/loans"));
        Assert.Equal(0, list.GetProperty("total").GetInt32()); // all-or-nothing
    }

    [Fact]
    public async Task Upsert_RejectsOversizedBatch()
    {
        var reply = await Post(A, Enumerable.Range(1, PartnerLoanApi.MaxBatch + 1).Select(i => Loan($"L-{i}", "X")).ToArray());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reply.StatusCode);
        Assert.Equal("batch_too_large", (await Body(reply)).GetProperty("error").GetString());
    }

    // ---- read / delete / tenant isolation ---------------------------------------------------------

    [Fact]
    public async Task OneCompanyCannotSeeOrDeleteAnothers()
    {
        await Post(A, new[] { Loan("SHARED-1", "Owned by A"), Loan("A-ONLY", "A") });

        Assert.Equal(HttpStatusCode.NotFound, (await Get(B, "/api/v1/loans/A-ONLY")).StatusCode);
        Assert.Equal(0, (await Body(await Get(B, "/api/v1/loans"))).GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await Delete(B, "A-ONLY")).StatusCode);

        // B pushing the same account number makes its OWN row; A's is untouched.
        var b = await Body(await Post(B, new[] { Loan("SHARED-1", "Owned by B") }));
        Assert.Equal(1, Count(b, "created"));
        var aRow = await Body(await Get(A, "/api/v1/loans/SHARED-1"));
        Assert.Equal("Owned by A", aRow.GetProperty("loan").GetProperty("customerName").GetString());

        Assert.Equal(2, (await Body(await Get(A, "/api/v1/loans"))).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Delete_ReturnsDeletedShape_AndPagingWorks()
    {
        await Post(A, Enumerable.Range(1, 5).Select(i => Loan($"P-{i}", "N")).ToArray());

        var page2 = await Body(await Get(A, "/api/v1/loans?page=2&pageSize=2"));
        Assert.Equal((5, 2, 2), (page2.GetProperty("total").GetInt32(), page2.GetProperty("page").GetInt32(), page2.GetProperty("items").GetArrayLength()));

        var del = await Delete(A, "P-1");
        var d = await Body(del);
        Assert.True(d.GetProperty("deleted").GetBoolean());
        Assert.Equal("P-1", d.GetProperty("accountNumber").GetString());
        Assert.True(d.GetProperty("id").GetInt32() > 0);
        Assert.Equal(HttpStatusCode.NotFound, (await Get(A, "/api/v1/loans/P-1")).StatusCode);
    }

    // ---- auth -------------------------------------------------------------------------------------

    [Fact]
    public async Task MissingScope_Is403_AndNoToken_Is401_BothInTheErrorShape()
    {
        var readOnly = await Send(HttpMethod.Post, "/api/v1/loans", A, "loans:read", JsonContent.Create(new { loans = new[] { Loan("X", "Y") } }));
        Assert.Equal(HttpStatusCode.Forbidden, readOnly.StatusCode);
        var e = await Body(readOnly);
        Assert.Equal("insufficient_scope", e.GetProperty("error").GetString());
        Assert.True(e.TryGetProperty("message", out _) && e.TryGetProperty("details", out _));

        var anon = await _http.GetAsync("/api/v1/loans");
        Assert.Equal(HttpStatusCode.Unauthorized, anon.StatusCode);
        Assert.Equal("unauthorized", (await Body(anon)).GetProperty("error").GetString());
    }

    // ---- disbursals -------------------------------------------------------------------------------

    [Fact]
    public async Task Disbursals_SinceFilter_Paging_AndTenantScope()
    {
        Seed(A, "LN-1", new DateOnly(2026, 9, 1), disbursed: true);
        Seed(A, "LN-2", new DateOnly(2026, 9, 10), disbursed: true);
        Seed(A, "LN-3", new DateOnly(2026, 9, 20), disbursed: true);
        Seed(A, "LN-4", null, disbursed: false);                       // not disbursed: never listed
        Seed(B, "LN-5", new DateOnly(2026, 9, 15), disbursed: true);   // other company: never listed

        var all = await Body(await Get(A, "/api/v1/disbursals"));
        Assert.Equal(3, all.GetProperty("total").GetInt32());

        var since = await Body(await Get(A, "/api/v1/disbursals?since=2026-09-10"));
        Assert.Equal(new[] { "LN-2", "LN-3" }, since.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("applicationId").GetString()));

        var page = await Body(await Get(A, "/api/v1/disbursals?pageSize=2&page=2"));
        var only = Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.Equal("LN-3", only.GetProperty("applicationId").GetString());
        Assert.Equal(12, only.GetProperty("emiDayOfMonth").GetInt32());
        Assert.Equal("2026-10-22", only.GetProperty("firstEmiDate").GetString());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Get(A, "/api/v1/disbursals?since=yesterday")).StatusCode);
    }

    // ---- helpers ----------------------------------------------------------------------------------

    private static object Loan(string account, string name, decimal outstanding = 100m) => new
    {
        accountNumber = account, customerName = name, pan = "ABCDE1234F", mobile = "9876543210",
        product = "Car loan", sanctionedAmount = 1000m, outstandingAmount = outstanding, status = "Live",
        disbursedOn = "2026-01-15", branch = "Pune",
    };

    private void Seed(int company, string id, DateOnly? disbursedOn, bool disbursed)
    {
        using var db = NewContext();
        db.Applications.Add(new Application
        {
            Id = id, CompanyId = company, CustomerName = $"Cust {id}", LoanAmount = 250000m, Roi = 13.5m, Tenure = 36,
            Disbursed = disbursed, DisbursalDate = disbursedOn,
            FirstEmiDate = disbursedOn?.AddMonths(1).AddDays(2), EmiDayOfMonth = disbursed ? 12 : null,
        });
        db.SaveChanges();
    }

    private Task<HttpResponseMessage> Post(int company, IEnumerable<object> loans) =>
        Send(HttpMethod.Post, "/api/v1/loans", company, "loans:write loans:read", JsonContent.Create(new { loans }));

    private Task<HttpResponseMessage> Get(int company, string path) => Send(HttpMethod.Get, path, company, "loans:read");

    private Task<HttpResponseMessage> Delete(int company, string account) =>
        Send(HttpMethod.Delete, $"/api/v1/loans/{account}", company, "loans:write");

    private Task<HttpResponseMessage> Send(HttpMethod method, string path, int company, string scopes, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Add("X-Test-Company", company.ToString());
        request.Headers.Add("X-Test-Scope", scopes);
        request.Headers.Add("X-Test-Partner", "Paavan Software");
        return _http.SendAsync(request);
    }

    private static async Task<JsonElement> Body(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;

    private static int Count(JsonElement body, string name) => body.GetProperty(name).GetInt32();

    private LosDbContext NewContext()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "super"), new Claim(ClaimTypes.Role, TenantContext.SuperAdminRole),
        }, "TestAuth"));
        var tenant = new TenantContext(new FakeAuth(principal), new HttpContextAccessor(), new ActingCompanyStore());
        tenant.EnsureLoadedAsync().GetAwaiter().GetResult();
        return new LosDbContext(_options, tenant);
    }

    private sealed class TestFactory(PartnerLoanApiTests owner) : IDbContextFactory<LosDbContext>
    {
        // Sees every company, like the real cookie-less partner request does: the API's own company
        // predicate is then the only thing keeping tenants apart, and that is what the tests prove.
        public LosDbContext CreateDbContext() => owner.NewContext();
    }

    private sealed class FakeAuth(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
    }

    /// <summary>Stand-in for the PartnerBearer handler: turns three test headers into the real claims.</summary>
    private sealed class TestAuth(IOptionsMonitor<AuthenticationSchemeOptions> o, ILoggerFactory l, UrlEncoder e)
        : AuthenticationHandler<AuthenticationSchemeOptions>(o, l, e)
    {
        public const string Scheme = "TestPartner";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Company", out var company))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new("company_id", company.ToString()), new("connection_id", "c1"),
                new("partner_name", Request.Headers["X-Test-Partner"].ToString()),
            };
            claims.AddRange(Request.Headers["X-Test-Scope"].ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => new Claim("scope", s)));
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme)), Scheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
