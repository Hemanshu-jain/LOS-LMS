namespace LosLms.Models;

/// <summary>
/// The fields the NACH registration flow reads and writes, shared by
/// <see cref="EnachMandate"/> and <see cref="SecurityNachMandate"/>.
/// </summary>
/// <remarks>
/// Two implementations, both real and both on screen at once — this is not an abstraction invented
/// for a hypothetical third. It exists so <c>MandateFlow.razor</c> can be written once instead of
/// pasted twice into Post Sanction, which is where the E-Nach and Security-Nach tabs sit side by
/// side running the identical flow over different rows.
///
/// It covers the flow only. Fields unique to one mandate — EnachMandate.DebitDate,
/// SecurityNachMandate.MandateHolder — stay off it and are rendered by the tab that owns them.
/// </remarks>
public interface INachMandate
{
    string? Umrn { get; set; }

    /// <summary>Pending / Registered / Rejected.</summary>
    string Status { get; set; }

    string? AccountNumber { get; set; }
    string? Ifsc { get; set; }
    string? BankName { get; set; }
    string? BankBranch { get; set; }

    /// <summary>NotRun / Unavailable. Never Matched — nothing here may fabricate a verification.</summary>
    string NameMatchStatus { get; set; }

    DateTime? NameMatchCheckedAt { get; set; }

    bool ConfirmationAccepted { get; set; }

    /// <summary>Digital / Physical.</summary>
    string? MandateType { get; set; }

    /// <summary>NetBanking / DebitCard.</summary>
    string? DigitalMode { get; set; }

    string? DigioMandateId { get; set; }
    string? AuthMode { get; set; }
    string? DestinationBankId { get; set; }
    string? DigioState { get; set; }
    string? BankState { get; set; }
    string? RejectReason { get; set; }
    string? DigioRequestId { get; set; }
    decimal? CollectionAmount { get; set; }
    decimal? MaximumAmount { get; set; }
    DateTime? RegisteredAt { get; set; }
    string? SignedFormPath { get; set; }
}
