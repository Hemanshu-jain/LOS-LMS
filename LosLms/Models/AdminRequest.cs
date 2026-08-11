using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// Something an officer needs an Admin to decide: bypass a gate, or reject an application.
/// </summary>
/// <remarks>
/// One table for all three request types rather than three. They differ only in what approving them
/// does — everything else (who asked, when, why, who decided, what they said) is identical, and three
/// near-identical tables would mean the Admin Inbox joined across all of them to build one list.
///
/// No CompanyId of its own. A request reaches its company through its application, which is already
/// query-filtered, so the Inbox is tenant-scoped without a second filter that could disagree with the
/// first.
/// </remarks>
public class AdminRequest
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>One of <see cref="CibilBypass"/>, <see cref="MakeModelCapBypass"/>, <see cref="Reject"/>.</summary>
    [MaxLength(30)]
    public string RequestType { get; set; } = string.Empty;

    /// <summary>Pending / Approved / Denied.</summary>
    [MaxLength(20)]
    public string Status { get; set; } = Pending;

    public string? RequestedByUserId { get; set; }

    public DateTime RequestedAt { get; set; }

    /// <summary>Required when rejecting; optional context on the two bypass types.</summary>
    [MaxLength(1000)]
    public string? RequestReason { get; set; }

    /// <summary>
    /// What the request is about, when "this application" is not specific enough. A vehicle cap
    /// bypass names the exact make/model/year, so approving one does not quietly clear a different
    /// vehicle swapped in afterwards. Null for types where the application itself is the subject.
    /// </summary>
    [MaxLength(120)]
    public string? SubjectKey { get; set; }

    public string? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// The Admin's words. Required when denying — "no" without a reason is not a decision an officer
    /// can act on — and optional when approving.
    /// </summary>
    [MaxLength(1000)]
    public string? ReviewNote { get; set; }

    public Application? Application { get; set; }
    public ApplicationUser? RequestedBy { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }

    // ---- Request types ----

    /// <summary>Unblocks the CIBIL gate for the whole application.</summary>
    public const string CibilBypass = "CibilBypass";

    /// <summary>Unblocks Loan &amp; Security when the vehicle is over cap or absent from the catalog.</summary>
    public const string MakeModelCapBypass = "MakeModelCapBypass";

    /// <summary>Approving this is what actually rejects the application.</summary>
    public const string Reject = "Reject";

    // ---- Statuses ----

    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Denied = "Denied";

    /// <summary>Human label for the Inbox, so the raw enum value never reaches a screen.</summary>
    public static string LabelFor(string requestType) => requestType switch
    {
        CibilBypass => "CIBIL bypass",
        MakeModelCapBypass => "Vehicle cap bypass",
        Reject => "Reject application",
        _ => requestType,
    };
}
