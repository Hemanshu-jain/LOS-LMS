using LosLms.Data;
using LosLms.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>The company profile captured when a new tenant signs up.</summary>
public sealed record CompanyProfile(string Name, string ContactEmail, string ContactPhone, string Address);

/// <summary>The owning administrator captured when a new tenant signs up.</summary>
public sealed record AdminAccount(string DisplayName, string Email, string Password);

/// <summary>The outcome of a provisioning attempt — the created admin and company id, or the errors.</summary>
public sealed class CompanyProvisioningResult
{
    private CompanyProvisioningResult(bool succeeded, ApplicationUser? user, int companyId, IReadOnlyList<string> errors)
    {
        Succeeded = succeeded;
        User = user;
        CompanyId = companyId;
        Errors = errors;
    }

    public bool Succeeded { get; }
    public ApplicationUser? User { get; }
    public int CompanyId { get; }
    public IReadOnlyList<string> Errors { get; }

    public static CompanyProvisioningResult Success(ApplicationUser user, int companyId) =>
        new(true, user, companyId, Array.Empty<string>());

    public static CompanyProvisioningResult Failure(params string[] errors) =>
        new(false, null, 0, errors);
}

/// <summary>
/// Creates a brand-new tenant — a <see cref="Company"/> plus its owning administrator — in one shot, so
/// the public sign-up page and the demo-tenant seeder both provision a company the same way.
/// </summary>
/// <remarks>
/// This is the multi-tenant counterpart to <see cref="IdentitySeeder.CreateFirstAdminAsync"/>, which is
/// pinned to the single seeded company and refuses once one admin exists. Here every call makes a fresh
/// company with its own id, and the admin gets the <see cref="TenantContext.AdminRole"/> only — never
/// <see cref="TenantContext.SuperAdminRole"/>, which bypasses the tenant filter and would see every
/// company's data.
///
/// The company row and its default dropdown options are written through an unscoped
/// <see cref="TenantContext.ForSeeding"/> context (the same escape hatch <see cref="IdentitySeeder"/>
/// uses), so an explicit <c>CompanyId</c> is honoured rather than stamped over. The admin is created
/// through <see cref="UserManager{T}"/> so the password is hashed and the security stamp is set the same
/// way as every other account.
/// </remarks>
public sealed class CompanyProvisioningService(
    DbContextOptions<LosDbContext> dbOptions,
    UserManager<ApplicationUser> userManager,
    PasswordVault vault,
    ILogger<CompanyProvisioningService> logger)
{
    public async Task<CompanyProvisioningResult> ProvisionCompanyAsync(CompanyProfile profile, AdminAccount admin)
    {
        var email = admin.Email.Trim();

        // Global uniqueness check up front, so a duplicate email is rejected before any company row is
        // written and cannot leave an orphan tenant behind. The ApplicationUser query filter allows an
        // anonymous/unscoped context to see every user, so this is a true system-wide check.
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return CompanyProvisioningResult.Failure(
                "That email is already registered. Sign in instead, or use a different email.");
        }

        // Create the company and seed its built-in dropdown options in one unscoped context.
        int companyId;
        await using (var db = new LosDbContext(dbOptions, TenantContext.ForSeeding()))
        {
            var company = new Company
            {
                Name = profile.Name.Trim(),
                ContactEmail = profile.ContactEmail.Trim(),
                ContactPhone = profile.ContactPhone.Trim(),
                Address = profile.Address.Trim(),
            };
            db.Companies.Add(company);
            await db.SaveChangesAsync();
            companyId = company.Id;

            // Sourcing/scheme/RCU-vendor defaults, so the new company's dropdowns are never empty.
            await LookupSeeder.EnsureDefaultsAsync(db, companyId);
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString("N"),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = admin.DisplayName.Trim(),
            CompanyId = companyId,
            IsActive = true,
            MustChangePassword = false,
        };

        var result = await userManager.CreateAsync(user, admin.Password);
        if (!result.Succeeded)
        {
            // The admin could not be created (e.g. the password failed the policy). Remove the company we
            // just made so a failed sign-up never leaves a tenant nobody can sign in to.
            await RollbackCompanyAsync(companyId);
            return CompanyProvisioningResult.Failure(result.Errors.Select(e => e.Description).ToArray());
        }

        await userManager.AddToRoleAsync(user, TenantContext.AdminRole);

        // Keep the admin's own password viewable in User Management, matching the setup and invite flows.
        vault.Store(user.Id, admin.Password);

        logger.LogInformation("Provisioned company {CompanyId} with owning admin {Email}.", companyId, email);
        return CompanyProvisioningResult.Success(user, companyId);
    }

    /// <summary>Removes a just-created company and its seeded lookups when admin creation fails.</summary>
    private async Task RollbackCompanyAsync(int companyId)
    {
        await using var db = new LosDbContext(dbOptions, TenantContext.ForSeeding());

        var lookups = await db.LookupValues.Where(l => l.CompanyId == companyId).ToListAsync();
        db.LookupValues.RemoveRange(lookups);

        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
        if (company is not null)
        {
            db.Companies.Remove(company);
        }

        await db.SaveChangesAsync();
    }
}
