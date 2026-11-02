using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Data;

/// <summary>
/// Seeds one ready-to-use demo tenant on the public web instance: a company with a known admin login, a
/// branch and a small vehicle-cap catalog, so a visitor can sign in and land straight in a working —
/// but empty — dashboard, no first-run setup in the way. Carries no loan applications; it is a clean
/// tenant to look around, not a populated data set (that is <see cref="DemoSeedData"/>).
/// </summary>
/// <remarks>
/// Off unless <c>Seed:DemoTenant</c> is true (set only in the web build's config), and idempotent — it
/// does nothing once the demo admin exists. The credentials come from config with sensible defaults and
/// are logged at startup, because this login is meant to be handed out for the demo, not kept secret.
///
/// It also closes the single-tenant first-run wizard on the public instance: once this admin exists,
/// <see cref="IdentitySeeder.NeedsFirstAdminAsync"/> is false, so nobody can use <c>/account/setup</c> to
/// mint a SuperAdmin on the seeded company.
/// </remarks>
public static class DemoTenantSeeder
{
    private const string DefaultEmail = "demo@los-lms.bhodhix.com";
    private const string DefaultPassword = "Demo@Los2026";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var email = config.GetValue("Seed:DemoTenantEmail", DefaultEmail)!;
        var password = config.GetValue("Seed:DemoTenantPassword", DefaultPassword)!;

        // Idempotent: the demo tenant already exists, nothing to do.
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var provisioning = scope.ServiceProvider.GetRequiredService<CompanyProvisioningService>();
        var result = await provisioning.ProvisionCompanyAsync(
            new CompanyProfile(
                Name: "Demo Finance Co.",
                ContactEmail: email,
                ContactPhone: "9000000000",
                Address: "Demo Branch, Nashik, Maharashtra"),
            new AdminAccount(DisplayName: "Demo Admin", Email: email, Password: password));

        if (!result.Succeeded)
        {
            logger.LogError("Demo tenant seeding failed: {Errors}", string.Join("; ", result.Errors));
            return;
        }

        // A branch (completes first-run setup so the admin lands in the app, not the setup gate) and a
        // short vehicle-cap catalog (so Loan & Security's vehicle dropdown works and a loan can be filed).
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<LosDbContext>>();
        await using (var db = new LosDbContext(options, TenantContext.ForSeeding()))
        {
            db.Branches.Add(new Branch { CompanyId = result.CompanyId, Name = "Head Office — Nashik" });

            foreach (var (make, model, year, cap) in StarterVehicleCatalog)
            {
                db.VehicleLoanCaps.Add(new VehicleLoanCap
                {
                    CompanyId = result.CompanyId,
                    Make = make,
                    Model = model,
                    Year = year,
                    MaxLoanAmount = cap,
                });
            }

            await db.SaveChangesAsync();
        }

        logger.LogInformation(
            "Demo tenant ready (company {CompanyId}). Sign in at /account/login with {Email} / {Password}.",
            result.CompanyId, email, password);
    }

    /// <summary>A handful of vehicles so the demo tenant's Loan &amp; Security dropdowns are populated.</summary>
    private static readonly (string Make, string Model, int Year, decimal Cap)[] StarterVehicleCatalog =
    {
        ("Tata", "Signa 2823.K tipper", 2026, 2_650_000m),
        ("Tata", "Ace Gold", 2025, 1_800_000m),
        ("Ashok Leyland", "Dost+", 2025, 2_000_000m),
        ("Mahindra", "Bolero Pik-Up", 2026, 1_600_000m),
    };
}
