using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// One billable provider API and what a successful call costs. Global reference data (not tenant
/// scoped), seeded from the Digio quote and editable by the vendor — a rate change never needs a deploy.
/// Clients are charged exactly the provider's rate, so there is a single price column.
/// </summary>
public class ApiRate
{
    /// <summary>Stable code the call sites use, e.g. <c>KYC_DIGILOCKER</c>.</summary>
    [Key]
    [MaxLength(40)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Report grouping: DigiSign, DigiKYC, Bank verification, DigiCollect…</summary>
    [MaxLength(40)]
    public string Product { get; set; } = string.Empty;

    /// <summary>Rupees per successful call, excluding GST.</summary>
    public decimal UnitRate { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// One provider API call made on a company's behalf. Insert-only: the rate is snapshotted so a later
/// rate change never rewrites history. Only successful calls are billed (Digio bills per success).
/// </summary>
public class ApiUsageLog
{
    public long Id { get; set; }

    public int CompanyId { get; set; }

    [MaxLength(40)]
    public string ApiCode { get; set; } = string.Empty;

    /// <summary>The loan file the call was made for, when there is one.</summary>
    [MaxLength(20)]
    public string? ApplicationId { get; set; }

    /// <summary>The provider's own request/transaction id, for reconciling against their invoice.</summary>
    [MaxLength(80)]
    public string? ProviderRef { get; set; }

    public bool IsSuccess { get; set; }

    /// <summary>Rate at the time of the call (ex-GST). Charged only when <see cref="IsSuccess"/>.</summary>
    public decimal UnitRate { get; set; }

    public DateTime CreatedAt { get; set; }

    public Company? Company { get; set; }
}

/// <summary>
/// The monthly bill for a company's API usage (postpaid). Generated lazily once the month has closed;
/// unpaid past <see cref="DueDate"/> it blocks the company's API use until the vendor marks it paid.
/// </summary>
public class ApiInvoice
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    /// <summary>First day of the billed month.</summary>
    public DateOnly PeriodStart { get; set; }

    /// <summary>Sum of successful-call rates, ex-GST.</summary>
    public decimal Subtotal { get; set; }

    public decimal Gst { get; set; }

    /// <summary>Last day the client may pay before API use is paused (end of the grace window).</summary>
    public DateOnly DueDate { get; set; }

    public DateTime GeneratedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    [MaxLength(200)]
    public string? PaymentNote { get; set; }

    public decimal Total => Subtotal + Gst;

    public Company? Company { get; set; }
}
