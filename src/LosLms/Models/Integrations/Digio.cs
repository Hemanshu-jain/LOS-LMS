using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// One thing we asked Digio for. Written BEFORE the call leaves (status Sending) and updated after, so a crash mid-call leaves
/// an inspectable row. <see cref="Id"/> is OUR id: it is sent to Digio as the idempotency/correlation key
/// (unique_request_id / reference_id / callback / customer_ref_number) and matches its webhooks back to this row.
/// </summary>
public class DigioRequest
{
    /// <summary>32-char Guid "N".</summary>
    [Key]
    [MaxLength(32)]
    public string Id { get; set; } = string.Empty;

    public int CompanyId { get; set; }

    [MaxLength(20)]
    public string? ApplicationId { get; set; }

    public int? PartyId { get; set; }

    /// <summary>Sandbox or Production: the environment the call went to. Sandbox evidence can never satisfy a production company.</summary>
    [MaxLength(12)]
    public string Environment { get; set; } = "Sandbox";

    /// <summary>KYC_ID, KYC_STUDIO, AML, BANK, RC, KYB, ESIGN, MANDATE…</summary>
    [MaxLength(20)]
    public string Product { get; set; } = string.Empty;

    /// <summary>PAN_VERIFY, DIGILOCKER_AADHAAR_PAN, SIGN_AGREEMENT, ENACH_CREATE…</summary>
    [MaxLength(40)]
    public string Operation { get; set; } = string.Empty;

    /// <summary>Digio's id (KID…, DID…, ENA…); unique per company when set.</summary>
    [MaxLength(80)]
    public string? DigioId { get; set; }

    /// <summary>Sending, Sent, WaitingForCustomer, Processing, Succeeded, Failed, Rejected, Expired, Cancelled.</summary>
    [MaxLength(20)]
    public string Status { get; set; } = "Sending";

    /// <summary>Digio's own status string, for support.</summary>
    [MaxLength(60)]
    public string? ProviderStatus { get; set; }

    /// <summary>SHA-256 of the inputs that matter; evidence goes stale when the current inputs no longer hash to it.</summary>
    [MaxLength(64)]
    public string? InputFingerprint { get; set; }

    [MaxLength(450)]
    public string? RequestedByUserId { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? LastPolledAt { get; set; }

    public int PollAttempts { get; set; }

    public DateTime? ExpiresAt { get; set; }

    /// <summary>Link, OnDevice or None.</summary>
    [MaxLength(10)]
    public string? CustomerChannel { get; set; }

    /// <summary>Masked mobile/email only.</summary>
    [MaxLength(60)]
    public string? NotifiedTo { get; set; }

    [MaxLength(40)]
    public string? ErrorCode { get; set; }

    [MaxLength(300)]
    public string? ErrorMessage { get; set; }

    /// <summary>The consent wording and version the customer agreed to before this request (Aadhaar, AML, DigiLocker).</summary>
    [MaxLength(300)]
    public string? Consent { get; set; }

    /// <summary>Minimised result, AES-GCM encrypted with the vault key (see PasswordVault.Seal). Never plain.</summary>
    public string? ResultJson { get; set; }
}

/// <summary>Every verified webhook, stored before it is processed. (CompanyId, DigioEventId) is unique: a redelivery is a no-op.</summary>
public class DigioWebhookInbox
{
    public long Id { get; set; }

    public int CompanyId { get; set; }

    /// <summary>The payload's own id (WHN…), the de-duplication key.</summary>
    [MaxLength(80)]
    public string DigioEventId { get; set; } = string.Empty;

    [MaxLength(60)]
    public string Event { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? EntityType { get; set; }

    [MaxLength(80)]
    public string? EntityId { get; set; }

    public DateTime ReceivedAt { get; set; }

    [MaxLength(45)]
    public string? SourceIp { get; set; }

    /// <summary>Pending, Processed, Ignored or Failed.</summary>
    [MaxLength(10)]
    public string Status { get; set; } = "Pending";

    public int Attempts { get; set; }

    public DateTime? NextAttemptAt { get; set; }

    [MaxLength(300)]
    public string? LastError { get; set; }

    /// <summary>The raw body, encrypted with the vault key.</summary>
    public string PayloadJson { get; set; } = string.Empty;

    public DateTime? ProcessedAt { get; set; }
}

/// <summary>What gates read: a verdict about one subject (a PAN, an account, the signed agreement…) from one Digio request.</summary>
public class VerificationEvidence
{
    public long Id { get; set; }

    public int CompanyId { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    public int? PartyId { get; set; }

    /// <summary>PAN, AADHAAR, FACE, LIVENESS, AML, CKYC, BANK_ACCOUNT, VEHICLE_RC, GSTIN, CIN, DIN, AGREEMENT_SIGNED, MANDATE…</summary>
    [MaxLength(24)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>Verified, Mismatch, Failed, Unavailable or AcceptedMismatch (a recorded human decision).</summary>
    [MaxLength(20)]
    public string Verdict { get; set; } = string.Empty;

    public decimal? Score { get; set; }

    /// <summary>Short non-PII text, e.g. "Name match 92, DOB match".</summary>
    [MaxLength(300)]
    public string? Summary { get; set; }

    [MaxLength(32)]
    public string? DigioRequestId { get; set; }

    [MaxLength(12)]
    public string Environment { get; set; } = "Sandbox";

    [MaxLength(64)]
    public string? InputFingerprint { get; set; }

    [MaxLength(450)]
    public string? DecidedByUserId { get; set; }

    [MaxLength(300)]
    public string? DecisionNote { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>True when the inputs the verdict was reached on have changed since: the gate treats it as not verified.</summary>
    public bool IsStaleFor(string? currentFingerprint) =>
        InputFingerprint is not null && currentFingerprint is not null && InputFingerprint != currentFingerprint;
}

/// <summary>One company's non-secret Digio configuration. Secrets stay in the vault.</summary>
public class DigioSettings
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    // Product toggles: off until the account has the product and the company opts in.
    public bool PanVerify { get; set; }
    public bool DigiLocker { get; set; }
    public bool AadhaarMask { get; set; }
    public bool Aml { get; set; }
    public bool IdOcr { get; set; }
    public bool Face { get; set; }
    public bool Rc { get; set; }
    public bool Kyb { get; set; }
    public bool Esign { get; set; }
    public bool Estamp { get; set; }
    public bool Enach { get; set; }
    public bool Debits { get; set; }
    public bool Ckyc { get; set; }
    public bool UpiVpa { get; set; }
    public bool Uan { get; set; }

    [MaxLength(60)]
    public string DigiLockerTemplate { get; set; } = "DIGILOCKER_AADHAAR_PAN";

    /// <summary>SMS, WHATSAPP, ALL or EMAIL: how Digio notifies a customer.</summary>
    [MaxLength(10)]
    public string DefaultChannel { get; set; } = "SMS";

    public int LinkExpiryDays { get; set; } = 7;

    [MaxLength(100)]
    public string? LenderSignatoryName { get; set; }

    /// <summary>IFileStore key of the lender's signature image, stamped onto the agreement by us (the lender is not a Digio signer).</summary>
    [MaxLength(400)]
    public string? LenderSignaturePath { get; set; }

    /// <summary>DigiCollect corporate_config_id, one per environment so a sandbox id can never go to production. Not a secret, never in the repo.</summary>
    [MaxLength(80)]
    public string? EnachConfigIdSandbox { get; set; }

    [MaxLength(80)]
    public string? EnachConfigIdProduction { get; set; }

    /// <summary>management_category for the EMI mandate (confirm with the sponsor bank).</summary>
    [MaxLength(10)]
    public string NachCategory { get; set; } = "L001";

    /// <summary>Adhoc lets a bounced EMI be re-presented; Monthly allows one success a month.</summary>
    [MaxLength(20)]
    public string NachFrequency { get; set; } = "Adhoc";

    /// <summary>maximum_amount = EMI x this, rounded up to Rs 100.</summary>
    public decimal NachMaxMultiplier { get; set; } = 2m;

    /// <summary>final_collection_date = last EMI + this many months (room to re-present a bounce).</summary>
    public int NachFinalBufferMonths { get; set; } = 12;

    /// <summary>Days before the due date an EMI debit is created at Digio (cut-off is T-1; holidays roll forward).</summary>
    public int DebitLeadDays { get; set; } = 2;

    /// <summary>Debit attempts per instalment, the first included (slice 3 re-presentation).</summary>
    public int MaxAttempts { get; set; } = 2;

    /// <summary>Days to wait after a bounce before presenting again (Digio refuses a new debit within 3 days of a failure, DG103).</summary>
    public int RepresentAfterDays { get; set; } = 3;

    /// <summary>Charge for each bounced debit (before GST), and the GST % on it. An Admin can waive a charge.</summary>
    public decimal BounceChargeAmount { get; set; } = 500m;

    public decimal BounceChargeGstPct { get; set; } = 18m;

    /// <summary>The NBFC's collection account at the sponsor bank, per environment (a sandbox account must never receive production debits).</summary>
    [MaxLength(30)]
    public string? CorporateAccountSandbox { get; set; }

    [MaxLength(30)]
    public string? CorporateAccountProduction { get; set; }

    /// <summary>Digio e-stamp inventory tag (entity_STATE_amount_LOB); empty = no stamp requested.</summary>
    [MaxLength(80)]
    public string? EstampTag { get; set; }

    [MaxLength(200)]
    public string? LenderSignatoryEmail { get; set; }

    /// <summary>https logo shown on Digio's pages.</summary>
    [MaxLength(300)]
    public string? BrandLogoUrl { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>One signer on a loan-agreement sign request: the per-signer status rows on Post Sanction. Refreshed from Digio's signing_parties.</summary>
public class EsignSigner
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>Digio's document id (DID...) this row belongs to; a re-send makes new rows.</summary>
    [MaxLength(80)]
    public string DocumentId { get; set; } = string.Empty;

    public int? PartyId { get; set; }

    [MaxLength(20)]
    public string PartyType { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>The mobile/email Digio knows the signer by (matches signing_parties[].identifier).</summary>
    [MaxLength(200)]
    public string Identifier { get; set; } = string.Empty;

    /// <summary>1-based signing order (sequential).</summary>
    public int SignOrder { get; set; }

    /// <summary>requested, sequenced (waiting for the earlier signer), signed, failed, rejected, expired, cancelled.</summary>
    [MaxLength(20)]
    public string Status { get; set; } = "sequenced";

    [MaxLength(300)]
    public string? FailureReason { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>When our refresh first saw this signer as signed (the page shows "Signed by ... on ...").</summary>
    public DateTime? SignedAt { get; set; }
}
