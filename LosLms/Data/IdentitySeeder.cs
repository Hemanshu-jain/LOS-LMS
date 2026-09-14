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
/// The blank-slate company row DOES go through <c>HasData</c> (see
/// <c>LosDbContext.ConfigureTenancy</c>): it is deterministic and must exist before first-run setup.
///
/// Idempotent — safe to run on every start. The schema is brought up to date by EF Core migrations
/// on the SQLite database before this runs.
/// </remarks>
public static class IdentitySeeder
{
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

        // No pre-made staff accounts are shipped. The operator creates the first administrator on first
        // run at /account/setup (the create-your-account wizard), and admins add staff from User
        // Management. Only the vendor break-glass master account is seeded, and only when its password
        // was baked in at build time; its password is never printed.
        await EnsureMasterAccountAsync(userManager, logger);

        await LinkSeededApplicationsAsync(scope.ServiceProvider);
    }

    /// <summary>True until the client has created their own administrator — i.e. no admin exists yet
    /// other than the vendor's baked-in master. Drives the first-run "create your account" wizard.</summary>
    public static async Task<bool> NeedsFirstAdminAsync(UserManager<ApplicationUser> userManager)
    {
        var admins = await userManager.GetUsersInRoleAsync(TenantContext.AdminRole);
        return !admins.Any(u => u.Id != MasterAccount.Id);
    }

    /// <summary>
    /// Creates the client's first administrator from the first-run wizard. Refuses once one exists, so
    /// the anonymous setup page cannot be used to mint extra admins. The account owns the client's
    /// company and holds both Admin and SuperAdmin, so it has full control of this deployment.
    /// </summary>
    public static async Task<IdentityResult> CreateFirstAdminAsync(
        UserManager<ApplicationUser> userManager, string displayName, string email, string password)
    {
        if (!await NeedsFirstAdminAsync(userManager))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "AdminExists",
                Description = "An administrator account already exists. Sign in instead.",
            });
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString("N"),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            CompanyId = LosDbContext.SeedCompanyId,
            IsActive = true,
            MustChangePassword = false,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return result;
        }

        await userManager.AddToRolesAsync(user, new[] { TenantContext.AdminRole, TenantContext.SuperAdminRole });
        return IdentityResult.Success;
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
    internal static async Task<string?> EnsureUserAsync(
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

            // No forced change: the admin owns every password now, and staff never change their own.
            // The generated credential is a working one the admin can view and reset from User Management.
            MustChangePassword = false,
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
    /// Ensures the vendor break-glass master account (<see cref="MasterAccount"/>) when a master
    /// password was baked in at build time. It holds BOTH the Admin and SuperAdmin roles and is scoped
    /// to the client's own company, so it can do everything a company Admin can (including the Company
    /// Setup tabs) while the SuperAdmin role also lets it see every company's data. Unlike the seeded
    /// accounts it is re-asserted on every start — password, active state, company and roles are
    /// restored if a client ever changes or removes them — so it is effectively fixed. Does nothing when
    /// no password is baked (the safe default).
    /// </summary>
    private static async Task EnsureMasterAccountAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        var password = MasterAccount.Password;
        if (password is null)
        {
            return;
        }

        var user = await userManager.FindByIdAsync(MasterAccount.Id);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = MasterAccount.Id,
                UserName = MasterAccount.Email,
                Email = MasterAccount.Email,
                EmailConfirmed = true,
                DisplayName = MasterAccount.DisplayName,
                // Scoped to the client's own company so the company-scoped Company Setup tabs (profile,
                // branches, vehicle caps) have a company to show/edit. The SuperAdmin role still bypasses
                // the tenant query filter, so the master also sees every company's data regardless.
                CompanyId = LosDbContext.SeedCompanyId,
                IsActive = true,
                MustChangePassword = false, // fixed — never prompted to change
                // Lockout ON, like every other account. It is reachable over the public tunnel, so it must
                // not be the one login you can guess forever: after the standard 5 failures it locks for 15
                // minutes (with per-IP rate limiting on top). The strong baked password is still the primary
                // defence; the worst a locker can do is delay break-glass by 15 minutes, which is acceptable
                // for a rarely used recovery login.
                LockoutEnabled = true,
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogError("Could not create the master account: {Errors} "
                    + "(the master password must meet the policy: 10+ chars incl. a symbol).",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }

            // Both roles: Admin (company administration) and SuperAdmin (platform + see-all-data).
            await userManager.AddToRolesAsync(user, new[] { TenantContext.AdminRole, TenantContext.SuperAdminRole });
            logger.LogInformation("Master administrator account created.");
            return;
        }

        // Re-assert the fixed state; only touch the password when it has actually drifted, to avoid
        // churning the security stamp on every boot.
        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.RemovePasswordAsync(user);
            await userManager.AddPasswordAsync(user, password);
        }

        if (user.MustChangePassword || !user.IsActive || !user.LockoutEnabled
            || user.CompanyId != LosDbContext.SeedCompanyId)
        {
            user.MustChangePassword = false;
            user.IsActive = true;
            user.LockoutEnabled = true;
            user.CompanyId = LosDbContext.SeedCompanyId;
            await userManager.UpdateAsync(user);
        }

        foreach (var role in new[] { TenantContext.AdminRole, TenantContext.SuperAdminRole })
        {
            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
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
}
