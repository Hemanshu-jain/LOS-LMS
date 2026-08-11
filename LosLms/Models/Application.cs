using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// A loan application — the root record of the nine-stage lifecycle.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA. This exists to make the Applications Dashboard and Customer Details screens
/// functional; it is reconciled and expanded during the cross-screen review once all nine screens
/// exist. No column here should be treated as permanently locked.
///
/// Explicit <see cref="MaxLengthAttribute"/> on every string is not decoration: Pomelo maps an
/// unbounded string to LONGTEXT, and MySQL cannot use LONGTEXT as a primary key or in an index
/// without a prefix length, so the migration would fail outright.
/// </remarks>
public class Application
{
    /// <summary>Format 'LN-{year}-{6-digit sequence}', e.g. 'LN-2026-004999'. Generated server-side.</summary>
    [MaxLength(20)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Owning tenant. Stamped from the signed-in user's claims on insert and never accepted from the
    /// client — see <c>LosDbContext.StampTenant</c>. A global query filter keys off this column, so
    /// every child table is isolated too: a child row can only be reached via its application.
    /// </summary>
    public int CompanyId { get; set; }

    public Company? Company { get; set; }

    [MaxLength(100)]
    public string? CustomerType { get; set; }

    [MaxLength(100)]
    public string? Branch { get; set; }

    [MaxLength(100)]
    public string? LoanProduct { get; set; }

    [MaxLength(100)]
    public string? Scheme { get; set; }

    public decimal LoanAmount { get; set; }

    /// <summary>Tenure in months. Formatted for display ("48 mo") at render time, not stored that way.</summary>
    public int? Tenure { get; set; }

    /// <summary>Annual rate of interest as a percentage, e.g. 13.25. Rendered with "%" at display time.</summary>
    public decimal? Roi { get; set; }

    public decimal? ProcessingFee { get; set; }

    public decimal? AdvanceEmi { get; set; }

    /// <summary>One of: NACH, Post-dated cheques, Cash. Placeholder options pending confirmation.</summary>
    [MaxLength(40)]
    public string? RepaymentMode { get; set; }

    public DateOnly? DisbursalDate { get; set; }

    /// <summary>1-8. Stage 1 is Customer Details.</summary>
    public int CurrentStage { get; set; } = 1;

    /// <summary>One of: New, In progress, Sanctioned, Rejected.</summary>
    [MaxLength(30)]
    public string Status { get; set; } = "New";

    /// <summary>
    /// True once Stage 8 has released the funds. Stage 8 is the last stage, so
    /// <see cref="CurrentStage"/> stops at 8 whether or not the money ever moved — this flag is the
    /// only thing on the application that says the lifecycle actually finished.
    /// </summary>
    public bool Disbursed { get; set; }

    // The three columns below are not in the original brief's schema. They back the dashboard's
    // customer column and its sourcing-channel / assigned-officer filters, which would otherwise
    // stop working after the data-source swap. Channel and officer belong to the application
    // rather than to any one party; CustomerName is denormalised from the Applicant on save.
    [MaxLength(200)]
    public string? CustomerName { get; set; }

    [MaxLength(60)]
    public string? SourcingChannel { get; set; }

    [MaxLength(100)]
    public string? AssignedOfficer { get; set; }

    /// <summary>
    /// The assigned officer as a real foreign key into <see cref="ApplicationUser"/>, the source of
    /// truth for the dashboard's officer filter and the Summary Rail. <see cref="AssignedOfficer"/>
    /// above is the older name-string representation, kept in sync on write and now otherwise
    /// redundant — see OPEN-QUESTIONS-FOR-ARUN.md.
    /// </summary>
    public string? AssignedOfficerId { get; set; }

    public ApplicationUser? AssignedOfficerNavigation { get; set; }

    /// <summary>Doubles as the dashboard's queue date.</summary>
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Left uninitialised on purpose: EF's HasData seeding rejects a seed entity that has a
    // navigation property set, and every seeded row would otherwise carry an empty collection.
    public ICollection<Party>? Parties { get; set; }
}
