using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>What a KYC action did, in words the officer can read. <see cref="Verdict"/> is null when nothing was decided.</summary>
/// <param name="NotEnabled">Digio said 403/404: the product is not on this account. Shown as "Not enabled", never as a failed check.</param>
public sealed record KycOutcome(string? Verdict, string Message, bool NotEnabled = false, string? RequestId = null, string? Url = null)
{
    public static KycOutcome Off(string what) => new(null, $"{what} is switched off for this company.");
}

/// <summary>
/// Stage 1 checks on top of <see cref="DigioClient"/>: PAN verify, AML screening, Aadhaar masking, DigiLocker, name matching.
/// Each writes <see cref="VerificationEvidence"/> (what the gate reads) and never invents a result: a missing field is "Unavailable".
/// Field names come from the saved sandbox samples (docs/digio/samples) and Digio's spec.
/// </summary>
public sealed class DigioKyc(DigioClient digio, ApiBillingService billing, IDbContextFactory<LosDbContext> dbFactory)
{
    public const string ConsentText = "The customer agrees that their identity details may be fetched and verified through Digio.";
    public const string ConsentVersion = "v1";
    private const int MaxMaskBytes = 1_000_000; // Digio: "not more than 1 MB"

    public Task<bool> MaskingOnAsync(int companyId) => EnabledAsync(companyId, s => s.AadhaarMask);

    // ---- PAN ----

    /// <summary>Verifies a PAN against the name (and DOB when known). Same inputs as a verdict already on file: no second call, no second charge.</summary>
    public async Task<KycOutcome> VerifyPanAsync(int companyId, string applicationId, int partyId, string? pan, string? name, DateOnly? dob, bool individual, string? userId)
    {
        if (!await EnabledAsync(companyId, s => s.PanVerify))
        {
            return KycOutcome.Off("PAN verification");
        }

        pan = pan?.Trim().ToUpperInvariant();
        if (pan is null || !System.Text.RegularExpressions.Regex.IsMatch(pan, "^[A-Z]{5}[0-9]{4}[A-Z]$") || string.IsNullOrWhiteSpace(name))
        {
            return new(null, "Enter a valid PAN and the full name first.");
        }

        var fingerprint = Fingerprint(pan, name.Trim().ToUpperInvariant(), dob?.ToString("yyyy-MM-dd")); // must equal KycEvidenceCheck.Fingerprint(party)
        if (await LatestAsync(companyId, applicationId, partyId, "PAN") is { } prior && prior.InputFingerprint == fingerprint && prior.Verdict != "Failed" && prior.Verdict != "Unavailable")
        {
            return new(prior.Verdict, prior.Summary ?? "Already verified.");
        }

        var requestId = Guid.NewGuid().ToString("N");
        var r = await digio.SendAsync(new DigioCall(companyId, HttpMethod.Post, "/v3/client/kyc/fetch_id_data/PAN",
            new { id_no = pan, name = name.Trim(), dob = dob?.ToString("yyyy-MM-dd"), unique_request_id = requestId },
            DigioRates.IdVerify, "KYC_ID", "PAN_VERIFY", applicationId, partyId, requestId, fingerprint, userId));
        if (Failure(r) is { } fail)
        {
            return fail;
        }

        var status = r.Str("status")?.ToLowerInvariant();
        if (status is null)
        {
            return await RecordAsync(companyId, applicationId, partyId, "PAN", "Unavailable", null, "Digio gave no PAN status.", requestId, r, fingerprint);
        }

        var nameOk = r.Bool("name_as_per_pan_match") == true;
        var dobOk = dob is null || r.Bool("date_of_birth_match") == true;
        var isIndividual = !individual || string.Equals(r.Str("category"), "individual", StringComparison.OrdinalIgnoreCase);
        var note = r.Str("aadhaar_seeding_status") is { } seed && !seed.Equals("y", StringComparison.OrdinalIgnoreCase) ? "; Aadhaar not seeded to PAN" : "";

        var (verdict, summary) = status != "valid" ? ("Failed", "PAN is not valid")
            : !isIndividual ? ("Mismatch", "PAN is not an individual's")
            : nameOk && dobOk ? ("Verified", "Name matches" + (dob is null ? "" : ", DOB matches") + note)
            : ("Mismatch", (nameOk ? "Name matches" : "Name does not match") + (dob is null ? "" : dobOk ? ", DOB matches" : ", DOB does not match") + note);
        return await RecordAsync(companyId, applicationId, partyId, "PAN", verdict, null, summary, requestId, r, fingerprint);
    }

    // ---- name match (internal: PAN name vs Aadhaar name vs bank-holder name) ----

    /// <summary>0-100. Order-free; "RAJ KUMAR SHARMA" matches "SHARMA R K" and "RAJ SHARMA"; one typo per word is forgiven. Free, no call.</summary>
    public static int NameScore(string? a, string? b)
    {
        var x = Tokens(a);
        var y = Tokens(b);
        if (x.Count == 0 || y.Count == 0)
        {
            return 0;
        }

        var (small, large) = x.Count <= y.Count ? (x, y) : (y, x);
        var pool = large.ToList();
        var hits = 0;
        foreach (var t in small)
        {
            var i = pool.FindIndex(u => SameToken(t, u));
            if (i >= 0)
            {
                hits++;
                pool.RemoveAt(i);
            }
        }

        return (int)Math.Round(100.0 * hits / large.Count);
    }

    private static List<string> Tokens(string? s) =>
        new string((s ?? "").ToUpperInvariant().Select(c => char.IsLetter(c) ? c : ' ').ToArray())
            .Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => t is not ("MR" or "MRS" or "MS" or "SHRI" or "SMT" or "DR")).ToList();

    private static bool SameToken(string a, string b) =>
        a == b || (a.Length == 1 && b.StartsWith(a)) || (b.Length == 1 && a.StartsWith(b)) || (a.Length > 3 && b.Length > 3 && Edits(a, b) <= 1);

    private static int Edits(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) { d[i, 0] = i; }
        for (var j = 0; j <= b.Length; j++) { d[0, j] = j; }
        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            }
        }

        return d[a.Length, b.Length];
    }

    // ---- AML ----

    /// <summary>Screens a person against Digio's lists. 403/404 means DigiShield is not on the account: reported as such, nothing recorded.</summary>
    public async Task<KycOutcome> ScreenAmlAsync(int companyId, string applicationId, int partyId, string? name, string? pan, bool consent, string? userId)
    {
        if (!await EnabledAsync(companyId, s => s.Aml))
        {
            return KycOutcome.Off("AML screening");
        }

        if (!consent)
        {
            return new(null, "Record the customer's consent first.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return new(null, "Enter the full name first.");
        }

        var fingerprint = Fingerprint(name.Trim().ToUpperInvariant(), pan?.Trim().ToUpperInvariant());
        if (await LatestAsync(companyId, applicationId, partyId, "AML") is { } prior && prior.InputFingerprint == fingerprint && prior.Verdict is "Verified" or "Mismatch" or "AcceptedMismatch")
        {
            return new(prior.Verdict, prior.Summary ?? "Already screened.");
        }

        var requestId = Guid.NewGuid().ToString("N");
        var r = await digio.SendAsync(new DigioCall(companyId, HttpMethod.Post, "/client/v1/user/screen", new
        {
            screening_mode = "SCREENING",
            screening_settings = new { screening_type = "SCREEN_ONLY_RESPONSE", response_mode = "DETAILED", cut_off_threshold = 80 },
            // A stable id with no PII in it. The DOB is left out: its format was never captured (AML is not enabled on our sandbox).
            user_details = new { client_id = $"{companyId}-{partyId}", name = Cut(name.Trim(), 50), pan = string.IsNullOrWhiteSpace(pan) ? null : pan.Trim().ToUpperInvariant() },
        }, null, "AML", "AML_SCREEN", applicationId, partyId, requestId, fingerprint, userId));
        if (Failure(r) is { } fail)
        {
            return fail;
        }

        // Without the alert count we cannot say "clear": that would be inventing a result.
        if (r.Data is not { ValueKind: JsonValueKind.Object } d || !d.TryGetProperty("total_alerts", out var t) || !t.TryGetInt32(out var alerts))
        {
            return await RecordAsync(companyId, applicationId, partyId, "AML", "Unavailable", null, "Digio's reply had no alert count.", requestId, r, fingerprint);
        }

        var high = d.TryGetProperty("high_severity_alerts", out var h) && h.TryGetInt32(out var hv) ? hv : 0;
        var score = d.TryGetProperty("top_match_score", out var s) && s.TryGetDecimal(out var sv) ? sv : (decimal?)null;
        var summary = alerts == 0 ? "No matches on the screening lists" : $"{alerts} possible match(es), {high} high severity (risk {r.Str("risk_level")?.ToLowerInvariant() ?? "unknown"})";
        return await RecordAsync(companyId, applicationId, partyId, "AML", alerts == 0 ? "Verified" : "Mismatch", score, summary, requestId, r, fingerprint);
    }

    /// <summary>An Admin's decision on AML alerts: false positive (accepted, with a note) lets Customer Details complete. Reject is the existing rejection flow.</summary>
    public async Task<bool> AcceptAmlAlertsAsync(int companyId, string applicationId, int partyId, string note, string adminUserId)
    {
        if (string.IsNullOrWhiteSpace(note) || await LatestAsync(companyId, applicationId, partyId, "AML") is not { Verdict: "Mismatch" } latest)
        {
            return false;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        db.VerificationEvidence.Add(new VerificationEvidence
        {
            CompanyId = companyId, ApplicationId = applicationId, PartyId = partyId, Subject = "AML", Verdict = "AcceptedMismatch", Score = latest.Score,
            Summary = Cut("Admin: false positive. " + latest.Summary, 300), DigioRequestId = latest.DigioRequestId, Environment = latest.Environment,
            InputFingerprint = latest.InputFingerprint, DecidedByUserId = adminUserId, DecisionNote = Cut(note.Trim(), 300), CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return true;
    }

    // ---- Aadhaar masking ----

    /// <summary>The masked file, or null with the reason. A failure means the upload is refused: an unmasked original must never be stored.</summary>
    public async Task<(byte[]? Masked, string Message)> MaskAadhaarAsync(int companyId, string applicationId, int partyId, byte[] file, string contentType, string fileName, bool consent, string? userId)
    {
        if (!await EnabledAsync(companyId, s => s.AadhaarMask))
        {
            return (null, "Aadhaar masking is switched off for this company.");
        }

        if (!consent)
        {
            return (null, "Record the customer's consent first.");
        }

        var type = contentType.ToLowerInvariant() switch { "image/png" => "PNG", "image/jpeg" => "JPEG", "image/jpg" => "JPG", "application/pdf" => "PDF", _ => null };
        if (type is null)
        {
            return (null, "Upload a PNG, JPG or PDF.");
        }

        if (file.Length > MaxMaskBytes)
        {
            return (null, "The file is over 1 MB. Compress it and upload again.");
        }

        var requestId = Guid.NewGuid().ToString("N");
        var r = await digio.SendAsync(new DigioCall(companyId, HttpMethod.Post, "/v4/client/kyc/aadhaar/mask",
            new { data = Convert.ToBase64String(file), data_content_type = type, file_name = Cut(fileName, 100), consent = "yes", mask_qr = true, is_validate = true, unique_request_id = requestId },
            DigioRates.AadhaarMasking, "KYC_ID", "AADHAAR_MASK", applicationId, partyId, requestId, null, userId));
        if (Failure(r) is { } fail)
        {
            return (null, fail.NotEnabled ? fail.Message : fail.Message + " The upload was not saved.");
        }

        var done = r.Data is { ValueKind: JsonValueKind.Object } d && d.TryGetProperty("details", out var det)
            && det.TryGetProperty("masking_done_count", out var m) && m.TryGetInt32(out var md) ? md : 0;
        var already = r.Data is { ValueKind: JsonValueKind.Object } d2 && d2.TryGetProperty("details", out var det2)
            && det2.TryGetProperty("already_masked_count", out var a) && a.TryGetInt32(out var ad) ? ad : 0;
        if (r.Str("masked_output") is not { Length: > 0 } output || (done == 0 && already == 0))
        {
            return (null, "No Aadhaar number found to mask. Upload a clearer image.");
        }

        try
        {
            return (Convert.FromBase64String(output), "Masked.");
        }
        catch (FormatException)
        {
            return (null, "Digio's masked file could not be read. Try again.");
        }
    }

    // ---- DigiLocker ----

    /// <summary>Starts a DigiLocker request. "Link": Digio notifies the customer. "OnDevice": no notification; <see cref="KycOutcome.Url"/> opens the flow here.</summary>
    public async Task<KycOutcome> StartDigiLockerAsync(int companyId, string applicationId, int partyId, string? name, string? mobile, bool onDevice, bool consent, string returnBaseUrl, string? userId)
    {
        if (!await EnabledAsync(companyId, s => s.DigiLocker))
        {
            return KycOutcome.Off("DigiLocker");
        }

        if (!consent)
        {
            return new(null, "Record the customer's consent first.");
        }

        if (mobile is null || !System.Text.RegularExpressions.Regex.IsMatch(mobile, "^[6-9][0-9]{9}$") || string.IsNullOrWhiteSpace(name))
        {
            return new(null, "Enter the full name and a valid 10-digit mobile first.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var settings = await db.DigioSettings.IgnoreQueryFilters().AsNoTracking().FirstAsync(s => s.CompanyId == companyId);
        var requestId = Guid.NewGuid().ToString("N");
        var r = await digio.SendAsync(new DigioCall(companyId, HttpMethod.Post, "/client/kyc/v2/request/with_template", new
        {
            customer_identifier = mobile, template_name = settings.DigiLockerTemplate, customer_name = Cut(name.Trim(), 100), reference_id = requestId,
            transaction_id = applicationId, notify_customer = !onDevice, expire_in_days = settings.LinkExpiryDays, generate_access_token = true,
        }, null, "KYC_ID", "DIGILOCKER_AADHAAR_PAN", applicationId, partyId, requestId, null, userId, SuccessStatus: "WaitingForCustomer"));
        if (Failure(r) is { } fail)
        {
            return fail;
        }

        var kid = r.Str("id");
        var notifiedTo = "XXXXXX" + mobile[^4..];
        var expires = DateTime.UtcNow.AddDays(settings.LinkExpiryDays);
        await db.DigioRequests.IgnoreQueryFilters().Where(x => x.Id == requestId).ExecuteUpdateAsync(u => u
            .SetProperty(x => x.CustomerChannel, onDevice ? "OnDevice" : "Link")
            .SetProperty(x => x.NotifiedTo, notifiedTo)
            .SetProperty(x => x.ExpiresAt, expires)
            .SetProperty(x => x.Consent, $"{ConsentText} (text {ConsentVersion})"));

        var url = onDevice && kid is not null && r.Data is { } d && d.TryGetProperty("access_token", out var tok) && tok.TryGetProperty("id", out _)
            ? $"{DigioEndpoints.Gateway(digio.Credentials(companyId)?.Production == true)}{kid}/{requestId}/{mobile}?redirect_url={Uri.EscapeDataString($"{returnBaseUrl.TrimEnd('/')}/digio/return?r={requestId}")}"
            : null;
        return new("Sent", onDevice ? "Opening DigiLocker on this device." : "Link sent to the customer.", RequestId: requestId, Url: url);
    }

    /// <summary>
    /// Asks Digio for the real result of a DigiLocker request and, when the customer finished, records AADHAAR evidence, fills BLANK party fields
    /// and meters once. Safe to call repeatedly (poll, webhook, "Refresh status").
    /// </summary>
    public async Task<KycOutcome> CompleteDigiLockerAsync(int companyId, string requestId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var req = await db.DigioRequests.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == requestId && x.CompanyId == companyId && x.Operation == "DIGILOCKER_AADHAAR_PAN");
        if (req?.DigioId is null || req.ApplicationId is null || req.PartyId is null)
        {
            return new(null, "No such DigiLocker request.");
        }

        if (req.Status is "Succeeded" or "Failed" or "Expired" or "Cancelled" or "Rejected")
        {
            return new(req.Status == "Succeeded" ? "Verified" : null, $"Request is {req.Status.ToLowerInvariant()}.", RequestId: requestId);
        }

        var r = await digio.GetAsync(companyId, null, $"/client/kyc/v2/{Uri.EscapeDataString(req.DigioId)}/response?detail_response=true", req.ApplicationId);
        if (Failure(r) is { } fail)
        {
            return fail with { RequestId = requestId };
        }

        var status = r.Str("status")?.ToLowerInvariant();
        var action = DigiLockerAction(r.Data);
        var actionStatus = action is { } a && a.TryGetProperty("status", out var s) ? s.GetString()?.ToLowerInvariant() : null;

        if (status is "expired" or "rejected" or "failed")
        {
            await SetStatusAsync(db, requestId, status == "expired" ? "Expired" : "Failed", status);
            return new(null, status == "expired" ? "The customer did not finish in time. Start a new request." : "DigiLocker did not complete.", RequestId: requestId);
        }

        if (status != "approved" && actionStatus is not ("success" or "approved"))
        {
            await db.DigioRequests.IgnoreQueryFilters().Where(x => x.Id == requestId).ExecuteUpdateAsync(u => u.SetProperty(x => x.LastPolledAt, DateTime.UtcNow).SetProperty(x => x.ProviderStatus, status));
            return new(null, "Waiting for the customer.", RequestId: requestId);
        }

        // One statement decides who completes it, so a webhook and a poll together record and charge once.
        var won = await db.DigioRequests.IgnoreQueryFilters().Where(x => x.Id == requestId && x.Status == "WaitingForCustomer")
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.Status, "Succeeded").SetProperty(x => x.CompletedAt, DateTime.UtcNow).SetProperty(x => x.ProviderStatus, status));
        if (won != 1)
        {
            return new("Verified", "Already completed.", RequestId: requestId);
        }

        await billing.RecordAsync(companyId, DigioRates.DigiLockerFetch, true, req.ApplicationId, req.DigioId);
        var data = action is { } act ? ReadIdentity(act) : null;
        var party = await db.Parties.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == req.PartyId && p.ApplicationId == req.ApplicationId);
        var summary = "Aadhaar fetched from DigiLocker";
        if (party is not null)
        {
            if (data is not null)
            {
                FillBlanks(party, data);
                var panName = party.FullName;
                summary += data.FullName is null || panName is null ? "" : $"; name match {NameScore(data.FullName, panName)}";
            }

            party.AadhaarVerified = true;
            party.AadhaarGeneratedOn = DateOnly.FromDateTime(DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        db.VerificationEvidence.Add(new VerificationEvidence
        {
            CompanyId = companyId, ApplicationId = req.ApplicationId, PartyId = req.PartyId, Subject = "AADHAAR", Verdict = "Verified", Summary = Cut(summary, 300),
            DigioRequestId = requestId, Environment = req.Environment, CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return new("Verified", "Aadhaar fetched from DigiLocker.", RequestId: requestId);
    }

    private static JsonElement? DigiLockerAction(JsonElement? root)
    {
        if (root is { ValueKind: JsonValueKind.Object } o && o.TryGetProperty("actions", out var actions) && actions.ValueKind == JsonValueKind.Array)
        {
            foreach (var a in actions.EnumerateArray())
            {
                if (a.TryGetProperty("type", out var t) && t.GetString() == "digilocker")
                {
                    return a;
                }
            }
        }

        return null;
    }

    // ponytail: where the filled identity sits was NOT captured in Phase 0 (no customer finished a sandbox journey). The spec puts it in
    // `id_card_data_response`; the keys below are read only if present. Re-check against a real completed sample (DECISIONS B).
    private static DigiKycData? ReadIdentity(JsonElement action)
    {
        if (!action.TryGetProperty("id_card_data_response", out var c) || c.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        string? Get(params string[] keys) => keys.Select(k => c.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        DateOnly? dob = DateOnly.TryParse(Get("dob", "date_of_birth"), out var d) ? d : null;
        return new DigiKycData(Get("name"), dob, Get("gender"), Get("fathers_name", "father_name"), Get("address"), Get("city"), Get("state"), Get("pincode", "pin_code"));
    }

    private static void FillBlanks(Party p, DigiKycData d)
    {
        p.FullName = string.IsNullOrWhiteSpace(p.FullName) ? d.FullName ?? p.FullName : p.FullName;
        p.DateOfBirth ??= d.DateOfBirth;
        p.Gender = string.IsNullOrWhiteSpace(p.Gender) ? d.Gender : p.Gender;
        p.FatherSpouseName = string.IsNullOrWhiteSpace(p.FatherSpouseName) ? d.FatherSpouseName : p.FatherSpouseName;
        p.Address1 = string.IsNullOrWhiteSpace(p.Address1) ? d.Address1 : p.Address1;
        p.City = string.IsNullOrWhiteSpace(p.City) ? d.City : p.City;
        p.State = string.IsNullOrWhiteSpace(p.State) ? d.State : p.State;
        p.PinCode = string.IsNullOrWhiteSpace(p.PinCode) ? d.PinCode : p.PinCode;
    }

    private static Task SetStatusAsync(LosDbContext db, string requestId, string status, string? provider) =>
        db.DigioRequests.IgnoreQueryFilters().Where(x => x.Id == requestId && x.Status == "WaitingForCustomer")
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.Status, status).SetProperty(x => x.CompletedAt, DateTime.UtcNow).SetProperty(x => x.ProviderStatus, provider));

    // ---- shared ----

    private async Task<bool> EnabledAsync(int companyId, Func<DigioSettings, bool> toggle)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var s = await db.DigioSettings.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId);
        return s is not null && toggle(s);
    }

    private async Task<VerificationEvidence?> LatestAsync(int companyId, string applicationId, int partyId, string subject)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.VerificationEvidence.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.ApplicationId == applicationId && e.PartyId == partyId && e.Subject == subject)
            .OrderByDescending(e => e.Id).FirstOrDefaultAsync();
    }

    private async Task<KycOutcome> RecordAsync(int companyId, string applicationId, int partyId, string subject, string verdict, decimal? score, string summary, string requestId, DigioResult r, string fingerprint)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.VerificationEvidence.Add(new VerificationEvidence
        {
            CompanyId = companyId, ApplicationId = applicationId, PartyId = partyId, Subject = subject, Verdict = verdict, Score = score, Summary = Cut(summary, 300),
            DigioRequestId = requestId, Environment = r.Environment ?? "Sandbox", InputFingerprint = fingerprint, CreatedAt = DateTime.UtcNow,
        });
        if (subject == "PAN")
        {
            // Only evidence sets this flag, never a click.
            var party = await db.Parties.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == partyId && p.ApplicationId == applicationId);
            if (party is not null)
            {
                party.PanVerified = verdict == "Verified";
            }
        }

        await db.SaveChangesAsync();
        return new(verdict, summary, RequestId: requestId);
    }

    /// <summary>Not-Ok results as an outcome.</summary>
    private static KycOutcome? Failure(DigioResult r)
    {
        if (r.Status == DigioStatus.Ok)
        {
            return null;
        }

        var notEnabled = r.HttpStatus is 403 or 404;
        return new(null, notEnabled ? "Not enabled on your Digio account. Ask Digio to switch it on." : r.Error ?? "Digio could not complete this.", NotEnabled: notEnabled, RequestId: r.RequestId);
    }

    public static string Fingerprint(params string?[] parts) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', parts.Select(p => p ?? ""))))).ToLowerInvariant();

    private static string Cut(string s, int max) => s.Length > max ? s[..max] : s;
}
