using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// The standard loan terms of a scheme, set in Settings → Scheme Charges: requested amount, tenure, rate, and the exact processing
/// fee and advance EMI in rupees. Picking the scheme on Loan Details starts the file from these; the officer can still change any
/// of them. The scheme itself (a <see cref="LookupKind.Scheme"/> name) is not changed by this, so existing schemes and files are untouched.
/// </summary>
/// <remarks>Company-scoped like <see cref="SchemeChargeTemplate"/>. Every figure is optional: a blank means "this scheme does not set it".</remarks>
public class SchemeTerms
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    /// <summary>Matches a <see cref="LookupKind.Scheme"/> name.</summary>
    [MaxLength(120)]
    public string Scheme { get; set; } = string.Empty;

    public decimal? Amount { get; set; }

    public int? Tenure { get; set; }

    public decimal? Roi { get; set; }

    public decimal? ProcessingFee { get; set; }

    public decimal? AdvanceEmi { get; set; }

    public Company? Company { get; set; }
}
