using LosLms.Data;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// Hands out short-lived <see cref="LosDbContext"/> instances that already know which company the
/// signed-in user belongs to.
/// </summary>
/// <remarks>
/// This replaces the <see cref="IDbContextFactory{TContext}"/> that <c>AddDbContextFactory</c>
/// registers. That one is a singleton and therefore cannot see scoped services such as
/// <see cref="TenantContext"/>; registering this one afterwards as scoped wins the resolution, so
/// every existing <c>await DbFactory.CreateDbContextAsync()</c> call site keeps working untouched.
///
/// The async overload is the one every page uses, and it is where the claims get resolved — which is
/// why there is no sync-over-async anywhere in the tenant path.
/// </remarks>
public sealed class TenantDbContextFactory(
    DbContextOptions<LosDbContext> options,
    TenantContext tenant) : IDbContextFactory<LosDbContext>
{
    public async Task<LosDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        await tenant.EnsureLoadedAsync();
        return new LosDbContext(options, tenant);
    }

    /// <summary>
    /// Synchronous fallback. It cannot await the claims, so if they have not been resolved yet the
    /// context is built from an empty tenant and sees nothing — failing closed rather than leaking.
    /// Identity's own stores resolve <c>LosDbContext</c> through this path.
    /// </summary>
    public LosDbContext CreateDbContext() => new(options, tenant);
}
