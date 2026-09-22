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

    /// <summary>
    /// Short unique company code (3 characters, derived from the name), used as the prefix of every
    /// application id this company creates — e.g. "KIS" gives KIS-2026-000001. Because the code is
    /// unique across companies (a unique index plus generation that avoids clashes), each company's
    /// application numbering is segregated inside the key itself and can never collide with another's.
    /// Nullable only so the column can be added to existing rows; a startup backfill fills any that are
    /// missing, and every newly provisioned company is given one at creation.
    /// </summary>
    [MaxLength(3)]
    public string? Code { get; set; }

    // ---- Profile ----

    [MaxLength(400)] public string? Address { get; set; }
    [MaxLength(200)] public string? ContactEmail { get; set; }
    [MaxLength(20)] public string? ContactPhone { get; set; }

    // ---- Firm details (mirror Business.*; the source of truth pre-filled onto Approvals so the
    //      officer no longer retypes them per file — "approvals entered in backend only", #14) ----

    [MaxLength(60)] public string? Constitution { get; set; }
    [MaxLength(20)] public string? Gstin { get; set; }
    [MaxLength(30)] public string? Vintage { get; set; }
    public DateOnly? IncorpDate { get; set; }

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
    /// The danger band is aligned to the hard cap by default (<see cref="FoirRiskDangerPct"/> = 50 =
    /// <see cref="FoirCapPct"/>, and 85 = <see cref="LtvCapPct"/> for LTV), so the red signal fires at
    /// exactly the point the eligibility engine starts capping — rather than the old defaults (danger
    /// 60/90 against caps 50/85), which let a file read amber "elevated but permissible" while it was
    /// already being capped. The values remain editable per company in Company Setup and still need the
    /// client to confirm the actual policy numbers; only the ordering is fixed here.
    /// </remarks>
    public decimal FoirRiskCautionPct { get; set; } = 40m;

    /// <summary>FOIR above this reads risk. Aligned to <see cref="FoirCapPct"/>.</summary>
    public decimal FoirRiskDangerPct { get; set; } = 50m;

    /// <summary>LTV at or below this reads healthy.</summary>
    public decimal LtvRiskCautionPct { get; set; } = 75m;

    /// <summary>LTV above this reads risk. Aligned to <see cref="LtvCapPct"/>.</summary>
    public decimal LtvRiskDangerPct { get; set; } = 85m;

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

    /// <summary>
    /// When first-run setup was completed for this company — a real <see cref="Name"/> plus at least
    /// one <see cref="Branch"/>. Null means setup is still outstanding, and until it is done every
    /// company-scoped user is redirected to Company Setup and can do nothing else. Stamped once, the
    /// first time both conditions hold; never cleared.
    /// </summary>
    public DateTime? SetupCompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
