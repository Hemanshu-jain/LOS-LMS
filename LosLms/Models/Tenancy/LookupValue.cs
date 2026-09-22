using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// A company-scoped, editable dropdown option. Replaces the dropdowns that used to be hardcoded
/// <c>string[]</c> arrays in the stage screens — DSA/sourcing channels and loan schemes on Loan &amp;
/// Security, and RCU vendors on Reports. Admins add and rename these under Settings, and each
/// dropdown offers its active values from here.
/// </summary>
/// <remarks>
/// One table for every simple named lookup, distinguished by <see cref="Kind"/> (see
/// <see cref="LookupKind"/>) — mirroring <see cref="Branch"/> rather than adding three near-identical
/// tables. Company-scoped by the same global query filter as Branch. Deactivated, never deleted, so a
/// value removed from a dropdown still renders on the applications that already stored it: the stage
/// screens save the chosen value as a name string, not a foreign key.
/// </remarks>
public class LookupValue
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    /// <summary>Which dropdown this value belongs to — one of the <see cref="LookupKind"/> constants.</summary>
    [MaxLength(40)]
    public string Kind { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional short agent/reference code. Only meaningful for <see cref="LookupKind.SourcingChannel"/>
    /// (a DSA's code), where it is shown in the dropdown as "Name — CODE" and recorded on the
    /// application as <see cref="Application.SourcingAgentCode"/>. Null for the other kinds.
    /// </summary>
    [MaxLength(40)]
    public string? Code { get; set; }

    /// <summary>Cleared, never deleted — a retired option leaves historical applications intact.</summary>
    public bool IsActive { get; set; } = true;

    public Company? Company { get; set; }
}

/// <summary>
/// The kinds of <see cref="LookupValue"/>. The value is stored verbatim in the <c>Kind</c> column, and
/// <see cref="Defaults"/> is what a company starts with so no dropdown is ever empty on first use.
/// </summary>
public static class LookupKind
{
    public const string SourcingChannel = "SourcingChannel";
    public const string Scheme = "Scheme";
    public const string RcuVendor = "RcuVendor";

    /// <summary>Every kind, so a seeder can walk them.</summary>
    public static readonly string[] All = { SourcingChannel, Scheme, RcuVendor };

    /// <summary>
    /// The built-in options seeded for a company the first time it has none of a kind. These are the
    /// exact values that used to be hardcoded in the stage screens, so upgrading changes nothing the
    /// user sees until they edit the lists themselves.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> Defaults =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [SourcingChannel] = new[] { "DSA — Patil Motors", "DSA — Shree Associates", "Branch walk-in", "Digital" },
            [Scheme] = new[] { "CV-STD-2026", "CV-PRIME-2026", "LAP-STD-2026" },
            [RcuVendor] = new[]
            {
                "Verified Field Services", "TransUnion CIBIL RCU", "CRISIL Risk Solutions",
                "SecureCheck Verifications", "Other",
            },
        };
}
