using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LosLms.Tests;

/// <summary>
/// "Connect to LOS-LMS": the device flow Paavan's desktop software uses, ported from Handoff's tests. Runs on a
/// temp-file SQLite database (not in-memory) so the concurrent-poll test really uses two connections.
/// </summary>
public sealed class PartnerOAuthTests : IDisposable
{
    private const string Admin1 = "admin-1";
    private const string Staff1 = "staff-1";
    private static readonly DateTime T0 = new(2026, 10, 9, 10, 0, 0, DateTimeKind.Utc);

    private readonly string _file = Path.Combine(Path.GetTempPath(), $"partner-oauth-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<LosDbContext> _options;
    private readonly PartnerOAuthService _svc;
    private readonly ITestOutputHelper _output;
    private readonly string _clientId;

    public PartnerOAuthTests(ITestOutputHelper output)
    {
        _output = output;
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;

        using (var db = NewContext())
        {
            db.Database.EnsureCreated();
            db.Roles.AddRange(
                new IdentityRole { Id = "r-admin", Name = TenantContext.AdminRole, NormalizedName = "ADMIN" },
                new IdentityRole { Id = "r-staff", Name = TenantContext.StaffRole, NormalizedName = "STAFF" },
                new IdentityRole { Id = "r-super", Name = TenantContext.SuperAdminRole, NormalizedName = "SUPERADMIN" });
            AddUser(db, Admin1, 1, "r-admin");
            AddUser(db, Staff1, 1, "r-staff");
            AddUser(db, "super-only", null, "r-super");
            db.SaveChanges();
        }

        _svc = new PartnerOAuthService(new TestFactory(this));
        _clientId = _svc.CreateClientAsync("Paavan Software", "super-only", T0).GetAwaiter().GetResult();
    }

    private static void AddUser(LosDbContext db, string id, int? companyId, string roleId)
    {
        db.Users.Add(new ApplicationUser
        {
            Id = id, UserName = id, NormalizedUserName = id.ToUpperInvariant(), Email = $"{id}@x.test",
            DisplayName = id, CompanyId = companyId, IsActive = true,
        });
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = id, RoleId = roleId });
    }

    // ---- helpers ----

    private async Task<(string DeviceCode, string UserCode)> StartAsync(string? scope = null, DateTime? now = null)
    {
        var r = await _svc.StartDeviceAsync(_clientId, scope, "https://lms.test/link", now ?? T0);
        return (r.DeviceCode, r.UserCode);
    }

    private Task<TokenResponse> PollAsync(string deviceCode, DateTime now, string? clientId = null) =>
        _svc.TokenAsync(PartnerOAuthService.DeviceGrant, clientId ?? _clientId, deviceCode, null, now);

    private Task<TokenResponse> RefreshAsync(string refresh, DateTime now) =>
        _svc.TokenAsync(PartnerOAuthService.RefreshGrant, _clientId, null, refresh, now);

    /// <summary>A full connection: start, Admin allows, claim the tokens.</summary>
    private async Task<TokenResponse> ConnectAsync(string? scope = null, DateTime? now = null)
    {
        var t = now ?? T0;
        var (device, user) = await StartAsync(scope, t);
        Assert.Equal(DecideResult.Done, await _svc.DecideAsync(user, true, Admin1, t));
        return await PollAsync(device, t.AddSeconds(10));
    }

    private static async Task<OAuthException> ErrorOf(Task<TokenResponse> call) =>
        await Assert.ThrowsAsync<OAuthException>(() => call);

    // ---- device flow ----

    [Fact]
    public async Task HappyPath_PendingThenApprovedThenTokens()
    {
        var start = await _svc.StartDeviceAsync(_clientId, null, "https://lms.test/link", T0);
        Assert.Matches("^[BCDFGHJKLMNPQRSTVWXZ]{4}-[BCDFGHJKLMNPQRSTVWXZ]{4}$", start.UserCode);
        Assert.StartsWith("lcd_", start.DeviceCode);
        Assert.Equal((900, 5), (start.ExpiresIn, start.Interval));
        Assert.Equal($"https://lms.test/link?code={start.UserCode}", start.VerificationUriComplete);

        Assert.Equal("authorization_pending", (await ErrorOf(PollAsync(start.DeviceCode, T0.AddSeconds(5)))).Name);

        // The Admin may type the code lower-case, without the dash.
        var typed = start.UserCode.Replace("-", "").ToLowerInvariant();
        var seen = await _svc.FindDeviceAsync(typed, T0.AddSeconds(6));
        Assert.Equal(("Paavan Software", start.UserCode), (seen!.ClientName, seen.UserCode));
        Assert.Equal(["loans:write", "loans:read"], seen.Scopes);

        Assert.Equal(DecideResult.Done, await _svc.DecideAsync(typed, true, Admin1, T0.AddSeconds(7)));
        var tokens = await PollAsync(start.DeviceCode, T0.AddSeconds(12));

        Assert.StartsWith("lct_", tokens.AccessToken);
        Assert.StartsWith("lcr_", tokens.RefreshToken);
        Assert.Equal(("Bearer", 3600, "loans:write loans:read"), (tokens.TokenType, tokens.ExpiresIn, tokens.Scope));

        var who = await _svc.AuthenticateAsync(tokens.AccessToken, T0.AddMinutes(1));
        Assert.Equal((1, "Paavan Software"), (who!.CompanyId, who.PartnerName));
        Assert.Equal(["loans:write", "loans:read"], who.Scopes);

        // Codes and tokens are stored as SHA-256 only.
        await using var db = NewContext();
        var stored = db.PartnerTokens.Single();
        Assert.Equal(PartnerOAuthService.Hash(tokens.AccessToken), stored.AccessHash);
        Assert.Equal(PartnerOAuthService.Hash(tokens.RefreshToken), stored.RefreshHash);
        Assert.DoesNotContain(db.PartnerDeviceCodes.Select(d => d.DeviceHash + d.UserCodeHash), s => s.Contains(start.DeviceCode));
    }

    [Fact]
    public async Task Deny_PollingSaysAccessDenied()
    {
        var (device, user) = await StartAsync();
        Assert.Equal(DecideResult.Done, await _svc.DecideAsync(user, false, Admin1, T0));

        Assert.Equal("access_denied", (await ErrorOf(PollAsync(device, T0.AddSeconds(10)))).Name);
        Assert.Equal("access_denied", (await ErrorOf(PollAsync(device, T0.AddSeconds(20)))).Name);
    }

    [Fact]
    public async Task Expiry_LatePollSaysExpired_ApproveTooLateFails_PurgeAfterAnHour()
    {
        var (device, user) = await StartAsync();

        var late = T0.AddMinutes(16);
        Assert.Equal("expired_token", (await ErrorOf(PollAsync(device, late))).Name);
        Assert.Null(await _svc.FindDeviceAsync(user, late));
        Assert.Equal(DecideResult.Gone, await _svc.DecideAsync(user, true, Admin1, late));

        Assert.Equal(0, await _svc.PurgeAsync(T0.AddMinutes(15).AddMinutes(59))); // kept 1 h past expiry
        Assert.Equal(1, await _svc.PurgeAsync(T0.AddMinutes(15).AddMinutes(61)));
        Assert.Equal("invalid_grant", (await ErrorOf(PollAsync(device, T0.AddHours(2)))).Name);
    }

    [Fact]
    public async Task SlowDown_GrowsBy5_CappedAt60()
    {
        var (device, _) = await StartAsync();
        Assert.Equal("authorization_pending", (await ErrorOf(PollAsync(device, T0.AddSeconds(5)))).Name);

        var intervals = new List<int>();
        for (var i = 0; i < 14; i++)
        {
            var e = await ErrorOf(PollAsync(device, T0.AddSeconds(5))); // same instant = far too fast
            Assert.Equal("slow_down", e.Name);
            intervals.Add(int.Parse(Regex.Match(e.Message, @"\d+").Value));
        }

        Assert.Equal([10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 60, 60, 60], intervals);

        // Waiting out the (now 60 s) interval is just pending again.
        Assert.Equal("authorization_pending", (await ErrorOf(PollAsync(device, T0.AddSeconds(5 + 61)))).Name);
    }

    [Fact]
    public async Task TwoConcurrentPolls_OnlyOneWinsTheTokens()
    {
        var (device, user) = await StartAsync();
        await _svc.DecideAsync(user, true, Admin1, T0);

        var results = await Task.WhenAll(
            Task.Run(() => Try(PollAsync(device, T0.AddSeconds(10)))),
            Task.Run(() => Try(PollAsync(device, T0.AddSeconds(10)))));

        Assert.Single(results, r => r is TokenResponse);
        Assert.Single(results, r => r is OAuthException { Name: "invalid_grant" });
        await using var db = NewContext();
        Assert.Single(db.PartnerConnections.ToList());
        Assert.Single(db.PartnerTokens.ToList());

        static async Task<object> Try(Task<TokenResponse> call)
        {
            try { return await call; } catch (OAuthException e) { return e; }
        }
    }

    [Fact]
    public async Task UnknownOrDisabledClient_IsInvalidClient_AndDisablingCutsConnections()
    {
        await Assert.ThrowsAsync<OAuthException>(() => _svc.StartDeviceAsync("lc_nope", null, "https://x/link", T0));
        var start = await Assert.ThrowsAsync<OAuthException>(() => _svc.StartDeviceAsync(null, null, "https://x/link", T0));
        Assert.Equal("invalid_client", start.Name);
        Assert.Equal("invalid_client", (await ErrorOf(PollAsync("lcd_x", T0, "lc_nope"))).Name);

        var tokens = await ConnectAsync();
        Assert.NotNull(await _svc.AuthenticateAsync(tokens.AccessToken, T0.AddMinutes(1)));

        await _svc.DisableClientAsync(_clientId, T0.AddMinutes(2));

        Assert.Null(await _svc.AuthenticateAsync(tokens.AccessToken, T0.AddMinutes(3)));
        Assert.Equal("invalid_client", (await ErrorOf(RefreshAsync(tokens.RefreshToken, T0.AddMinutes(3)))).Name);
        Assert.Equal("invalid_client", (await Assert.ThrowsAsync<OAuthException>(
            () => _svc.StartDeviceAsync(_clientId, null, "https://x/link", T0.AddMinutes(3)))).Name);
        Assert.False(await _svc.RevokeAsync(_clientId, tokens.AccessToken, T0.AddMinutes(3)));
    }

    [Fact]
    public async Task ScopeAndGrantValidation()
    {
        var bad = await Assert.ThrowsAsync<OAuthException>(() => _svc.StartDeviceAsync(_clientId, "loans:write admin", "https://x/link", T0));
        Assert.Equal("invalid_scope", bad.Name);

        Assert.Equal("unsupported_grant_type", (await ErrorOf(_svc.TokenAsync("password", _clientId, null, null, T0))).Name);
        Assert.Equal("invalid_request", (await ErrorOf(_svc.TokenAsync(PartnerOAuthService.DeviceGrant, _clientId, null, null, T0))).Name);

        // A device code belongs to the client that started it.
        var (device, _) = await StartAsync();
        var other = await _svc.CreateClientAsync("Other App", "super-only", T0);
        Assert.Equal("invalid_grant", (await ErrorOf(_svc.TokenAsync(PartnerOAuthService.DeviceGrant, other, device, null, T0))).Name);
    }

    // ---- tokens ----

    [Fact]
    public async Task Refresh_Rotates_AndReusingTheOldRefreshTokenCutsTheConnection()
    {
        var first = await ConnectAsync();

        var later = T0.AddMinutes(30);
        var second = await RefreshAsync(first.RefreshToken, later);
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        Assert.NotEqual(first.AccessToken, second.AccessToken);
        Assert.Equal(first.Scope, second.Scope);
        Assert.Null(await _svc.AuthenticateAsync(first.AccessToken, later)); // the old pair is gone
        Assert.NotNull(await _svc.AuthenticateAsync(second.AccessToken, later));
        await using (var db = NewContext())
        {
            Assert.Equal(later.AddDays(90), db.PartnerTokens.Single(t => t.RefreshHash == PartnerOAuthService.Hash(second.RefreshToken)).RefreshExpiresAt);
        }

        // Replaying the used refresh token means it was copied: the whole connection is cut.
        Assert.Equal("invalid_grant", (await ErrorOf(RefreshAsync(first.RefreshToken, later.AddMinutes(1)))).Name);
        Assert.Null(await _svc.AuthenticateAsync(second.AccessToken, later.AddMinutes(1)));
        Assert.Equal("invalid_grant", (await ErrorOf(RefreshAsync(second.RefreshToken, later.AddMinutes(2)))).Name);
    }

    [Fact]
    public async Task AccessToken_ExpiresAfterAnHour_RefreshStillWorks()
    {
        var tokens = await ConnectAsync();

        Assert.NotNull(await _svc.AuthenticateAsync(tokens.AccessToken, T0.AddMinutes(59)));
        Assert.Null(await _svc.AuthenticateAsync(tokens.AccessToken, T0.AddMinutes(61)));
        Assert.NotNull(await RefreshAsync(tokens.RefreshToken, T0.AddMinutes(61)));
    }

    [Fact]
    public async Task Reapprove_NarrowsScopes_WithoutKillingAnotherPcsTokens()
    {
        var pc1 = await ConnectAsync();
        var pc2 = await ConnectAsync("loans:read", T0.AddMinutes(5));

        var who = await _svc.AuthenticateAsync(pc1.AccessToken, T0.AddMinutes(6));
        Assert.Equal(["loans:read"], who!.Scopes); // PC 1 still works, but only within the connection's scopes
        Assert.Equal("loans:read", (await RefreshAsync(pc1.RefreshToken, T0.AddMinutes(7))).Scope);
        await using var db = NewContext();
        Assert.Single(db.PartnerConnections.ToList());
        Assert.Equal("loans:read", pc2.Scope);
    }

    [Fact]
    public async Task Revoke_AlwaysSucceedsForAValidClient_AndCutsTheConnection()
    {
        var tokens = await ConnectAsync();

        Assert.True(await _svc.RevokeAsync(_clientId, "lct_never-issued", T0));
        Assert.True(await _svc.RevokeAsync(_clientId, null, T0));
        Assert.NotNull(await _svc.AuthenticateAsync(tokens.AccessToken, T0.AddMinutes(1)));

        Assert.True(await _svc.RevokeAsync(_clientId, tokens.RefreshToken, T0.AddMinutes(2)));
        Assert.Null(await _svc.AuthenticateAsync(tokens.AccessToken, T0.AddMinutes(3)));
        Assert.True(await _svc.RevokeAsync(_clientId, tokens.RefreshToken, T0.AddMinutes(4))); // again: still fine
        Assert.False(await _svc.RevokeAsync("lc_nope", tokens.RefreshToken, T0));
    }

    [Fact]
    public async Task Disconnect_KillsAccessAndRefresh_OnlyForTheOwningCompany()
    {
        var tokens = await ConnectAsync();
        var app = Assert.Single(await _svc.ListConnectionsAsync(1));
        Assert.Equal(("Paavan Software", Admin1), (app.AppName, app.ApprovedBy));
        Assert.Empty(await _svc.ListConnectionsAsync(2));

        Assert.False(await _svc.DisconnectAsync(app.Id, 2, T0.AddMinutes(1))); // another company can't
        Assert.NotNull(await _svc.AuthenticateAsync(tokens.AccessToken, T0.AddMinutes(1)));

        Assert.True(await _svc.DisconnectAsync(app.Id, 1, T0.AddMinutes(2)));
        Assert.Null(await _svc.AuthenticateAsync(tokens.AccessToken, T0.AddMinutes(3)));
        Assert.Equal("invalid_grant", (await ErrorOf(RefreshAsync(tokens.RefreshToken, T0.AddMinutes(3)))).Name);
        Assert.Empty(await _svc.ListConnectionsAsync(1));
        Assert.False(await _svc.DisconnectAsync(app.Id, 1, T0.AddMinutes(4)));
    }

    [Fact]
    public async Task LastUsed_IsStampedWhenATokenIsUsed()
    {
        var tokens = await ConnectAsync();
        Assert.Null(Assert.Single(await _svc.ListConnectionsAsync(1)).LastUsedAt);

        await _svc.AuthenticateAsync(tokens.AccessToken, T0.AddMinutes(9));

        Assert.Equal(T0.AddMinutes(9), Assert.Single(await _svc.ListConnectionsAsync(1)).LastUsedAt);
    }

    [Theory]
    [InlineData("demoted")]
    [InlineData("deactivated")]
    public async Task ApproverNoLongerAdmin_TokenStops_AndRefreshCutsTheConnection(string how)
    {
        var tokens = await ConnectAsync();
        Assert.NotNull(await _svc.AuthenticateAsync(tokens.AccessToken, T0.AddMinutes(1)));

        await using (var db = NewContext())
        {
            if (how == "demoted")
            {
                db.UserRoles.Remove(db.UserRoles.Single(r => r.UserId == Admin1));
                db.UserRoles.Add(new IdentityUserRole<string> { UserId = Admin1, RoleId = "r-staff" });
            }
            else
            {
                db.Users.Single(u => u.Id == Admin1).IsActive = false;
            }

            await db.SaveChangesAsync();
        }

        Assert.Null(await _svc.AuthenticateAsync(tokens.AccessToken, T0.AddMinutes(2)));
        Assert.Equal("invalid_grant", (await ErrorOf(RefreshAsync(tokens.RefreshToken, T0.AddMinutes(2)))).Name);
        Assert.Empty(await _svc.ListConnectionsAsync(1));
    }

    // ---- /link: who may approve, and the wrong-code limiter ----

    [Theory]
    [InlineData(Staff1)]
    [InlineData("super-only")] // SuperAdmin alone is not a company Admin
    [InlineData("nobody")]
    public async Task OnlyAnActiveAdminCanApproveOrDeny(string userId)
    {
        var (device, user) = await StartAsync();

        Assert.Equal(DecideResult.NotAdmin, await _svc.DecideAsync(user, true, userId, T0));
        Assert.Equal(DecideResult.NotAdmin, await _svc.DecideAsync(user, false, userId, T0));

        Assert.Equal("authorization_pending", (await ErrorOf(PollAsync(device, T0.AddSeconds(10)))).Name); // untouched
        Assert.Equal(DecideResult.Done, await _svc.DecideAsync(user, true, Admin1, T0));
    }

    [Fact]
    public async Task ACodeCanBeDecidedOnlyOnce_AndAWrongCodeFindsNothing()
    {
        var (_, user) = await StartAsync();

        Assert.Null(await _svc.FindDeviceAsync("BCDF-GHJK", T0));
        Assert.Null(await _svc.FindDeviceAsync("short", T0));
        Assert.Equal(DecideResult.Done, await _svc.DecideAsync(user, true, Admin1, T0));
        Assert.Equal(DecideResult.Gone, await _svc.DecideAsync(user, false, Admin1, T0));
        Assert.Null(await _svc.FindDeviceAsync(user, T0)); // no longer pending
    }

    [Fact]
    public void WrongCodeLimiter_FiveMissesPerUserAndPerIp_ThenPausesFor15Minutes()
    {
        var limiter = new PartnerGuessLimiter();
        string[] alice1 = ["user:alice", "ip:1.1.1.1"];
        string[] alice2 = ["user:alice", "ip:2.2.2.2"];
        string[] bob1 = ["user:bob", "ip:1.1.1.1"];
        string[] carol3 = ["user:carol", "ip:3.3.3.3"];

        for (var i = 0; i < 5; i++)
        {
            Assert.False(limiter.Blocked(alice1, T0));
            limiter.Miss(alice1, T0.AddSeconds(i));
        }

        Assert.Equal(0, limiter.Left(alice1, T0.AddSeconds(5)));
        Assert.True(limiter.Blocked(alice1, T0.AddSeconds(5)));
        Assert.True(limiter.Blocked(alice2, T0.AddSeconds(5))); // same person, other address
        Assert.True(limiter.Blocked(bob1, T0.AddSeconds(5)));   // other person, same address
        Assert.False(limiter.Blocked(carol3, T0.AddSeconds(5)));
        Assert.False(limiter.Blocked(alice1, T0.AddMinutes(16))); // window passed
    }

    [Fact]
    public void WrongCodeLimiter_RemembersAtMostMaxKeys()
    {
        var limiter = new PartnerGuessLimiter(max: 5, maxKeys: 3);
        for (var i = 0; i < 10; i++)
        {
            limiter.Miss([$"ip:{i}"], T0);
        }

        // The oldest were forgotten, the newest still counted (and a repeat offender is still blocked).
        for (var i = 0; i < 4; i++) { limiter.Miss(["ip:9"], T0); }
        Assert.True(limiter.Blocked(["ip:9"], T0));
        Assert.False(limiter.Blocked(["ip:0"], T0));
    }

    // ---- the PartnerBearer scheme the data API builds on ----

    [Fact]
    public async Task PartnerBearer_SetsTheAgreedClaims_AndScopePoliciesFollowThem()
    {
        var tokens = await ConnectAsync(now: DateTime.UtcNow.AddMinutes(-1)); // the handler uses the real clock
        await using var provider = new ServiceCollection()
            .AddLogging().AddSingleton<IDbContextFactory<LosDbContext>>(new TestFactory(this))
            .AddScoped<PartnerOAuthService>().AddPartnerAuth().BuildServiceProvider();

        async Task<(AuthenticateResult Result, IAuthorizationService Auth)> Run(string? header)
        {
            var http = new DefaultHttpContext { RequestServices = provider.CreateScope().ServiceProvider };
            if (header is not null) { http.Request.Headers.Authorization = header; }
            var result = await http.AuthenticateAsync(PartnerAuth.Scheme);
            return (result, http.RequestServices.GetRequiredService<IAuthorizationService>());
        }

        var (ok, auth) = await Run("Bearer " + tokens.AccessToken);
        Assert.True(ok.Succeeded);
        var p = ok.Principal!;
        Assert.Equal("1", p.FindFirstValue("company_id"));
        Assert.Equal("Paavan Software", p.FindFirstValue("partner_name"));
        Assert.False(string.IsNullOrEmpty(p.FindFirstValue("connection_id")));
        Assert.Equal(["loans:write", "loans:read"], p.FindAll("scope").Select(c => c.Value));
        Assert.True((await auth.AuthorizeAsync(p, "loans:read")).Succeeded);
        Assert.True((await auth.AuthorizeAsync(p, "loans:write")).Succeeded);

        // A read-only connection fails the write policy.
        var readOnly = await ConnectAsync("loans:read", DateTime.UtcNow.AddMinutes(-1));
        var (ro, _) = await Run("Bearer " + readOnly.AccessToken);
        Assert.True((await auth.AuthorizeAsync(ro.Principal!, "loans:read")).Succeeded);
        Assert.False((await auth.AuthorizeAsync(ro.Principal!, "loans:write")).Succeeded);

        Assert.True((await Run(null)).Result.None); // no header: other schemes get their turn
        Assert.True((await Run("Bearer something-else")).Result.None);
        Assert.NotNull((await Run("Bearer lct_forged")).Result.Failure); // looks like ours but isn't → 401
    }

    // ---- wire format: the exact JSON Paavan's module parses ----

    [Fact]
    public async Task Endpoints_SpeakHandoffsWireContract()
    {
        var services = new ServiceCollection().AddSingleton(_svc).BuildServiceProvider();
        var unknown = await Post(services, "/oauth/token", "grant_type=x&client_id=lc_nope");
        Assert.Equal(400, unknown.Status);
        Assert.Equal("invalid_client", unknown.Json.GetProperty("error").GetString());
        Assert.Equal("no-store", unknown.CacheControl);

        var start = await Post(services, "/oauth/device_authorization", $"client_id={_clientId}&scope=loans%3Awrite+loans%3Aread");
        _output.WriteLine($"device_authorization -> {start.Status} {start.Body}");
        Assert.Equal(200, start.Status);
        Assert.Equal(
            ["device_code", "user_code", "verification_uri", "verification_uri_complete", "expires_in", "interval"],
            start.Json.EnumerateObject().Select(p => p.Name));
        Assert.Equal("https://lms.test/link", start.Json.GetProperty("verification_uri").GetString());
        var device = start.Json.GetProperty("device_code").GetString()!;
        var form = $"grant_type={Uri.EscapeDataString(PartnerOAuthService.DeviceGrant)}&device_code={device}&client_id={_clientId}";

        var pending = await Post(services, "/oauth/token", form);
        _output.WriteLine($"token (pending) -> {pending.Status} {pending.Body}");
        Assert.Equal(400, pending.Status);
        Assert.Equal("authorization_pending", pending.Json.GetProperty("error").GetString());

        await _svc.DecideAsync(start.Json.GetProperty("user_code").GetString(), true, Admin1, DateTime.UtcNow);
        var success = await Post(services, "/oauth/token", form);
        _output.WriteLine($"token (success) -> {success.Status} {Regex.Replace(success.Body, "(lc[tr]_)[A-Za-z0-9_-]+", "$1…")}");
        Assert.Equal(200, success.Status);
        Assert.Equal(["access_token", "token_type", "expires_in", "refresh_token", "scope"], success.Json.EnumerateObject().Select(p => p.Name));
        Assert.Equal("no-store", success.CacheControl);

        var refresh = success.Json.GetProperty("refresh_token").GetString()!;
        var rotated = await Post(services, "/oauth/token", $"grant_type=refresh_token&refresh_token={refresh}&client_id={_clientId}");
        Assert.Equal(200, rotated.Status);
        var reuse = await Post(services, "/oauth/token", $"grant_type=refresh_token&refresh_token={refresh}&client_id={_clientId}");
        _output.WriteLine($"token (refresh reuse) -> {reuse.Status} {reuse.Body}");
        Assert.Equal("invalid_grant", reuse.Json.GetProperty("error").GetString());

        var revoked = await Post(services, "/oauth/revoke", $"token=lct_unknown&client_id={_clientId}");
        _output.WriteLine($"revoke (unknown token) -> {revoked.Status} {revoked.Body}");
        Assert.Equal((200, "{}"), (revoked.Status, revoked.Body));

        // Not form-encoded (e.g. JSON) is treated as an empty form, never a 500.
        var json = await Post(services, "/oauth/token", $"{{\"client_id\":\"{_clientId}\"}}", "application/json");
        Assert.Equal(400, json.Status);
        Assert.Equal(404, (await Post(services, "/oauth/nope", "")).Status);
    }

    [Theory]
    [InlineData("https://los-lms.bhodhix.com", "https://los-lms.bhodhix.com/link")]
    [InlineData("https://los-lms.bhodhix.com/", "https://los-lms.bhodhix.com/link")] // trailing slash tolerated
    [InlineData("", "https://lms.test/link")]                                         // unset: the request's own scheme + host
    public async Task VerificationUri_UsesPublicBaseUrlWhenSet(string configured, string expected)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["PublicBaseUrl"] = configured }).Build();
        var services = new ServiceCollection().AddSingleton(_svc).AddSingleton<IConfiguration>(config).BuildServiceProvider();

        var start = await Post(services, "/oauth/device_authorization", $"client_id={_clientId}");

        Assert.Equal(expected, start.Json.GetProperty("verification_uri").GetString());
        Assert.StartsWith(expected + "?code=", start.Json.GetProperty("verification_uri_complete").GetString());
    }

    private async Task<(int Status, string Body, JsonElement Json, string? CacheControl)> Post(
        IServiceProvider services, string path, string body, string contentType = "application/x-www-form-urlencoded")
    {
        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Method = "POST";
        http.Request.Path = path;
        http.Request.Scheme = "https";
        http.Request.Host = new HostString("lms.test");
        http.Request.ContentType = contentType;
        http.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        http.Response.Body = new MemoryStream();

        await PartnerOAuthEndpoints.HandleAsync(http);

        http.Response.Body.Position = 0;
        var text = await new StreamReader(http.Response.Body).ReadToEndAsync();
        return (http.Response.StatusCode, text, JsonDocument.Parse(text).RootElement.Clone(), http.Response.Headers.CacheControl.ToString());
    }

    // ---- plumbing ----

    private LosDbContext NewContext()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "super"), new Claim(ClaimTypes.Role, TenantContext.SuperAdminRole)], "TestAuth"));
        var tenant = new TenantContext(new FakeAuth(principal), new HttpContextAccessor(), new ActingCompanyStore());
        tenant.EnsureLoadedAsync().GetAwaiter().GetResult();
        return new LosDbContext(_options, tenant);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_file);
    }

    private sealed class TestFactory(PartnerOAuthTests owner) : IDbContextFactory<LosDbContext>
    {
        public LosDbContext CreateDbContext() => owner.NewContext();
    }

    private sealed class FakeAuth(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
    }
}
