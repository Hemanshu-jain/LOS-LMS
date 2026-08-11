using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>Outcome of checking a requested amount against the company's catalog ceiling.</summary>
/// <param name="Blocked">True stops Loan &amp; Security completing.</param>
/// <param name="Reason">Shown to the officer; null when nothing is wrong.</param>
/// <param name="Cap">The ceiling that applied, or null when no catalog row matched.</param>
public sealed record VehicleCapResult(bool Blocked, string? Reason, decimal? Cap)
{
    public static readonly VehicleCapResult Clear = new(false, null, null);
}

/// <summary>
/// Whether the requested amount is within what this company will lend against this vehicle.
/// </summary>
/// <remarks>
/// Lives only on Loan &amp; Security. Unlike the CIBIL gate it needs no retrofit into the other seven
/// screens: the vehicle and the amount are both set here, and the file cannot leave Stage 2 until the
/// check passes or an Admin bypasses it.
///
/// The catalog is keyed on make, model AND YEAR, because a model's ceiling drops as it ages. A
/// lookup that ignored the year would price a four-year-old truck like a new one.
/// </remarks>
public static class VehicleCapCheck
{
    /// <summary>
    /// The cap expressed as a stage gate, with any admin bypass folded in. This is what both the
    /// button state and the server-side guard use, so they cannot disagree.
    /// </summary>
    public static async Task<GateResult> GateAsync(
        LosDbContext db,
        string applicationId,
        string assetType,
        string? makeModel,
        string? mfgYear,
        decimal requestedAmount)
    {
        var result = await EvaluateAsync(db, assetType, makeModel, mfgYear, requestedAmount);
        if (!result.Blocked)
        {
            return GateResult.Clear;
        }

        var subject = SubjectKeyFor(makeModel, mfgYear);

        var (pending, approved, deniedNote) = await GateCheckService.LatestRequestStateAsync(
            db, applicationId, AdminRequest.MakeModelCapBypass, subject);

        // An approved bypass for THIS vehicle clears it. For any other vehicle it does not exist.
        return approved
            ? GateResult.Clear
            : new GateResult(false, result.Reason, AdminRequest.MakeModelCapBypass, pending, deniedNote);
    }

    /// <summary>Identifies the exact vehicle a cap bypass was granted for.</summary>
    public static string SubjectKeyFor(string? makeModel, string? mfgYear) =>
        $"{makeModel?.Trim()}|{mfgYear?.Trim()}";

    public static async Task<VehicleCapResult> EvaluateAsync(
        LosDbContext db,
        string assetType,
        string? makeModel,
        string? mfgYear,
        decimal requestedAmount)
    {
        // Property has no make, model or year — there is nothing to cap it against here.
        if (!string.Equals(assetType, "Vehicle", StringComparison.OrdinalIgnoreCase))
        {
            return VehicleCapResult.Clear;
        }

        // Nothing chosen yet is not a breach. The officer is still filling the form, and the
        // completeness rules already require a vehicle before the stage counts as done.
        if (string.IsNullOrWhiteSpace(makeModel) || requestedAmount <= 0m)
        {
            return VehicleCapResult.Clear;
        }

        var caps = await db.VehicleLoanCaps.AsNoTracking().ToListAsync();

        // Composed "Make Model" is what SecurityDetail stores, so match on the same shape.
        var match = caps.FirstOrDefault(c =>
            string.Equals($"{c.Make} {c.Model}", makeModel.Trim(), StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(mfgYear) || c.Year.ToString() == mfgYear.Trim()));

        if (match is null)
        {
            // Absent from the catalog is treated exactly like exceeding a cap. A vehicle nobody has
            // priced is not implicitly unlimited — that is the assumption this whole table exists to
            // remove.
            return new VehicleCapResult(
                Blocked: true,
                Reason: "No loan cap configured for this vehicle — admin action required.",
                Cap: null);
        }

        if (requestedAmount <= match.MaxLoanAmount)
        {
            return VehicleCapResult.Clear;
        }

        return new VehicleCapResult(
            Blocked: true,
            Reason: $"Requested amount exceeds the cap for {match.Make} {match.Model} ({match.Year}) "
                    + $"of {Inr(match.MaxLoanAmount)}. Request admin bypass to continue.",
            Cap: match.MaxLoanAmount);
    }

    private static string Inr(decimal amount) =>
        "₹" + amount.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("en-IN"));
}
