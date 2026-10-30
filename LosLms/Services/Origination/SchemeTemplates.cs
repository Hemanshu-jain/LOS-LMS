using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>A scheme's default charges for ONE company. Two companies can have a scheme of the same name; only the application's company's template is used.</summary>
public static class SchemeTemplates
{
    public static async Task<List<SchemeChargeTemplate>> ForAsync(LosDbContext db, int companyId, string? scheme) =>
        string.IsNullOrWhiteSpace(scheme)
            ? []
            : await db.SchemeChargeTemplates.AsNoTracking().Where(t => t.Scheme == scheme && t.CompanyId == companyId).OrderBy(t => t.Id).ToListAsync();
}
