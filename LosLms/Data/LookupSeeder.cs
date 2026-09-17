using LosLms.Models;
using LosLms.Services;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Data;

/// <summary>
/// Gives every company the built-in dropdown options (<see cref="LookupKind.Defaults"/>) the first
/// time it has none of a kind, so the DSA/sourcing, scheme and RCU-vendor dropdowns are never empty
/// after the upgrade that moved them out of hardcoded arrays and into <see cref="LookupValue"/>.
/// </summary>
/// <remarks>
/// Idempotent and re-run on every start. Values are only added for a kind the company has zero of —
/// options are deactivated, never deleted, so once a company has been seeded its rows persist and this
/// never overwrites an admin's edits.
/// </remarks>
public static class LookupSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<LosDbContext>>();

        // Unscoped: startup has no signed-in user, so a company-filtered context would see no rows to seed.
        await using var db = new LosDbContext(options, TenantContext.ForSeeding());

        var companyIds = await db.Companies.Select(c => c.Id).ToListAsync();
        foreach (var companyId in companyIds)
        {
            await EnsureDefaultsAsync(db, companyId);
        }
    }

    /// <summary>Seeds the built-in options for any kind this company currently has none of.</summary>
    public static async Task EnsureDefaultsAsync(LosDbContext db, int companyId)
    {
        var seededKinds = await db.LookupValues
            .Where(l => l.CompanyId == companyId)
            .Select(l => l.Kind)
            .Distinct()
            .ToListAsync();

        var toAdd = new List<LookupValue>();
        foreach (var kind in LookupKind.All)
        {
            if (seededKinds.Contains(kind)) { continue; }
            if (!LookupKind.Defaults.TryGetValue(kind, out var names)) { continue; }

            toAdd.AddRange(names.Select(name => new LookupValue
            {
                CompanyId = companyId,
                Kind = kind,
                Name = name,
                IsActive = true,
            }));
        }

        if (toAdd.Count > 0)
        {
            db.LookupValues.AddRange(toAdd);
            await db.SaveChangesAsync();
        }
    }
}
