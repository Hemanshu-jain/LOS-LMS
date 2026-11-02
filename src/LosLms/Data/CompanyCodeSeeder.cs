using LosLms.Services;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Data;

/// <summary>
/// Gives every company a unique <see cref="Models.Company.Code"/> the first time it is missing one, so
/// existing companies (created before the code column existed) get one too — not just companies made
/// through the signup flow. Idempotent: a company that already has a code is never changed.
/// </summary>
/// <remarks>
/// Runs at startup with an unscoped seeding context so it can read and write across every company. The
/// unique index on Code is the hard guarantee; this generates against the codes already present so a
/// clash is avoided in the first place.
/// </remarks>
public static class CompanyCodeSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<LosDbContext>>();
        await using var db = new LosDbContext(options, TenantContext.ForSeeding());

        var companies = await db.Companies.OrderBy(c => c.Id).ToListAsync();

        var taken = companies
            .Where(c => !string.IsNullOrWhiteSpace(c.Code))
            .Select(c => c.Code!.ToUpperInvariant())
            .ToHashSet();

        var changed = false;
        foreach (var company in companies.Where(c => string.IsNullOrWhiteSpace(c.Code)))
        {
            var code = CompanyCode.Generate(company.Name, taken);
            company.Code = code;
            taken.Add(code);
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }
    }
}
