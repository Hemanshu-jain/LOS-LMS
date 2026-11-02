using LosLms.Data;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// Whether a company's first-run setup is done, and the one place that decides it.
/// </summary>
/// <remarks>
/// "Done" means a complete company profile — <see cref="Models.Company.Name"/>,
/// <see cref="Models.Company.ContactEmail"/>, <see cref="Models.Company.ContactPhone"/> and
/// <see cref="Models.Company.Address"/> — plus at least one <see cref="Models.Branch"/>. The profile
/// fields are what lets each company be told apart at the vendor's back end, so onboarding captures
/// them up front. Until setup is done every company-scoped user is redirected to Company Setup (see the
/// middleware in Program.cs and the backstop in MainLayout) and can reach nothing else.
///
/// <see cref="Models.Company.SetupCompletedAt"/> is stamped lazily the first time both conditions
/// hold — from whichever call notices first — so completion persists without every save path having to
/// remember to set it. Once stamped it is treated as complete and never re-evaluated or cleared:
/// renaming the company or removing a branch afterwards is the client's own business, not a reason to
/// lock them back out.
/// </remarks>
public static class CompanySetupState
{
    /// <summary>
    /// True when the company's setup is complete, stamping <c>SetupCompletedAt</c> if it has just
    /// become so. Query filters are ignored and the company id is matched explicitly, so this is
    /// correct whether the caller's context is company-scoped, SuperAdmin, or unscoped.
    /// </summary>
    public static async Task<bool> IsCompleteAsync(LosDbContext db, int companyId)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
        if (company is null)
        {
            return false;
        }

        if (company.SetupCompletedAt is not null)
        {
            return true;
        }

        var hasProfile = !string.IsNullOrWhiteSpace(company.Name)
            && !string.IsNullOrWhiteSpace(company.ContactEmail)
            && !string.IsNullOrWhiteSpace(company.ContactPhone)
            && !string.IsNullOrWhiteSpace(company.Address);
        var hasBranch = await db.Branches.IgnoreQueryFilters().AnyAsync(b => b.CompanyId == companyId);

        if (!hasProfile || !hasBranch)
        {
            return false;
        }

        company.SetupCompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }
}
