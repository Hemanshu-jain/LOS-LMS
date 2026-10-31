using System.Security.Cryptography;
using System.Text.Json;
using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

public sealed record EsignSendResult(bool Ok, string? Error, string? DocumentId = null, bool AlreadySent = false);

/// <summary>
/// Multi-party loan-agreement e-Sign (02 section 8.1). Borrowers sign one after another (applicant, co-applicants, guarantors);
/// the lender's signature is an image we stamp on the PDF (LoanAgreementPdf), not a Digio signer. The agreement's SHA-256 is the
/// fingerprint: a regenerated agreement cancels the live request and sends a new one. Nothing here sets 'Signed' (only the verified
/// webhook / confirm does, in <see cref="DigioWebhookService"/>).
/// </summary>
public sealed class DigioEsignService(
    IDbContextFactory<LosDbContext> dbFactory, DigioClient digio, IFileStore files, DigioStatusNotifier notifier)
{
    public static readonly string[] PartyOrder = ["Applicant", "CoApplicant", "Guarantor"];
    private const int ExpireInDays = 10;

    public static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    /// <summary>The customer channels the officer can pick. DEVICE = no message, the officer opens the link on this device.</summary>
    public static readonly string[] Channels = ["SMS", "WHATSAPP", "EMAIL", "ALL", "DEVICE"];

    public async Task<EsignSendResult> SendAsync(int companyId, string applicationId, string? userId, string? channel = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var disbursement = await db.Disbursements.IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.ApplicationId == applicationId && d.Application!.CompanyId == companyId);
        if (disbursement?.AgreementFilePath is not { } path)
        {
            return new(false, "Generate the agreement first.");
        }

        var settings = await db.DigioSettings.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(s => s.CompanyId == companyId);
        if (settings is not { Esign: true } || digio.Credentials(companyId) is null)
        {
            return new(false, EsignService.Unavailable);
        }

        var bytes = await files.ReadAllBytesAsync(path);
        if (bytes is null)
        {
            return new(false, "The agreement file is missing. Generate it again.");
        }

        var hash = Sha256(bytes);
        if (disbursement.AgreementEsignStatus is "Sent" or "Signed" && disbursement.AgreementFingerprint == hash)
        {
            return new(true, null, disbursement.EsignDocumentId, AlreadySent: true); // idempotent: same agreement, same request
        }

        // A double click while the first call is still in flight: the request row (written before the call) is the lock.
        if (await db.DigioRequests.IgnoreQueryFilters().AnyAsync(r => r.CompanyId == companyId && r.ApplicationId == applicationId
                && r.Product == "ESIGN" && r.Operation == "SIGN_AGREEMENT" && r.InputFingerprint == hash && r.Status == "Sending"))
        {
            return new(false, "This agreement is already being sent. Wait a moment, then refresh.");
        }

        var parties = await LoadSignersAsync(db, applicationId);
        if (parties.Count == 0)
        {
            return new(false, "Add the applicant on Customer Details first.");
        }

        channel = Channels.Contains(channel ?? "", StringComparer.Ordinal) ? channel! : settings.DefaultChannel;
        var device = channel == "DEVICE";
        var missing = parties.FirstOrDefault(p => Identifier(p.Party, channel) is null);
        if (missing is not null)
        {
            return new(false, $"{missing.Party.FullName ?? missing.Party.PartyType} has no {(channel == "EMAIL" ? "email" : "mobile or email")} on Customer Details; the signing link goes there.");
        }

        // The agreement changed after it was sent: cancel the old request (only while it is still open) before the new one goes out.
        if (disbursement.AgreementEsignStatus == "Sent" && disbursement.EsignDocumentId is { } oldDoc)
        {
            var cancelled = await CancelAsync(companyId, applicationId, oldDoc, userId);
            if (!cancelled.Ok)
            {
                return new(false, "Could not cancel the earlier sign request: " + cancelled.Error);
            }
        }

        var requestId = Guid.NewGuid().ToString("N");
        var signers = parties.Select((p, i) => new
        {
            identifier = Identifier(p.Party, channel)!, name = p.Party.FullName, reason = "Loan agreement",
            sign_type = "aadhaar", signer_tag = p.Party.PartyType, index = i + 1,
        }).ToList();

        var body = new Dictionary<string, object?>
        {
            ["file_name"] = Path.GetFileName(path), ["file_data"] = Convert.ToBase64String(bytes),
            ["signers"] = signers, ["sequential"] = true, ["expire_in_days"] = ExpireInDays,
            ["notify_signers"] = !device, ["send_sign_link"] = !device,
            ["include_authentication_url"] = true,
            ["callback"] = requestId, ["reference_id"] = applicationId, ["display_on_page"] = "last",
        };
        if (!device)
        {
            body["customer_notification_mode"] = channel;
        }

        // ponytail: NO signature_verification. Digio's spec only says `signature_verification: object` and its sandbox rejected our guessed shape
        // (match_type / max_attempt / abort_on_fail missing). Plain Aadhaar e-sign until Digio gives the exact shape; VerificationRules below is kept
        // for that day (its field names are camelCase guesses and must be re-checked against a real sample first).

        if (settings.Estamp && !string.IsNullOrWhiteSpace(settings.EstampTag))
        {
            body["estamp_request"] = new { tags = new Dictionary<string, int> { [settings.EstampTag.Trim()] = 1 }, sign_on_page = "first" };
        }

        var result = await digio.SendAsync(new DigioCall(companyId, HttpMethod.Post, "/v2/client/document/uploadpdf", body,
            ApiCode: DigioRates.EsignSend, Product: "ESIGN", Operation: "SIGN_AGREEMENT", ApplicationId: applicationId, RequestId: requestId,
            InputFingerprint: hash, RequestedByUserId: userId, SuccessStatus: "WaitingForCustomer"));
        if (result.Status != DigioStatus.Ok || result.Str("id") is not { } docId)
        {
            return new(false, result.Status == DigioStatus.NotConfigured ? EsignService.Unavailable : result.Error ?? "Digio did not accept the document.");
        }

        var row = await db.Disbursements.IgnoreQueryFilters().FirstAsync(d => d.Id == disbursement.Id);
        row.AgreementEsignStatus = "Sent";
        row.EsignDocumentId = docId;
        row.EsignRequestId = requestId;
        row.AgreementFingerprint = hash;
        row.EsignFailureReason = null;
        row.SignedAgreementFilePath = null;
        db.EsignSigners.AddRange(parties.Select((p, i) => new EsignSigner
        {
            CompanyId = companyId, ApplicationId = applicationId, DocumentId = docId, PartyId = p.Party.Id,
            PartyType = p.Party.PartyType, Name = p.Party.FullName ?? p.Party.PartyType, Identifier = signers[i].identifier,
            SignOrder = i + 1, Status = i == 0 ? "requested" : "sequenced", UpdatedAt = DateTime.UtcNow,
        }));
        var req = await db.DigioRequests.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == requestId && r.CompanyId == companyId);
        if (req is not null)
        {
            req.ExpiresAt = DateTime.UtcNow.AddDays(ExpireInDays);
            req.CustomerChannel = device ? "OnDevice" : "Link";
            req.NotifiedTo = device ? null : Mask(signers[0].identifier) + (signers.Count > 1 ? $" +{signers.Count - 1}" : "");
        }

        await db.SaveChangesAsync();
        await RefreshSignersAsync(companyId, docId, result.Data);
        await notifier.NotifyAsync(applicationId);
        return new(true, null, docId);
    }

    /// <summary>POST /v2/client/document/{id}/cancel (only while it is still open). The old request and signer rows are closed.</summary>
    public async Task<EsignSendResult> CancelAsync(int companyId, string applicationId, string docId, string? userId)
    {
        var result = await digio.SendAsync(new DigioCall(companyId, HttpMethod.Post, $"/v2/client/document/{Uri.EscapeDataString(docId)}/cancel", new { },
            Product: "ESIGN", Operation: "CANCEL_SIGN_REQUEST", ApplicationId: applicationId, RequestedByUserId: userId,
            Free: "cancelling an open sign request (UNSURE: no price in the quote)"));
        if (result.Status != DigioStatus.Ok)
        {
            return new(false, result.Error ?? "Digio did not cancel the request.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        await db.DigioRequests.IgnoreQueryFilters().Where(r => r.CompanyId == companyId && r.DigioId == docId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, "Cancelled").SetProperty(r => r.CompletedAt, DateTime.UtcNow));
        await db.EsignSigners.IgnoreQueryFilters().Where(x => x.CompanyId == companyId && x.DocumentId == docId && x.Status != "signed")
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "cancelled").SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
        await db.Disbursements.IgnoreQueryFilters()
            .Where(d => d.EsignDocumentId == docId && d.Application!.CompanyId == companyId && d.AgreementEsignStatus == "Sent")
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.AgreementEsignStatus, "NotSent"));
        return new(true, null, docId);
    }

    /// <summary>The document as Digio reports it (status and signers). Read-only; also used by the SuperAdmin masked view.</summary>
    public Task<DigioResult> ReadDocumentAsync(int companyId, string docId) =>
        digio.GetAsync(companyId, null, $"/v2/client/document/{Uri.EscapeDataString(docId)}", null, "reading a document's signing status (UNSURE: no price in the quote)");

    /// <summary>
    /// The signer's own signing link ("Open on this device"). Only the signer who is NEXT in order (status requested) gets one. Digio does not return
    /// a link for documents sent with notify_signers=false in our setup, so the link is built the documented way (the redirection approach):
    /// {gateway}{docId}/{any txn id}/{signer identifier}?redirect_url=... , and Digio's own authentication_url is used when it does give one.
    /// </summary>
    public async Task<(string? Url, string? Why)> SignerLinkAsync(int companyId, string docId, string identifier, string returnBaseUrl)
    {
        var doc = await ReadDocumentAsync(companyId, docId);
        if (doc.Status != DigioStatus.Ok || doc.Data is not { } d || !d.TryGetProperty("signing_parties", out var parties) || parties.ValueKind != JsonValueKind.Array)
        {
            return (null, "Digio could not be reached to check the signer. Try again.");
        }

        var party = parties.EnumerateArray().Where(p => string.Equals(Str(p, "identifier"), identifier, StringComparison.OrdinalIgnoreCase)).Select(p => (JsonElement?)p).FirstOrDefault();
        if (party is null)
        {
            return (null, "Digio does not list this signer on the document.");
        }

        var status = Str(party.Value, "status");
        if (status != "requested")
        {
            return (null, status switch
            {
                "signed" => "This signer has already signed.",
                "sequenced" => "This signer is not next: the earlier signers must sign first.",
                _ => $"This signer cannot sign now (Digio status: {status ?? "unknown"}).",
            });
        }

        if (Str(party.Value, "authentication_url") is { Length: > 0 } given)
        {
            return (given, null);
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var requestId = await db.DigioRequests.IgnoreQueryFilters().Where(r => r.CompanyId == companyId && r.DigioId == docId).Select(r => r.Id).FirstOrDefaultAsync() ?? docId;
        var gateway = DigioEndpoints.Gateway(digio.Credentials(companyId)?.Production == true);
        var back = Uri.EscapeDataString($"{returnBaseUrl.TrimEnd('/')}/digio/return?r={requestId}");
        return ($"{gateway}{Uri.EscapeDataString(docId)}/{requestId}/{Uri.EscapeDataString(identifier)}?redirect_url={back}", null);
    }

    /// <summary>Copies signing_parties[].status onto the per-signer rows (matched on identifier). Safe to call any number of times.</summary>
    public async Task RefreshSignersAsync(int companyId, string docId, JsonElement? doc)
    {
        if (doc is not { ValueKind: JsonValueKind.Object } d || !d.TryGetProperty("signing_parties", out var parties) || parties.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var rows = await db.EsignSigners.IgnoreQueryFilters().Where(x => x.CompanyId == companyId && x.DocumentId == docId).ToListAsync();
        foreach (var p in parties.EnumerateArray())
        {
            var row = rows.FirstOrDefault(r => string.Equals(r.Identifier, Str(p, "identifier"), StringComparison.OrdinalIgnoreCase));
            if (row is null || Str(p, "status") is not { } status || row.Status == "signed")
            {
                continue;
            }

            row.Status = status;
            row.UpdatedAt = DateTime.UtcNow;
            row.FailureReason = status is "failed" ? RuleFailure(p) : null;
        }

        await db.SaveChangesAsync();
    }

    // ---- helpers ----

    private sealed record Signer(Party Party);

    /// <summary>Borrowers in signing order: applicant, co-applicants, guarantors. A party with no name or contact still appears (and blocks the send with a clear reason).</summary>
    private static async Task<List<Signer>> LoadSignersAsync(LosDbContext db, string applicationId)
    {
        var all = await db.Parties.IgnoreQueryFilters().AsNoTracking().Where(p => p.ApplicationId == applicationId).ToListAsync();
        return all.Where(p => PartyOrder.Contains(p.PartyType))
            .OrderBy(p => Array.IndexOf(PartyOrder, p.PartyType)).ThenBy(p => p.Id).Select(p => new Signer(p)).ToList();
    }

    private static string? Identifier(Party p, string channel) =>
        channel == "EMAIL" ? Blank(p.Email) : Blank(p.Mobile) ?? Blank(p.Email);

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>NAME fuzzy, YOB, AADHAAR last 4, GENDER: only those the party's verified data supports.</summary>
    internal static object[]? VerificationRules(Party p)
    {
        var conditions = new List<object>();
        if (Blank(p.FullName) is { } name)
        {
            conditions.Add(new { field = "NAME", matchType = "FUZZY", value = name, threshold = 70 });
        }

        if (p.DateOfBirth is { } dob)
        {
            conditions.Add(new { field = "YOB", matchType = "EXACT", value = dob.Year.ToString() });
        }

        if (Blank(p.Aadhaar) is { Length: >= 4 } a)
        {
            conditions.Add(new { field = "AADHAAR", matchType = "EXACT", value = a[^4..] });
        }

        if (p.Gender?.Trim().ToUpperInvariant() is { Length: > 0 } g && (g[0] == 'M' || g[0] == 'F'))
        {
            conditions.Add(new { field = "GENDER", matchType = "EXACT", value = g[0].ToString() });
        }

        return conditions.Count == 0 ? null : [new { operation = "AND", conditions }];
    }

    internal static string Mask(string identifier) =>
        identifier.Contains('@') ? identifier[..1] + "***" + identifier[identifier.IndexOf('@')..]
        : identifier.Length > 4 ? new string('X', identifier.Length - 4) + identifier[^4..] : identifier;

    private static string? RuleFailure(JsonElement party)
    {
        if (!party.TryGetProperty("signature_verification_response", out var r) || r.ValueKind != JsonValueKind.Object
            || !r.TryGetProperty("ruleResults", out var rules) || rules.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var failed = rules.EnumerateArray()
            .SelectMany(x => x.TryGetProperty("conditions", out var c) && c.ValueKind == JsonValueKind.Array ? c.EnumerateArray() : [])
            .Where(c => Str(c, "result") == "FAIL").Select(c => Str(c, "field")).Where(f => f is not null).Distinct().ToList();
        return failed.Count == 0 ? null : "Signer checks failed: " + string.Join(", ", failed);
    }

    private static string? Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
