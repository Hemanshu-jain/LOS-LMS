using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>Starts a file's loan terms from its scheme, without ever overwriting what an officer has entered.</summary>
public static class SchemeTermsService
{
    public static Task<SchemeTerms?> ForAsync(LosDbContext db, int companyId, string? scheme) =>
        string.IsNullOrWhiteSpace(scheme)
            ? Task.FromResult<SchemeTerms?>(null)
            : db.SchemeTerms.AsNoTracking().FirstOrDefaultAsync(t => t.CompanyId == companyId && t.Scheme == scheme);

    /// <summary>
    /// Moves the file from one scheme's terms to another's. A field is replaced only when it is blank or still equals what the previous
    /// scheme set (so the officer never typed it); a figure the officer changed stays. A scheme that does not set a figure leaves it alone.
    /// </summary>
    public static void Apply(Application app, SchemeTerms? previous, SchemeTerms? next)
    {
        if (next is null) { return; }

        if (next.Amount is { } amount && (app.LoanAmount <= 0m || app.LoanAmount == previous?.Amount)) { app.LoanAmount = amount; }
        if (next.Tenure is { } tenure && (app.Tenure is null or 0 || app.Tenure == previous?.Tenure)) { app.Tenure = tenure; }
        if (next.Roi is { } roi && (app.Roi is null or 0m || app.Roi == previous?.Roi)) { app.Roi = roi; }
        if (next.ProcessingFee is { } fee && (app.ProcessingFee is null or 0m || app.ProcessingFee == previous?.ProcessingFee)) { app.ProcessingFee = fee; }
        if (next.AdvanceEmi is { } emi && (app.AdvanceEmi is null or 0m || app.AdvanceEmi == previous?.AdvanceEmi)) { app.AdvanceEmi = emi; }
    }
}
