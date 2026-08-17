using LosLms.Data;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// Whether a company's first-run setup is done, and the one place that decides it.
/// </summary>
/// <remarks>
/// "Done" means the two things a company cannot meaningfully operate without: a real
/// <see cref="Models.Company.Name"/> and at least one <see cref="Models.Branch"/>. Until then every
/// company-scoped user is redirected to Company Setup (see the middleware in Program.cs and the
/// backstop in MainLayout) and can reach nothing else.
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

        var hasName = !string.IsNullOrWhiteSpace(company.Name);
        var hasBranch = await db.Branches.IgnoreQueryFilters().AnyAsync(b => b.CompanyId == companyId);

        if (!hasName || !hasBranch)
        {
            return false;
        }

        company.SetupCompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }
}
