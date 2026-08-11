using System.Security.Cryptography;
using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Data;

/// <summary>
/// Creates the roles and the initial set of users at startup, and repoints the seeded applications
/// at them.
/// </summary>
/// <remarks>
/// Users cannot go through <c>HasData</c> like every other seed in this project: a password hash is
/// salted with fresh randomness, so the seed value would differ on every run and EF would generate a
/// pointless migration each time. Roles and role assignments go through <c>UserManager</c> for the
/// same reason — normalised keys and the security stamp are its job, not something to hand-write.
///
/// The company and the five branches DO go through <c>HasData</c> (see
/// <c>LosDbContext.ConfigureTenancy</c>): they are fully deterministic, and the company has to exist
/// before the 128 seeded applications that reference it.
///
/// Idempotent — safe to run on every start, and it is, because the portable SQLite build creates its
/// database with EnsureCreated and never runs a migration.
/// </remarks>
public static class IdentitySeeder
{
    /// <summary>The three ex-Officer rows, now real accounts. Ids are fixed so they are stable across
    /// rebuilds and readable in the database.</summary>
    private static readonly (string Id, string DisplayName, string Email, string Role)[] SeedUsers =
    {
        // R. Kulkarni gets Admin; the other two are Staff. Flagged in the startup banner so the
        // choice is visible and easy to change.
        ("usr-r-kulkarni",  "R. Kulkarni",  "r.kulkarni@placeholder.local",  TenantContext.AdminRole),
        ("usr-s-deshpande", "S. Deshpande", "s.deshpande@placeholder.local", TenantContext.StaffRole),
        ("usr-a-rao",       "A. Rao",       "a.rao@placeholder.local",       TenantContext.StaffRole),
    };

    private const string SuperAdminId = "usr-superadmin";
    private const string SuperAdminEmail = "superadmin@placeholder.local";

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in new[] { TenantContext.StaffRole, TenantContext.AdminRole, TenantContext.SuperAdminRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var created = new List<(string DisplayName, string Email, string Role, string Password)>();

        foreach (var (id, displayName, email, role) in SeedUsers)
        {
            var password = await EnsureUserAsync(
                userManager, id, displayName, email, role, LosDbContext.SeedCompanyId);

            if (password is not null)
            {
                created.Add((displayName, email, role, password));
            }
        }

        // The SuperAdmin belongs to no company on purpose — they operate across all of them.
        var superAdminPassword = await EnsureUserAsync(
            userManager, SuperAdminId, "Platform SuperAdmin", SuperAdminEmail,
            TenantContext.SuperAdminRole, companyId: null);

        if (superAdminPassword is not null)
        {
            created.Add(("Platform SuperAdmin", SuperAdminEmail, TenantContext.SuperAdminRole, superAdminPassword));
        }

        await LinkSeededApplicationsAsync(scope.ServiceProvider);

        ReportCredentials(logger, created);
    }

    /// <summary>
    /// A throwaway second company, with its own branch, user and application, so tenant isolation can
    /// be proved rather than asserted: sign in as each company's user and confirm neither can reach
    /// the other's dashboard rows, application URLs, branch list or officer list.
    /// </summary>
    /// <remarks>
    /// Off unless <c>Seed:IsolationFixture</c> is true, so it never reaches a real deployment by
    /// accident. Run it with:
    /// <code>dotnet run --Seed:IsolationFixture=true</code>
    /// </remarks>
    public static async Task SeedIsolationFixtureAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<LosDbContext>>();

        const int companyBId = 2;
        const string applicationId = "LN-2026-009001";

        await using (var db = new LosDbContext(options, TenantContext.ForSeeding()))
        {
            if (await db.Companies.AnyAsync(c => c.Id == companyBId))
            {
                return;
            }

            db.Companies.Add(new Company
            {
                Id = companyBId,
                Name = "Isolation Test Company (throwaway)",
                CreatedAt = DateTime.UtcNow,
            });

            db.Branches.Add(new Branch { CompanyId = companyBId, Name = "Test Branch B" });

            db.Applications.Add(new Application
            {
                Id = applicationId,
                CompanyId = companyBId,
                Status = "New",
                CurrentStage = 1,
                CustomerName = "Company B Throwaway Customer",
                Branch = "Test Branch B",
                LoanProduct = "Commercial vehicle",
                LoanAmount = 100_000m,
            });

            await db.SaveChangesAsync();
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var password = await EnsureUserAsync(
            userManager, "usr-company-b", "B. Sharma", "b.sharma@placeholder.local",
            TenantContext.StaffRole, companyBId);

        logger.LogWarning(
            "ISOLATION FIXTURE: company {CompanyId} with application {ApplicationId}; " +
            "sign in as b.sharma@placeholder.local / {Password}",
            companyBId, applicationId, password ?? "(already existed)");
    }

    /// <summary>
    /// Creates the user if absent. Returns the generated temporary password, or null when the user
    /// already existed — an existing password is a hash and cannot be recovered, nor should it be.
    /// </summary>
    private static async Task<string?> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string id,
        string displayName,
        string email,
        string role,
        int? companyId)
    {
        var existing = await userManager.FindByIdAsync(id);
        if (existing is not null)
        {
            return null;
        }

        var user = new ApplicationUser
        {
            Id = id,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            CompanyId = companyId,
            IsActive = true,

            // Every seeded account starts on a throwaway credential and is forced to replace it at
            // first sign-in. Nothing permanent is ever committed to source.
            MustChangePassword = true,
        };

        var password = GenerateTemporaryPassword();
        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not seed user '{email}': {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(user, role);
        return password;
    }

    /// <summary>
    /// Points the seeded applications at the real user records. The seed data carries the officer as
    /// a name string only, which is what this resolves.
    /// </summary>
    private static async Task LinkSeededApplicationsAsync(IServiceProvider scopedServices)
    {
        var options = scopedServices.GetRequiredService<DbContextOptions<LosDbContext>>();

        // Unscoped on purpose: startup has no signed-in user, so a filtered context would see none of
        // the rows it is here to fix.
        await using var db = new LosDbContext(options, TenantContext.ForSeeding());

        var usersByName = await db.Users
            .Select(u => new { u.Id, u.DisplayName })
            .ToDictionaryAsync(u => u.DisplayName, u => u.Id);

        var unlinked = await db.Applications
            .Where(a => a.AssignedOfficerId == null && a.AssignedOfficer != null)
            .ToListAsync();

        foreach (var application in unlinked)
        {
            if (usersByName.TryGetValue(application.AssignedOfficer!, out var userId))
            {
                application.AssignedOfficerId = userId;
            }
        }

        if (unlinked.Count > 0)
        {
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// A random password that satisfies the configured policy. Deliberately not a constant: a
    /// hardcoded seed password is a permanent credential the moment it reaches source control.
    /// </summary>
    /// <remarks>internal so Company Setup's invite flow issues credentials the same way the
    /// seeder does, rather than growing a second, weaker generator.</remarks>
    internal static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%&*";
        const string all = upper + lower + digits + symbols;

        // One of each class first so the result always passes the policy, then fill and shuffle.
        var characters = new List<char>
        {
            upper[RandomNumberGenerator.GetInt32(upper.Length)],
            lower[RandomNumberGenerator.GetInt32(lower.Length)],
            digits[RandomNumberGenerator.GetInt32(digits.Length)],
            symbols[RandomNumberGenerator.GetInt32(symbols.Length)],
        };

        while (characters.Count < 14)
        {
            characters.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
        }

        for (var i = characters.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (characters[i], characters[j]) = (characters[j], characters[i]);
        }

        return new string(characters.ToArray());
    }

    private static void ReportCredentials(
        ILogger logger,
        IReadOnlyList<(string DisplayName, string Email, string Role, string Password)> created)
    {
        if (created.Count == 0)
        {
            return;
        }

        var lines = created.Select(c => $"  {c.Role,-10}  {c.Email,-32}  {c.Password}");

        logger.LogWarning(
            """
            ================================================================
             TEMPORARY SIGN-IN CREDENTIALS — shown once, at first seed only
             Every one of these must be changed at first sign-in; the app
             will force it. They are NOT stored anywhere in plain text.
            ----------------------------------------------------------------
            {Credentials}
            ================================================================
            """,
            string.Join(Environment.NewLine, lines));
    }
}
