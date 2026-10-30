using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// An Admin's permission for ONE more run of a check that is already on file (so a Digio call is never repeated by accident), and the
/// audit of who allowed it, when and why. Created by VerificationLock.GrantAsync; <see cref="UsedAt"/> is stamped, atomically, by the run that uses it.
/// </summary>
public class VerificationRecheck
{
    public long Id { get; set; }

    public int CompanyId { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>The party the check is about; null for application-level checks (vehicle RC, GSTIN, CIN, bank account).</summary>
    public int? PartyId { get; set; }

    /// <summary>PAN, AADHAAR, AML, AADHAAR_MASK, BANK_ACCOUNT, VEHICLE_RC, GSTIN or CIN.</summary>
    [MaxLength(24)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(450)]
    public string GrantedByUserId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? GrantedByName { get; set; }

    public DateTime GrantedAt { get; set; }

    /// <summary>Null until the one allowed run starts.</summary>
    public DateTime? UsedAt { get; set; }
}
