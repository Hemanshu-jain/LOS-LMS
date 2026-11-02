using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>The migrated-loan upsert, by loan account number WITHIN one company: another company's loan with the same number is never touched.</summary>
public static class MigratedLoanImport
{
    public static async Task<int> UpsertAsync(LosDbContext db, int companyId, IReadOnlyList<MigratedLoanRow> rows, DateTime? nowUtc = null)
    {
        var existing = await db.MigratedLoans.Where(m => m.CompanyId == companyId).ToListAsync();
        foreach (var row in rows)
        {
            var match = existing.FirstOrDefault(m => string.Equals(m.LoanAccountNo, row.LoanAccountNo, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                match = new MigratedLoan { CompanyId = companyId, LoanAccountNo = row.LoanAccountNo };
                db.MigratedLoans.Add(match);
                existing.Add(match);
            }

            match.CustomerName = row.CustomerName;
            match.Pan = row.Pan;
            match.Mobile = row.Mobile;
            match.Product = row.Product;
            match.SanctionedAmount = row.SanctionedAmount;
            match.OutstandingAmount = row.OutstandingAmount;
            match.Status = row.Status;
            match.DisbursedOn = row.DisbursedOn;
            match.Branch = row.Branch;
            match.ImportedAt = nowUtc ?? DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return rows.Count;
    }
}
