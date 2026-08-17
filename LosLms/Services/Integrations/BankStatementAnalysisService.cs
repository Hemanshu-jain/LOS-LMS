using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// What an automated bank-statement read would return once a provider is wired: the average monthly
/// money in and out, and any recurring debit that looks like someone else's EMI.
/// </summary>
/// <param name="AvgMonthlyCredits">Average of the monthly total credits over the window.</param>
/// <param name="AvgMonthlyDebits">Average of the monthly total debits over the window.</param>
/// <param name="RecurringEmis">Debits that recur monthly at a stable amount to a lender-like payee.</param>
public sealed record BankAnalysisResult(
    decimal AvgMonthlyCredits,
    decimal AvgMonthlyDebits,
    IReadOnlyList<DetectedEmi> RecurringEmis);

/// <summary>One recurring EMI-like debit the analysis spotted.</summary>
/// <param name="LenderHint">Best guess at the payee, for the officer to confirm. Not trusted as fact.</param>
/// <param name="Emi">The recurring monthly amount.</param>
public sealed record DetectedEmi(string LenderHint, decimal Emi);

/// <summary>
/// The bank-statement analysis pipeline: honest stub for the fetch, real write-back for the result.
/// </summary>
/// <remarks>
/// No statement-analysis provider is configured in this build, so nothing ever calls
/// <see cref="ApplyAsync"/> with a real result — the Bank &amp; Financial screen only records a
/// <see cref="BankStatementAnalysis"/> request row with status <c>NotConfigured</c> and tells the
/// officer to enter income and existing-loan details by hand. The mapping below is built in full so
/// that wiring a provider later is a one-call change: fetch statements, parse the vendor response into
/// a <see cref="BankAnalysisResult"/> (see the marked boundary), then hand it to <see cref="ApplyAsync"/>.
///
/// This is convenience only. It never replaces manual entry on Viability or Existing Loan — it writes
/// the same rows an officer would, leaving everything editable afterwards.
/// </remarks>
public static class BankStatementAnalysisService
{
    public const string Unavailable =
        "Bank statement analysis unavailable — provider not configured. "
        + "Enter income and existing loan details manually.";

    public const string DetectedLender = "Detected from bank statement";

    /// <summary>
    /// Records that an analysis was asked for. Status is always <c>NotConfigured</c> in this build —
    /// a request row is an audit trail, never a fabricated result.
    /// </summary>
    public static async Task<BankStatementAnalysis> RequestAsync(
        LosDbContext db, string applicationId, string partyType)
    {
        var request = new BankStatementAnalysis
        {
            ApplicationId = applicationId,
            PartyType = partyType,
            Status = BankStatementAnalysis.NotConfigured,
            RequestedAt = DateTime.UtcNow,
            // RawResultJson stays null: there is no provider response to store.
        };

        db.BankStatementAnalyses.Add(request);
        await db.SaveChangesAsync();
        return request;
    }

    // ------------------------------------------------------------------------------------------------
    // Write-back — built now, unreached in this build. Wire a provider by producing a BankAnalysisResult
    // (parse the vendor's JSON where marked) and calling ApplyAsync with it.
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// Maps an analysis result onto Viability income and Existing Loan rows, creating either if absent.
    /// Additive: it fills the same fields an officer would, and never clears manual entries it did not set.
    /// </summary>
    public static async Task ApplyAsync(
        LosDbContext db, string applicationId, string partyType, BankAnalysisResult result)
    {
        // ---- Income → Viability ----
        //
        // The average monthly credit goes into IncomeOther. Documented choice: it is the neutral
        // "other income" bucket, so an automated read is not mislabelled as declared freight or salary
        // income — the officer can re-attribute it after reviewing the statements. One Viability row
        // per application, created if this is the first thing to touch it.
        var viability = await db.Viabilities.FirstOrDefaultAsync(v => v.ApplicationId == applicationId);
        if (viability is null)
        {
            viability = new Viability { ApplicationId = applicationId };
            db.Viabilities.Add(viability);
        }

        viability.IncomeOther = decimal.Round(result.AvgMonthlyCredits, 2);

        // ---- Recurring EMIs → ExistingLoan ----
        //
        // Each detected recurring debit becomes an existing-loan row for the officer to complete. Keyed
        // on party + the detected marker so re-running the analysis refreshes those rows without
        // duplicating them and without touching loans the officer entered by hand.
        var detected = await db.ExistingLoans
            .Where(l => l.ApplicationId == applicationId
                        && l.PartyType == partyType
                        && l.Lender == DetectedLender)
            .ToListAsync();

        db.ExistingLoans.RemoveRange(detected);

        foreach (var emi in result.RecurringEmis)
        {
            db.ExistingLoans.Add(new ExistingLoan
            {
                ApplicationId = applicationId,
                PartyType = partyType,
                Lender = DetectedLender,
                // LoanType, Sanctioned, Pos, Roi, DPD, bounces and RTR are left blank/default on
                // purpose — the officer completes the underwriting context; only the EMI is detected.
                Emi = decimal.Round(emi.Emi, 2),
            });
        }

        await db.SaveChangesAsync();
    }
}
