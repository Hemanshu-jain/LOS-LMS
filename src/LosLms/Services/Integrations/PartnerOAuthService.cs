using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>An OAuth error reply: <c>{"error": Name, "error_description": message}</c> with <see cref="Status"/>.</summary>
public sealed class OAuthException(string name, string description, int status = 400) : Exception(description)
{
    public string Name { get; } = name;

    public int Status { get; } = status;
}

public sealed record DeviceAuthorizationResponse(
    [property: JsonPropertyName("device_code")] string DeviceCode,
    [property: JsonPropertyName("user_code")] string UserCode,
    [property: JsonPropertyName("verification_uri")] string VerificationUri,
    [property: JsonPropertyName("verification_uri_complete")] string VerificationUriComplete,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("interval")] int Interval);

public sealed record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("scope")] string Scope);

/// <summary>What /link shows the Admin before Allow/Deny.</summary>
public sealed record DeviceRequest(string UserCode, string ClientName, IReadOnlyList<string> Scopes, DateTime RequestedAt, string? RequesterIp);

/// <summary>A live access token's connection: what the data API trusts. Tenant = <see cref="CompanyId"/>.</summary>
public sealed record PartnerIdentity(
    int CompanyId, string ConnectionId, string PartnerName, IReadOnlyList<string> Scopes, PartnerBlock? Blocked = null);

/// <summary>A valid token whose company may not use the API right now (→ 403 with this error and message).</summary>
public sealed record PartnerBlock(string Error, string Message);

public sealed record ConnectedApp(string Id, string AppName, string ApprovedBy, DateTime ConnectedAt, DateTime? LastUsedAt);

public sealed record PartnerAppRow(string Id, string Name, DateTime CreatedAt, DateTime? DisabledAt, int Connections);

public enum DecideResult { Done, NotAdmin, Gone }

/// <summary>
/// "Connect to LOS-LMS": OAuth 2.0 device authorization (RFC 8628) for partner desktop apps, hand-rolled to
/// match Handoff's wire contract exactly so Paavan's VB.NET module only changes base URL, client id and scope.
/// Every code and token is stored as SHA-256 only. Every method takes the clock as a parameter.
/// </summary>
public sealed class PartnerOAuthService(
    IDbContextFactory<LosDbContext> dbFactory, LicenseService? license = null, ApiBillingService? billing = null)
{
    /// <summary>Live pending device codes one app may hold; past it a new request is refused.</summary>
    public const int MaxPendingPerClient = 1000;

    public const string ScopeWrite = "loans:write";
    public const string ScopeRead = "loans:read";
    public static readonly IReadOnlyList<string> AllScopes = [ScopeWrite, ScopeRead];

    public const string DeviceGrant = "urn:ietf:params:oauth:grant-type:device_code";
    public const string RefreshGrant = "refresh_token";
    public const string AccessPrefix = "lct_";

    private const string ClientPrefix = "lc_";
    private const string DevicePrefix = "lcd_";
    private const string RefreshPrefix = "lcr_";

    public static readonly TimeSpan AccessLifetime = TimeSpan.FromHours(1);
    public static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(90); // renewed on every refresh
    public static readonly TimeSpan DeviceLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan DeviceKeep = TimeSpan.FromHours(1); // late polls still hear expired_token

    private const int PollSeconds = 5;
    private const int MaxPollSeconds = 60;

    // 20 consonants: no vowels (no accidental words), nothing that looks like 0/O or 1/I. 8 letters ≈ 2.6e10.
    private const string UserCodeAlphabet = "BCDFGHJKLMNPQRSTVWXZ";
    private const int UserCodeLength = 8;

    public static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string Token(string prefix) => prefix + Base64Url(RandomNumberGenerator.GetBytes(32));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string[] Split(string? scopes) =>
        (scopes ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static string NormalizeUserCode(string? value) =>
        new((value ?? "").ToUpperInvariant().Where(c => c is >= 'A' and <= 'Z').ToArray());

    public static string FormatUserCode(string code) => $"{code[..4]}-{code[4..]}";

    private static string NewUserCode() =>
        new(Enumerable.Range(0, UserCodeLength).Select(_ => UserCodeAlphabet[RandomNumberGenerator.GetInt32(UserCodeAlphabet.Length)]).ToArray());

    // ---- Partner registry (SuperAdmin) ----

    public async Task<string> CreateClientAsync(string name, string userId, DateTime now)
    {
        var clean = (name ?? "").Trim();
        if (clean.Length is < 2 or > 120)
        {
            throw new ArgumentException("Name the partner app (2 to 120 characters).");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var id = ClientPrefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
        db.PartnerClients.Add(new PartnerClient
        {
            Id = id, Name = clean, Scopes = string.Join(' ', AllScopes), CreatedByUserId = userId, CreatedAt = now,
        });
        await db.SaveChangesAsync();
        return id;
    }

    public async Task<IReadOnlyList<PartnerAppRow>> ListClientsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.PartnerClients.AsNoTracking().OrderByDescending(c => c.CreatedAt)
            .Select(c => new PartnerAppRow(c.Id, c.Name, c.CreatedAt, c.DisabledAt,
                db.PartnerConnections.Count(k => k.ClientId == c.Id && k.RevokedAt == null)))
            .ToListAsync();
    }

    /// <summary>Disabling a partner app also cuts every company's connection to it.</summary>
    public async Task DisableClientAsync(string clientId, DateTime now)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.PartnerClients.Where(c => c.Id == clientId && c.DisabledAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.DisabledAt, now));
        var connections = db.PartnerConnections.Where(k => k.ClientId == clientId && k.RevokedAt == null);
        await RevokeTokensAsync(db, connections.Select(k => k.Id), now);
        await connections.ExecuteUpdateAsync(s => s.SetProperty(k => k.RevokedAt, now));
    }

    private static Task<int> RevokeTokensAsync(LosDbContext db, IQueryable<string> connectionIds, DateTime now) =>
        db.PartnerTokens.Where(t => connectionIds.Contains(t.ConnectionId) && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));

    // ---- Device authorization: POST /oauth/device_authorization ----

    public async Task<DeviceAuthorizationResponse> StartDeviceAsync(
        string? clientId, string? scope, string verificationUri, DateTime now, string? requesterIp = null)
    {
        await using var probe = await dbFactory.CreateDbContextAsync();
        var client = await probe.PartnerClients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == (clientId ?? "") && c.DisabledAt == null)
            ?? throw new OAuthException("invalid_client", "This app is not registered with LOS-LMS for device linking.");

        var allowed = Split(client.Scopes);
        var wanted = string.IsNullOrWhiteSpace(scope) ? allowed : Split(scope);
        if (wanted.Length == 0 || wanted.Any(s => !allowed.Contains(s)))
        {
            throw new OAuthException("invalid_scope", $"This app may only ask for: {string.Join(' ', allowed)}.");
        }

        // Bounds the table against someone minting codes in a loop. temporarily_unavailable (RFC 6749 §4.1.2.1) is
        // the standard answer for "try again later"; slow_down belongs to the token endpoint's polling.
        if (await probe.PartnerDeviceCodes.CountAsync(d => d.ClientId == client.Id && d.Status == "pending" && d.ExpiresAt > now) >= MaxPendingPerClient)
        {
            throw new OAuthException("temporarily_unavailable", "Too many connection requests are waiting. Try again in a few minutes.", 503);
        }

        var deviceCode = Token(DevicePrefix);
        for (var attempt = 0; ; attempt++)
        {
            var userCode = NewUserCode();
            await using var db = await dbFactory.CreateDbContextAsync();
            db.PartnerDeviceCodes.Add(new PartnerDeviceCode
            {
                DeviceHash = Hash(deviceCode), UserCodeHash = Hash(userCode), ClientId = client.Id,
                Scopes = string.Join(' ', wanted), PollInterval = PollSeconds, CreatedAt = now, ExpiresAt = now + DeviceLifetime,
                RequesterIp = requesterIp,
            });
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException) when (attempt < 4)
            {
                continue; // that user code is already in use: draw another
            }

            var shown = FormatUserCode(userCode);
            return new DeviceAuthorizationResponse(deviceCode, shown, verificationUri, $"{verificationUri}?code={shown}",
                (int)DeviceLifetime.TotalSeconds, PollSeconds);
        }
    }

    // ---- /link: look up the code the Admin typed, then Allow/Deny ----

    /// <summary>The live request for a typed code, or null (the caller counts the wrong guess).</summary>
    public async Task<DeviceRequest?> FindDeviceAsync(string? userCode, DateTime now)
    {
        var code = NormalizeUserCode(userCode);
        if (code.Length != UserCodeLength)
        {
            return null;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var hash = Hash(code);
        var row = await (from d in db.PartnerDeviceCodes.AsNoTracking()
                         join c in db.PartnerClients on d.ClientId equals c.Id
                         where d.UserCodeHash == hash && d.Status == "pending" && d.ExpiresAt > now && c.DisabledAt == null
                         select new { c.Name, d.Scopes, d.CreatedAt, d.RequesterIp }).FirstOrDefaultAsync();
        return row is null ? null : new DeviceRequest(FormatUserCode(code), row.Name, Split(row.Scopes), row.CreatedAt, row.RequesterIp);
    }

    /// <summary>
    /// Allow or Deny. Only a currently active user holding the Admin role may decide; the company is that
    /// Admin's own (never taken from the request). One statement flips pending → decided, so a code can only
    /// be decided once.
    /// </summary>
    public async Task<DecideResult> DecideAsync(string? userCode, bool allow, string userId, DateTime now)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var admin = await ActiveAdmins(db).Where(u => u.Id == userId).Select(u => new { u.CompanyId }).FirstOrDefaultAsync();
        if (admin is null)
        {
            return DecideResult.NotAdmin;
        }

        var hash = Hash(NormalizeUserCode(userCode));
        var status = allow ? "approved" : "denied";
        var company = allow ? admin.CompanyId : null;
        var user = allow ? userId : null;
        var rows = await db.PartnerDeviceCodes
            .Where(d => d.UserCodeHash == hash && d.Status == "pending" && d.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.Status, status)
                .SetProperty(d => d.CompanyId, company).SetProperty(d => d.UserId, user));
        return rows == 1 ? DecideResult.Done : DecideResult.Gone;
    }

    // Active user, Admin role, belongs to a company. (SuperAdmin alone does not count.) Tenant filters
    // are bypassed on purpose: the token endpoints run with nobody signed in.
    private static IQueryable<ApplicationUser> ActiveAdmins(LosDbContext db) =>
        db.Users.IgnoreQueryFilters().Where(u => u.IsActive && u.CompanyId != null
            && db.UserRoles.Any(ur => ur.UserId == u.Id
                && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == TenantContext.AdminRole)));

    // ---- POST /oauth/token ----

    public async Task<TokenResponse> TokenAsync(
        string? grantType, string? clientId, string? deviceCode, string? refreshToken, DateTime now)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var client = await db.PartnerClients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == (clientId ?? "") && c.DisabledAt == null)
            ?? throw new OAuthException("invalid_client", "Invalid client: client is invalid");

        return grantType switch
        {
            DeviceGrant => await DeviceTokenAsync(db, client, Required(deviceCode, "device_code"), now),
            RefreshGrant => await RefreshTokenAsync(db, client, Required(refreshToken, "refresh_token"), now),
            _ => throw new OAuthException("unsupported_grant_type", "Unsupported grant type: `grant_type` is invalid"),
        };
    }

    private static string Required(string? value, string name) =>
        string.IsNullOrEmpty(value) ? throw new OAuthException("invalid_request", $"Missing parameter: `{name}`") : value;

    private static OAuthException InvalidGrant(string why) => new("invalid_grant", $"Invalid grant: {why}");

    private async Task<TokenResponse> DeviceTokenAsync(LosDbContext db, PartnerClient client, string deviceCode, DateTime now)
    {
        var hash = Hash(deviceCode);
        var row = await db.PartnerDeviceCodes.AsNoTracking().FirstOrDefaultAsync(d => d.DeviceHash == hash && d.ClientId == client.Id);
        if (row is null || row.Status == "used")
        {
            throw InvalidGrant("device code is invalid");
        }

        if (row.ExpiresAt <= now)
        {
            throw new OAuthException("expired_token", "The code expired. Start Connect to LOS-LMS again.");
        }

        if (row.Status == "denied")
        {
            throw new OAuthException("access_denied", "The person declined the connection.");
        }

        if (row.Status == "pending")
        {
            var tooFast = row.LastPolledAt is { } last && (now - last).TotalSeconds < row.PollInterval;
            var slower = Math.Min(row.PollInterval + 5, MaxPollSeconds);
            var interval = tooFast ? slower : row.PollInterval;
            await db.PartnerDeviceCodes.Where(d => d.DeviceHash == hash)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.LastPolledAt, now).SetProperty(d => d.PollInterval, interval));
            throw tooFast
                ? new OAuthException("slow_down", $"Poll every {slower} seconds.")
                : new OAuthException("authorization_pending", "Waiting for the person to approve.");
        }

        // Approved: only one poll may claim it.
        var claimed = await db.PartnerDeviceCodes.Where(d => d.DeviceHash == hash && d.Status == "approved")
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.Status, "used"));
        if (claimed != 1 || row.CompanyId is not { } companyId || row.UserId is not { } userId)
        {
            throw InvalidGrant("device code is invalid");
        }

        // One live connection per (company, app). A re-approve resets its scopes and approver; other PCs'
        // tokens keep working but never beyond the connection's current scopes, and are NOT revoked (a revoked
        // token coming back looks like a stolen refresh token and would cut every PC).
        var connection = await db.PartnerConnections.FirstOrDefaultAsync(k => k.CompanyId == companyId && k.ClientId == client.Id && k.RevokedAt == null);
        if (connection is null)
        {
            connection = new PartnerConnection
            {
                Id = $"conn-{Guid.NewGuid():N}", CompanyId = companyId, ClientId = client.Id, CreatedAt = now,
            };
            db.PartnerConnections.Add(connection);
        }

        connection.ApprovedByUserId = userId;
        connection.Scopes = row.Scopes;
        return await IssueAsync(db, connection, Split(row.Scopes), now);
    }

    private async Task<TokenResponse> RefreshTokenAsync(LosDbContext db, PartnerClient client, string refreshToken, DateTime now)
    {
        var hash = Hash(refreshToken);
        var admins = ActiveAdmins(db);
        var row = await (from t in db.PartnerTokens.AsNoTracking()
                         join k in db.PartnerConnections.AsNoTracking() on t.ConnectionId equals k.Id
                         where t.RefreshHash == hash && k.ClientId == client.Id
                         select new
                         {
                             t.Id, t.RevokedAt, t.RefreshExpiresAt, TokenScopes = t.Scopes, Connection = k,
                             ApproverOk = admins.Any(u => u.Id == k.ApprovedByUserId && u.CompanyId == k.CompanyId),
                         }).FirstOrDefaultAsync();
        if (row is null || row.Connection.RevokedAt is not null)
        {
            throw InvalidGrant("refresh token is invalid");
        }

        // A used refresh token coming back was copied; an approver who lost Admin can't keep a connection:
        // either way cut the whole connection.
        if (row.RevokedAt is not null || !row.ApproverOk)
        {
            await RevokeConnectionAsync(db, row.Connection.Id, now);
            throw InvalidGrant("refresh token is invalid");
        }

        if (row.RefreshExpiresAt <= now)
        {
            throw InvalidGrant("refresh token has expired");
        }

        var won = await db.PartnerTokens.Where(t => t.Id == row.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
        if (won != 1)
        {
            throw InvalidGrant("refresh token is invalid"); // a concurrent refresh got there first
        }

        var allowed = Split(row.Connection.Scopes);
        var connection = await db.PartnerConnections.FirstAsync(k => k.Id == row.Connection.Id);
        return await IssueAsync(db, connection, Split(row.TokenScopes).Where(allowed.Contains).ToArray(), now);
    }

    // Saves the connection (new or changed) and a fresh token pair in one SaveChanges.
    private static async Task<TokenResponse> IssueAsync(LosDbContext db, PartnerConnection connection, string[] scopes, DateTime now)
    {
        var access = Token(AccessPrefix);
        var refresh = Token(RefreshPrefix);
        var scope = string.Join(' ', scopes);
        db.PartnerTokens.Add(new PartnerToken
        {
            Id = $"tok-{Guid.NewGuid():N}", ConnectionId = connection.Id,
            AccessHash = Hash(access), AccessExpiresAt = now + AccessLifetime,
            RefreshHash = Hash(refresh), RefreshExpiresAt = now + RefreshLifetime,
            Scopes = scope, CreatedAt = now,
        });
        await db.SaveChangesAsync();
        return new TokenResponse(access, "Bearer", (int)AccessLifetime.TotalSeconds, refresh, scope);
    }

    // ---- POST /oauth/revoke ----

    /// <summary>
    /// The partner's own Disconnect: ends the connection the token belongs to. Returns false only for an
    /// unknown/disabled client; an unknown token is still success (RFC 7009), so the caller answers 200.
    /// </summary>
    public async Task<bool> RevokeAsync(string? clientId, string? token, DateTime now)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.PartnerClients.AnyAsync(c => c.Id == (clientId ?? "") && c.DisabledAt == null))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(token))
        {
            var hash = Hash(token);
            var connectionId = await (from t in db.PartnerTokens
                                      join k in db.PartnerConnections on t.ConnectionId equals k.Id
                                      where (t.AccessHash == hash || t.RefreshHash == hash) && k.ClientId == clientId
                                      select t.ConnectionId).FirstOrDefaultAsync();
            if (connectionId is not null)
            {
                await RevokeConnectionAsync(db, connectionId, now);
            }
        }

        return true;
    }

    private static async Task RevokeConnectionAsync(LosDbContext db, string connectionId, DateTime now)
    {
        await RevokeTokensAsync(db, db.PartnerConnections.Where(k => k.Id == connectionId).Select(k => k.Id), now);
        await db.PartnerConnections.Where(k => k.Id == connectionId && k.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.RevokedAt, now));
    }

    // ---- Connected apps (company Admin) ----

    public async Task<IReadOnlyList<ConnectedApp>> ListConnectionsAsync(int companyId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await (from k in db.PartnerConnections.AsNoTracking()
                      join c in db.PartnerClients on k.ClientId equals c.Id
                      join u in db.Users.IgnoreQueryFilters() on k.ApprovedByUserId equals u.Id
                      where k.CompanyId == companyId && k.RevokedAt == null
                      orderby k.CreatedAt descending
                      select new ConnectedApp(k.Id, c.Name, u.DisplayName != "" ? u.DisplayName : u.Email!, k.CreatedAt, k.LastUsedAt))
            .ToListAsync();
    }

    /// <summary>Disconnect: revokes the connection and all its tokens. False if it is not this company's live connection.</summary>
    public async Task<bool> DisconnectAsync(string connectionId, int companyId, DateTime now)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.PartnerConnections.AnyAsync(k => k.Id == connectionId && k.CompanyId == companyId && k.RevokedAt == null))
        {
            return false;
        }

        await RevokeConnectionAsync(db, connectionId, now);
        return true;
    }

    // ---- Bearer tokens on the data API ----

    /// <summary>
    /// The connection behind a live access token, or null (→ 401): token live, connection and app active, and
    /// the approver still an active Admin of that company. Stamps <c>last used</c>. Tenant = the connection's.
    /// </summary>
    public async Task<PartnerIdentity?> AuthenticateAsync(string? accessToken, DateTime now)
    {
        if (accessToken is null || !accessToken.StartsWith(AccessPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var hash = Hash(accessToken);
        var admins = ActiveAdmins(db);
        var row = await (from t in db.PartnerTokens.AsNoTracking()
                         join k in db.PartnerConnections.AsNoTracking() on t.ConnectionId equals k.Id
                         join c in db.PartnerClients.AsNoTracking() on k.ClientId equals c.Id
                         where t.AccessHash == hash && t.RevokedAt == null && t.AccessExpiresAt > now
                               && k.RevokedAt == null && c.DisabledAt == null
                               && admins.Any(u => u.Id == k.ApprovedByUserId && u.CompanyId == k.CompanyId)
                         select new { TokenScopes = t.Scopes, ConnectionScopes = k.Scopes, k.Id, k.CompanyId, c.Name })
            .FirstOrDefaultAsync();
        if (row is null)
        {
            return null;
        }

        await db.PartnerConnections.Where(k => k.Id == row.Id).ExecuteUpdateAsync(s => s.SetProperty(k => k.LastUsedAt, now));
        var allowed = Split(row.ConnectionScopes);
        return new PartnerIdentity(row.CompanyId, row.Id, row.Name, Split(row.TokenScopes).Where(allowed.Contains).ToArray(),
            await BlockedAsync(row.CompanyId, now));
    }

    // Company has no suspended/disabled flag; what can switch a company's API off is the licence (the whole
    // install freezes once it has expired) and an unpaid API bill past its grace date.
    private async Task<PartnerBlock?> BlockedAsync(int companyId, DateTime now)
    {
        if (license is { IsFrozen: true })
        {
            return new PartnerBlock("subscription_ended", "This LOS-LMS subscription has ended. Ask your company Admin to renew it.");
        }

        return billing is not null && await billing.CheckAccessAsync(companyId, now) is { Allowed: false } access
            ? new PartnerBlock("api_paused", access.Reason ?? "API access is paused for an unpaid bill.")
            : null;
    }

    // ---- Housekeeping ----

    /// <summary>Deletes device codes one hour past expiry. Returns how many.</summary>
    public async Task<int> PurgeAsync(DateTime now)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await PurgeAsync(db, now);
    }

    /// <summary>The delete itself, on a context the caller supplies. The hourly background job passes one that
    /// needs no signed-in user: the tenant factory asks Blazor for the user and throws outside a component scope.</summary>
    public static async Task<int> PurgeAsync(LosDbContext db, DateTime now)
    {
        var cutoff = now - DeviceKeep;
        return await db.PartnerDeviceCodes.Where(d => d.ExpiresAt < cutoff).ExecuteDeleteAsync();
    }
}

/// <summary>
/// Wrong codes typed at /link, counted per person and per network address: 5 in 15 minutes, then a
/// 15-minute pause. Singleton.
/// </summary>
/// <remarks>
/// ponytail: in memory (one server); a restart clears the counts. Move to the database if this ever runs on
/// several servers. At most <c>maxKeys</c> keys are remembered, oldest dropped first, so a flood of made-up
/// addresses cannot grow memory without limit.
/// </remarks>
public sealed class PartnerGuessLimiter(int max = 5, int maxKeys = 10_000)
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private readonly Dictionary<string, List<DateTime>> _misses = [];
    private readonly object _gate = new();

    private List<DateTime> Recent(string key, DateTime now) =>
        _misses.TryGetValue(key, out var times) ? times.Where(t => now - t < Window).ToList() : [];

    public bool Blocked(string[] keys, DateTime now)
    {
        lock (_gate)
        {
            return keys.Any(k => Recent(k, now).Count >= max);
        }
    }

    public void Miss(string[] keys, DateTime now)
    {
        lock (_gate)
        {
            foreach (var key in keys)
            {
                var times = Recent(key, now);
                times.Add(now);
                _misses.Remove(key); // re-insert so the newest activity sits last
                if (_misses.Count >= maxKeys)
                {
                    _misses.Remove(_misses.Keys.First());
                }

                _misses[key] = times;
            }
        }
    }

    public int Left(string[] keys, DateTime now)
    {
        lock (_gate)
        {
            return Math.Max(0, max - keys.Max(k => Recent(k, now).Count));
        }
    }
}
