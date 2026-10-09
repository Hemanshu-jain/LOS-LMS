using System.Data.Common;
using System.Security.Claims;
using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LosLms.Tests;

/// <summary>
/// The sign-in page's "does this system still need its first admin?" check must not share a context with
/// the request's Identity stores: two operations on one DbContext throw "A second operation was started on
/// this context instance" (the transient 500 on /account/login). A query is held in flight on one context
/// with an interceptor, which makes the collision deterministic.
/// </summary>
public sealed class FirstAdminCheckTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private const string MasterId = "usr-master"; // MasterAccount.Id (internal)
    private readonly HoldFirstQuery _hold = new();

    public FirstAdminCheckTests()
    {
        _connection.Open();
        using var db = NewContext(withHold: false);
        db.Database.EnsureCreated();
        db.Roles.Add(new IdentityRole { Id = "r-admin", Name = TenantContext.AdminRole, NormalizedName = "ADMIN" });
        db.SaveChanges();
    }

    [Fact]
    public async Task SharingOneContext_Collides_ButTheFactoryCheckDoesNot()
    {
        await using var shared = NewContext(withHold: true);

        // Something else on the request is mid-query on the shared (Identity) context...
        var inFlight = shared.Users.AnyAsync();
        await _hold.Started.Task;

        // ...so a second operation on that same instance is exactly the reported failure,
        await Assert.ThrowsAsync<InvalidOperationException>(() => shared.Roles.AnyAsync());

        // ...while the check on its own short-lived context goes straight through.
        var needs = await IdentitySeeder.NeedsFirstAdminAsync(new TestFactory(this)).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(needs); // no admin other than the vendor master yet

        _hold.Release.SetResult();
        await inFlight;
    }

    [Fact]
    public async Task NeedsFirstAdmin_FlipsOnceARealAdminExists_AndIgnoresTheMaster()
    {
        var factory = new TestFactory(this);
        await using (var db = NewContext(withHold: false))
        {
            db.Users.AddRange(User(MasterId), User("real-admin"));
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = MasterId, RoleId = "r-admin" });
            db.SaveChanges();
        }

        Assert.True(await IdentitySeeder.NeedsFirstAdminAsync(factory)); // only the master holds Admin

        await using (var db = NewContext(withHold: false))
        {
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = "real-admin", RoleId = "r-admin" });
            db.SaveChanges();
        }

        Assert.False(await IdentitySeeder.NeedsFirstAdminAsync(factory));
    }

    private static ApplicationUser User(string id) => new()
    {
        Id = id, UserName = id, NormalizedUserName = id.ToUpperInvariant(), Email = $"{id}@x.test",
        DisplayName = id, CompanyId = LosDbContext.SeedCompanyId, IsActive = true,
    };

    private LosDbContext NewContext(bool withHold)
    {
        var options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite(_connection);
        if (withHold)
        {
            options.AddInterceptors(_hold);
        }

        // Anonymous, like the sign-in page: no user, nothing resolved.
        var tenant = new TenantContext(new Anonymous(), new HttpContextAccessor(), new ActingCompanyStore());
        tenant.EnsureLoadedAsync().GetAwaiter().GetResult();
        return new LosDbContext(options.Options, tenant);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class TestFactory(FirstAdminCheckTests owner) : IDbContextFactory<LosDbContext>
    {
        public LosDbContext CreateDbContext() => owner.NewContext(withHold: false);
    }

    private sealed class Anonymous : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    /// <summary>Parks the first command it sees until released, so a query stays "in flight" on demand.</summary>
    private sealed class HoldFirstQuery : DbCommandInterceptor
    {
        private int _seen;
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _seen) == 1)
            {
                Started.SetResult();
                await Release.Task;
            }

            return result;
        }
    }
}
