using System.Security.Claims;
using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LosLms.Tests;

/// <summary>
/// The hourly device-code purge runs in a plain background scope, where Blazor's auth state provider throws
/// ("Do not call GetAuthenticationStateAsync outside of the DI scope for a Razor component"). It used to go
/// through the tenant-aware factory and so never deleted anything. This wires the REAL provider and factory
/// so the old path fails here, and checks the background service still deletes the expired code.
/// </summary>
public sealed class PartnerPurgeServiceTests : IDisposable
{
    private static readonly DateTime Now = DateTime.UtcNow; // the hosted service reads the real clock

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _services;

    public PartnerPurgeServiceTests()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite(_connection).Options;

        using (var db = VendorContext(options))
        {
            db.Database.EnsureCreated();
            db.PartnerClients.Add(new PartnerClient { Id = "lc_x", Name = "Paavan", Scopes = "loans:read", CreatedAt = Now });
            db.PartnerDeviceCodes.AddRange(
                Code("expired", Now.AddHours(-2)),   // expired 2 h ago: past the 1 h keep window
                Code("recent", Now.AddMinutes(-10)), // expired 10 min ago: late polls must still hear expired_token
                Code("live", Now.AddMinutes(10)));
            db.SaveChanges();
        }

        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(options)
            .AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>()
            .AddScoped<IHttpContextAccessor, HttpContextAccessor>()
            .AddSingleton<ActingCompanyStore>()
            .AddScoped<TenantContext>()
            .AddScoped<IDbContextFactory<LosDbContext>, TenantDbContextFactory>()
            .AddScoped<PartnerOAuthService>()
            .BuildServiceProvider();
    }

    [Fact]
    public async Task BackgroundPurge_DeletesTheExpiredCode_WithoutACircuit()
    {
        // The real hosted service, started the way the host starts it.
        var purge = new PartnerOAuthPurgeService(
            _services.GetRequiredService<IServiceScopeFactory>(), _services.GetRequiredService<ILogger<PartnerOAuthPurgeService>>());
        await purge.StartAsync(CancellationToken.None);
        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (Remaining().Contains("expired") && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
            }
        }
        finally
        {
            await purge.StopAsync(CancellationToken.None);
        }

        var left = Remaining();
        Assert.DoesNotContain("expired", left);
        Assert.Contains("recent", left); // inside the 1 h keep window: late polls still hear expired_token
        Assert.Contains("live", left);
    }

    private List<string> Remaining()
    {
        using var db = VendorContext(_services.GetRequiredService<DbContextOptions<LosDbContext>>());
        return db.PartnerDeviceCodes.Select(d => d.DeviceHash).ToList();
    }

    // A SuperAdmin-scoped context (sees every company), built the way the other tests do.
    private static LosDbContext VendorContext(DbContextOptions<LosDbContext> options)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "super"), new Claim(ClaimTypes.Role, TenantContext.SuperAdminRole) }, "TestAuth"));
        var tenant = new TenantContext(new FixedAuth(principal), new HttpContextAccessor(), new ActingCompanyStore());
        tenant.EnsureLoadedAsync().GetAwaiter().GetResult();
        return new LosDbContext(options, tenant);
    }

    private sealed class FixedAuth(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
    }

    private static PartnerDeviceCode Code(string name, DateTime expiresAt) => new()
    {
        DeviceHash = name, UserCodeHash = name + "-user", ClientId = "lc_x", Scopes = "loans:read",
        PollInterval = 5, CreatedAt = expiresAt.AddMinutes(-15), ExpiresAt = expiresAt,
    };

    public void Dispose()
    {
        _services.Dispose();
        _connection.Dispose();
    }
}
