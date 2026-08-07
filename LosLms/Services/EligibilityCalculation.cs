using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>Everything the eligible-amount formula needs, with nothing else attached.</summary>
/// <remarks>
/// <paramref name="OnRoadCost"/> is nullable on purpose: null means the CAM has never been
/// recalculated, which is *unavailable*, not a cost of zero.
/// </remarks>
public readonly record struct EligibilityInputs(
    decimal TotalIncome,
    decimal? OnRoadCost,
    decimal LoanAmount,
    decimal? Roi,
    int? Tenure,
    decimal ExistingEmiSum);

/// <summary>
/// The computed eligibility. Every figure other than <see cref="Ready"/> is zero when the inputs
/// were not sufficient to calculate — callers must check <see cref="Ready"/> before displaying any
/// of them.
/// </summary>
public sealed record EligibilityResult(
    bool Ready,
    decimal Eligible,
    decimal DeviationPct,
    decimal FoirAtEligible,
    decimal LtvPct,
    decimal FoirCapAmount,
    decimal LtvCapAmount,
    string? BindingLabel);

/// <summary>
/// How much this borrower may actually be lent. Used by Eligibility (Stage 6), which owns the
/// screen, and by Approvals (Stage 7), which pre-fills the sanctioned amount from it.
/// </summary>
/// <remarks>
/// Two entry points over one formula. <see cref="Compute"/> is pure so the Eligibility screen can
/// call it on every render against rows the officer is still editing — the eligible amount moves as
/// an EMI is typed, with no save and no query. <see cref="ComputeAsync"/> loads the same inputs from
/// the database for callers that have no in-memory state to offer.
/// </remarks>
public static class EligibilityCalculation
{
    // PLACEHOLDER underwriting policy — invented so the screen has something real to compute, and
    // flagged as such by the reference spec itself. These two numbers drive a hard completion gate,
    // so they are the first thing to confirm with Arun/the client.
    public const decimal FoirCapPct = 50m;
    public const decimal LtvCapPct = 85m;

    private static readonly EligibilityResult NotReady =
        new(false, 0m, 0m, 0m, 0m, 0m, 0m, null);

    public static EligibilityResult Compute(EligibilityInputs inputs)
    {
        // The readiness rule is part of the formula, not the screen. Returning early here is what
        // keeps the DeviationPct division below safe on an application whose loan amount is zero —
        // which is any file that reached this stage without completing Stage 2.
        var ready = inputs.TotalIncome > 0
            && inputs.OnRoadCost is > 0
            && inputs.LoanAmount > 0
            && inputs.Roi > 0
            && inputs.Tenure > 0;

        if (!ready)
        {
            return NotReady;
        }

        var onRoadCost = inputs.OnRoadCost!.Value;
        var roi = inputs.Roi!.Value;
        var tenure = inputs.Tenure!.Value;

        var maxNewEmi = Math.Max(0m, inputs.TotalIncome * FoirCapPct / 100m - inputs.ExistingEmiSum);

        var foirCapAmount = LoanMath.InverseEmi(maxNewEmi, roi, tenure);
        var ltvCapAmount = onRoadCost * LtvCapPct / 100m;

        var eligible = Math.Min(Math.Min(foirCapAmount, ltvCapAmount), inputs.LoanAmount);

        var proposedEmiAtEligible = LoanMath.Emi(eligible, roi, tenure);

        return new EligibilityResult(
            Ready: true,
            Eligible: eligible,
            DeviationPct: (eligible - inputs.LoanAmount) / inputs.LoanAmount * 100m,
            FoirAtEligible: (inputs.ExistingEmiSum + proposedEmiAtEligible) / inputs.TotalIncome * 100m,
            LtvPct: inputs.LoanAmount / onRoadCost * 100m,
            FoirCapAmount: foirCapAmount,
            LtvCapAmount: ltvCapAmount,
            BindingLabel: eligible < inputs.LoanAmount
                ? (foirCapAmount <= ltvCapAmount ? "FOIR" : "LTV")
                : null);
    }

    /// <summary>
    /// Loads the inputs for an application and computes. Returns a not-ready result when the
    /// application does not exist.
    /// </summary>
    public static async Task<EligibilityResult> ComputeAsync(LosDbContext db, string applicationId)
    {
        var application = await db.Applications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
        {
            return NotReady;
        }

        var viability = await db.Viabilities.AsNoTracking()
            .FirstOrDefaultAsync(v => v.ApplicationId == applicationId);

        var cam = await db.CamCostBreakdowns.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ApplicationId == applicationId);

        var existingEmiSum = await db.ExistingLoans.AsNoTracking()
            .Where(l => l.ApplicationId == applicationId)
            .SumAsync(l => (decimal?)l.Emi) ?? 0m;

        return Compute(new EligibilityInputs(
            TotalIncome: viability is null
                ? 0m
                : viability.IncomeFreight + viability.IncomeSalary + viability.IncomeOther,
            OnRoadCost: OnRoadCostOf(cam),
            LoanAmount: application.LoanAmount,
            Roi: application.Roi,
            Tenure: application.Tenure,
            ExistingEmiSum: existingEmiSum));
    }

    /// <summary>
    /// Applied columns only, and only once Recalculate has run — the same rule Stage 3's CAM tab
    /// enforces. Null means unavailable rather than zero.
    /// </summary>
    public static decimal? OnRoadCostOf(CamCostBreakdown? cam) =>
        cam?.LastRecalculatedAt is null
            ? null
            : (cam.AppliedExShowroomCost ?? 0m)
                + (cam.AppliedBodyAccessories ?? 0m)
                + (cam.AppliedInsuranceRegistration ?? 0m);
}
