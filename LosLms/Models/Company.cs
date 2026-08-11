using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// A tenant, and the single source of truth for that tenant's underwriting policy numbers.
/// </summary>
/// <remarks>
/// Every <see cref="Application"/>, <see cref="Branch"/>, <see cref="VehicleLoanCap"/> and
/// non-SuperAdmin <see cref="ApplicationUser"/> belongs to exactly one company, and EF global query
/// filters make that boundary structural rather than something each screen has to remember.
///
/// The policy block below used to be thirteen <c>const</c>s scattered across six files, each carrying
/// a comment admitting it was invented and needed confirming with the client. Two screens could
/// disagree about the same threshold and nothing would notice. They now live here, editable per
/// company from Company Setup, and are read through <see cref="Services.CompanyPolicy"/>.
///
/// Not itself query-filtered: a company row is only ever reached by id from a claim, and the
/// SuperAdmin screens need to enumerate all of them.
/// </remarks>
public class Company
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    // ---- Profile ----

    [MaxLength(400)] public string? Address { get; set; }
    [MaxLength(200)] public string? ContactEmail { get; set; }
    [MaxLength(20)] public string? ContactPhone { get; set; }

    // ---- Policy: queue ----

    /// <summary>
    /// Days an application may sit in the queue before the dashboard flags it overdue.
    /// </summary>
    /// <remarks>
    /// Was <c>ApplicationsDashboard.SlaOverdueDays = 5</c>, whose comment read "UNCONFIRMED. The
    /// five-day window comes from the reference spec, not from the client."
    /// </remarks>
    public int SlaOverdueDays { get; set; } = 5;

    // ---- Policy: eligibility caps (hard limits, drive a completion gate) ----

    /// <summary>Share of income the total EMI burden may reach. Caps the eligible loan amount.</summary>
    public decimal FoirCapPct { get; set; } = 50m;

    /// <summary>Share of on-road cost the loan may reach. Caps the eligible loan amount.</summary>
    public decimal LtvCapPct { get; set; } = 85m;

    // ---- Policy: risk bands (display colour only, no gate) ----

    /// <summary>FOIR at or below this reads healthy; above it reads caution.</summary>
    /// <remarks>
    /// KNOWN INCONSISTENCY, carried over deliberately rather than silently corrected:
    /// <see cref="FoirRiskDangerPct"/> defaults to 60 while <see cref="FoirCapPct"/> refuses at 50, so
    /// a file between the two reads amber — "elevated but permissible" — while the eligibility engine
    /// is already capping it. Same story for LTV: danger at 90 against a cap of 85. A file sitting
    /// exactly at either cap also never reads healthy. Resolve as a business decision in Company
    /// Setup; the code no longer has an opinion.
    /// </remarks>
    public decimal FoirRiskCautionPct { get; set; } = 40m;

    /// <summary>FOIR above this reads risk.</summary>
    public decimal FoirRiskDangerPct { get; set; } = 60m;

    /// <summary>LTV at or below this reads healthy.</summary>
    public decimal LtvRiskCautionPct { get; set; } = 75m;

    /// <summary>LTV above this reads risk.</summary>
    public decimal LtvRiskDangerPct { get; set; } = 90m;

    // ---- Policy: charges ----

    /// <summary>
    /// GST applied to fee heads, as a percentage (18 means 18%, not 0.18).
    /// </summary>
    /// <remarks>
    /// Was two independent <c>GstRate = 0.18m</c> constants — one on Approvals, one in the demo
    /// seeder — plus the rate baked a third time into the literal GST amounts on three seeded charge
    /// rows. Stored as a percentage because that is how it is written on a rate card.
    /// </remarks>
    public decimal GstPct { get; set; } = 18m;

    // ---- Policy: bureau ----

    /// <summary>Lowest bureau score treated as acceptable. Consumed by the CIBIL gate.</summary>
    public int CibilMinScore { get; set; } = 300;

    /// <summary>Highest bureau score on the scale in use.</summary>
    public int CibilMaxScore { get; set; } = 900;

    // ---- Policy: documents and notes ----

    /// <summary>Days an address proof stays current before the checklist marks it stale.</summary>
    public int AddressValidityDays { get; set; } = 90;

    /// <summary>
    /// How far the deviation may move before a saved approver note counts as written against
    /// different numbers and has to be redone.
    /// </summary>
    public decimal NoteStaleTolerancePct { get; set; } = 0.5m;

    /// <summary>Reference contacts required before Loan &amp; Security counts as complete.</summary>
    public int MinimumReferences { get; set; } = 2;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
