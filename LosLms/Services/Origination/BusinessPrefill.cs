using LosLms.Models;

namespace LosLms.Services;

/// <summary>
/// The Approvals "Business details" are the BORROWER's business: firm name from the applicant's employer / business name, constitution
/// from the customer category. Never the lender's own Company profile — an earlier version copied that in, so a row still holding the
/// lender's details is cleared first. Blanks only, so an officer's entry is never overwritten.
/// </summary>
public static class BusinessPrefill
{
    private static readonly string[] Constitutions = { "Proprietorship", "Partnership firm", "Private limited company", "HUF" };

    public static void Apply(Business business, Party? applicant, Company? lender)
    {
        if (lender is not null && Same(business.FirmName, lender.Name))
        {
            // The old lender-profile fill: drop everything that came from it.
            business.FirmName = string.Empty;
            ClearIfLenders(() => Same(business.Constitution, lender.Constitution), () => business.Constitution = string.Empty);
            ClearIfLenders(() => Same(business.Gstin, lender.Gstin), () => business.Gstin = string.Empty);
            ClearIfLenders(() => Same(business.Vintage, lender.Vintage), () => business.Vintage = string.Empty);
            if (business.IncorpDate is not null && business.IncorpDate == lender.IncorpDate)
            {
                business.IncorpDate = null;
            }
        }

        if (applicant is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(business.FirmName) && !string.IsNullOrWhiteSpace(applicant.EmployerName))
        {
            business.FirmName = applicant.EmployerName.Trim();
        }

        // An individual borrower's business is a proprietorship.
        var category = string.Equals(applicant.CustomerCategory?.Trim(), "Individual", StringComparison.OrdinalIgnoreCase) ? "Proprietorship" : applicant.CustomerCategory?.Trim();
        if (string.IsNullOrWhiteSpace(business.Constitution)
            && Constitutions.FirstOrDefault(c => string.Equals(c, category, StringComparison.OrdinalIgnoreCase)) is { } constitution)
        {
            business.Constitution = constitution;
        }
    }

    /// <summary>An individual or proprietor has no incorporation date or (necessarily) a GSTIN, so Approvals does not insist on them.</summary>
    public static bool IsSoleProprietorship(string? constitution) =>
        string.Equals(constitution?.Trim(), "Proprietorship", StringComparison.OrdinalIgnoreCase)
        || string.Equals(constitution?.Trim(), "Individual", StringComparison.OrdinalIgnoreCase);

    private static void ClearIfLenders(Func<bool> isLenders, Action clear)
    {
        if (isLenders())
        {
            clear();
        }
    }

    private static bool Same(string? a, string? b) =>
        !string.IsNullOrWhiteSpace(a) && string.Equals(a.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
}
