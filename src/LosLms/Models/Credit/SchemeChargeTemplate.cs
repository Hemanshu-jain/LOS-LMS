using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// A default charge line for a scheme. When an application on this scheme first reaches Approvals and
/// has no charges yet, one <see cref="Charge"/> row is created from each template for the scheme, so
/// the officer no longer types the same processing/documentation/stamp/service charges per file.
/// </summary>
/// <remarks>
/// Company-scoped like <see cref="LookupValue"/> (keyed on <see cref="CompanyId"/> by the same global
/// query filter). <see cref="Scheme"/> is the scheme's name string, matching what an application
/// stores in <c>Application.Scheme</c> — templates are copied into the file by value, so a template
/// can be edited or deleted freely without touching applications already created.
/// </remarks>
public class SchemeChargeTemplate
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    /// <summary>The scheme this template belongs to — matches an <see cref="LookupKind.Scheme"/> name.</summary>
    [MaxLength(120)]
    public string Scheme { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Head { get; set; } = string.Empty;

    /// <summary>
    /// True → <see cref="Value"/> is a percentage of the loan amount; false → a flat rupee amount.
    /// Resolved to a concrete figure when the charge row is created.
    /// </summary>
    public bool IsPercent { get; set; }

    /// <summary>Either a flat amount or a percentage, per <see cref="IsPercent"/>.</summary>
    public decimal Value { get; set; }

    /// <summary>Whether GST applies (false for exempt heads like stamp duty).</summary>
    public bool GstApplicable { get; set; } = true;

    public Company? Company { get; set; }
}
