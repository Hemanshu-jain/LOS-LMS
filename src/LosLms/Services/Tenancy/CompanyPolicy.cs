using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// A company's underwriting policy numbers, read as one immutable snapshot.
/// </summary>
/// <remarks>
/// These were thirteen <c>const</c>s spread across six files, every one carrying a comment saying it
/// had been invented and needed confirming. Two of them silently disagreed with each other — the FOIR
/// and LTV "caution" bands both sat above the caps that refuse the loan, so a file could read amber
/// while the eligibility engine was already capping it. See <see cref="Company.FoirRiskCautionPct"/>.
///
/// Reading them from one place means a screen can no longer hold a private opinion about a threshold.
/// </remarks>
public sealed record CompanyPolicy(
    int SlaOverdueDays,
    decimal FoirCapPct,
    decimal LtvCapPct,
    decimal FoirRiskCautionPct,
    decimal FoirRiskDangerPct,
    decimal LtvRiskCautionPct,
    decimal LtvRiskDangerPct,
    decimal GstPct,
    int CibilMinScore,
    int CibilMaxScore,
    int AddressValidityDays,
    decimal NoteStaleTolerancePct,
    int MinimumReferences)
{
    /// <summary>GST as a multiplier, e.g. 0.18 for 18%.</summary>
    public decimal GstRate => GstPct / 100m;

    /// <summary>
    /// The values every constant held before Company Setup existed. Used only when a policy row
    /// genuinely cannot be resolved, so behaviour degrades to what it always was rather than to
    /// zeroes — a zero FOIR cap would compute an eligible amount of nothing and read as a real answer.
    /// </summary>
    public static CompanyPolicy Fallback { get; } = From(new Company());

    public static CompanyPolicy From(Company company) => new(
        company.SlaOverdueDays,
        company.FoirCapPct,
        company.LtvCapPct,
        company.FoirRiskCautionPct,
        company.FoirRiskDangerPct,
        company.LtvRiskCautionPct,
        company.LtvRiskDangerPct,
        company.GstPct,
        company.CibilMinScore,
        company.CibilMaxScore,
        company.AddressValidityDays,
        company.NoteStaleTolerancePct,
        company.MinimumReferences);
}

/// <summary>
/// Loads the policy that applies to a given application or to the signed-in user's own company.
/// </summary>
/// <remarks>
/// DELIBERATELY NOT CACHED. Every caller already opens a short-lived DbContext, so this adds one small
/// keyed read; caching it would mean an Admin editing a threshold in Company Setup sees the old number
/// still driving the eligibility maths until something evicted the entry, which is exactly the class of
/// bug this whole change exists to remove. If it ever shows up in a profile, cache it with an explicit
/// invalidation on save — not before.
/// </remarks>
public static class CompanyPolicyReader
{
    /// <summary>
    /// Policy of the company that owns the application — NOT the company of whoever is looking at it.
    /// A SuperAdmin opening another company's file has to see that company's thresholds, because those
    /// are the terms the file was underwritten on.
    /// </summary>
    public static async Task<CompanyPolicy> ForApplicationAsync(LosDbContext db, string applicationId)
    {
        var company = await db.Applications.AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => a.Company)
            .FirstOrDefaultAsync();

        return company is null ? CompanyPolicy.Fallback : CompanyPolicy.From(company);
    }

    /// <summary>
    /// Policy of the signed-in user's own company, for screens that are not scoped to one application
    /// — the dashboard's SLA colouring, and Company Setup itself.
    /// </summary>
    /// <remarks>
    /// A SuperAdmin belongs to no company, so they get the defaults. Nothing they can reach through
    /// this path makes a lending decision; the per-application path above is what the maths uses.
    /// </remarks>
    public static async Task<CompanyPolicy> ForCurrentCompanyAsync(LosDbContext db, TenantContext tenant)
    {
        if (tenant.CompanyId is not { } companyId)
        {
            return CompanyPolicy.Fallback;
        }

        var company = await db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId);

        return company is null ? CompanyPolicy.Fallback : CompanyPolicy.From(company);
    }
}
