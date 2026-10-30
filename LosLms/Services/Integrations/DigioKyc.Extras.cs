using System.Text.Json;
using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>Phase 6 checks that are priced in Digio's quote: UPI ID (VPA) verification and UAN / employment verification.</summary>
public sealed partial class DigioKyc
{
    // ---- UPI ID (VPA), Bank & Financial: KYC_UPI_VPA ----

    /// <summary>Checks that a UPI id exists and belongs to the account holder. Same inputs as a verdict already on file: no second call, no second charge.</summary>
    public async Task<KycOutcome> VerifyVpaAsync(int companyId, string applicationId, string? vpa, string? holderName, string? userId)
    {
        if (!await EnabledAsync(companyId, s => s.UpiVpa))
        {
            return KycOutcome.Off("UPI ID verification");
        }

        vpa = vpa?.Trim().ToLowerInvariant();
        if (vpa is null || !System.Text.RegularExpressions.Regex.IsMatch(vpa, @"^[a-z0-9.\-_]{2,256}@[a-z][a-z0-9]{1,64}$") || string.IsNullOrWhiteSpace(holderName))
        {
            return new(null, "Enter a valid UPI id (name@bank) and the account holder's name first.");
        }

        var fingerprint = Fingerprint(vpa, holderName.Trim().ToUpperInvariant());
        if (await LatestForApplicationAsync(companyId, applicationId, "UPI_VPA") is { } prior && prior.InputFingerprint == fingerprint && prior.Verdict is "Verified" or "Mismatch" or "Failed")
        {
            return new(prior.Verdict, prior.Summary ?? "Already checked.");
        }

        // TODO(VerificationLock): call VerificationLock.TryBeginAsync(...) here, before the send, once ops/reverify-lock is on main.
        var requestId = Guid.NewGuid().ToString("N");
        var r = await digio.SendAsync(new DigioCall(companyId, HttpMethod.Post, "/v3/client/public/upi/check_vpa",
            new { reference_id = requestId, unique_request_id = requestId, virtual_address = vpa, name = holderName.Trim() },
            DigioRates.UpiVpaVerify, "BANK", "UPI_VPA_VERIFY", applicationId, null, requestId, fingerprint, userId));
        await StampConsentAsync(requestId);
        if (Failure(r) is { } fail)
        {
            return fail;
        }

        var status = r.Str("status")?.ToLowerInvariant();
        if (status is null)
        {
            return await RecordApplicationEvidenceAsync(companyId, applicationId, null, "UPI_VPA", "Unavailable", FieldsNote("Digio's reply had no UPI status.", r), requestId, r, fingerprint);
        }

        var score = r.Data is { ValueKind: JsonValueKind.Object } d && d.TryGetProperty("fuzzy_match_score", out var fs) && fs.TryGetInt32(out var fv) ? fv : (int?)null;
        var name = r.Str("customer_name");
        var matched = score ?? (name is null ? 0 : NameScore(name, holderName));
        var (verdict, summary) = status switch
        {
            "available" when matched >= 70 => ("Verified", $"UPI id exists; holder name match {matched}"),
            "available" => ("Mismatch", $"UPI id exists but the holder name does not match (score {matched})"),
            _ => ("Failed", "UPI id not verified: " + Cut(r.Str("status_description") ?? status, 150)),
        };
        return await RecordApplicationEvidenceAsync(companyId, applicationId, null, "UPI_VPA", verdict, summary, requestId, r, fingerprint, matched);
    }

    // ---- UAN / employment, Employment section: KYC_UAN ----

    /// <summary>Looks a UAN up (Rs 15, so repeated clicks on the same UAN and name reuse the verdict). Fills a BLANK employer name from the current employment.</summary>
    public async Task<KycOutcome> VerifyUanAsync(int companyId, string applicationId, int partyId, string? uan, string? name, string? userId)
    {
        if (!await EnabledAsync(companyId, s => s.Uan))
        {
            return KycOutcome.Off("UAN verification");
        }

        uan = uan?.Trim();
        if (uan is null || !System.Text.RegularExpressions.Regex.IsMatch(uan, "^[0-9]{12}$") || string.IsNullOrWhiteSpace(name))
        {
            return new(null, "Enter the 12-digit UAN and the full name first.");
        }

        var fingerprint = Fingerprint(uan, name.Trim().ToUpperInvariant());
        if (await LatestAsync(companyId, applicationId, partyId, "UAN") is { } prior && prior.InputFingerprint == fingerprint && prior.Verdict is "Verified" or "Mismatch" or "Failed")
        {
            return new(prior.Verdict, prior.Summary ?? "Already checked.");
        }

        // TODO(VerificationLock): call VerificationLock.TryBeginAsync(...) here, before the send, once ops/reverify-lock is on main.
        var requestId = Guid.NewGuid().ToString("N");
        var r = await digio.SendAsync(new DigioCall(companyId, HttpMethod.Post, "/client/v4/apis/verify/uan",
            new { type = "UAN", identifier = uan, uan_history = "YES", unique_request_id = requestId },
            DigioRates.UanVerify, "KYC_ID", "UAN_VERIFY", applicationId, partyId, requestId, fingerprint, userId));
        await StampConsentAsync(requestId);
        if (Failure(r) is { } fail)
        {
            return fail;
        }

        var history = r.Data is { ValueKind: JsonValueKind.Object } d && d.TryGetProperty("employment_history", out var h) && h.ValueKind == JsonValueKind.Array ? h.EnumerateArray().ToList() : [];
        if (r.Str("uan") is null)
        {
            return await RecordApplicationEvidenceAsync(companyId, applicationId, partyId, "UAN", "Unavailable", FieldsNote("Digio's reply had no UAN.", r), requestId, r, fingerprint);
        }

        if (history.Count == 0)
        {
            return await RecordApplicationEvidenceAsync(companyId, applicationId, partyId, "UAN", "Failed", "UAN found but it has no employment history.", requestId, r, fingerprint);
        }

        var memberName = history.Select(x => Str(x, "name")).FirstOrDefault(n => n is not null);
        var score = memberName is null ? 0 : NameScore(memberName, name);
        var current = history.FirstOrDefault(x => Str(x, "date_of_exit") is null) is var cur && cur.ValueKind == JsonValueKind.Object ? cur : history[^1];
        var employer = Str(current, "establishment_name");

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var party = await db.Parties.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == partyId && p.ApplicationId == applicationId);
            if (party is not null && string.IsNullOrWhiteSpace(party.EmployerName) && employer is not null)
            {
                party.EmployerName = Cut(employer, 200);
                await db.SaveChangesAsync();
            }
        }

        var (verdict, summary) = score >= 60
            ? ("Verified", $"UAN found, {history.Count} employment record(s); member name match {score}")
            : ("Mismatch", $"UAN found, {history.Count} employment record(s), but the member name does not match (score {score})");
        return await RecordApplicationEvidenceAsync(companyId, applicationId, partyId, "UAN", verdict, summary, requestId, r, fingerprint, score);
    }

    // ---- shared by the above ----

    private static string FieldsNote(string what, DigioResult r) =>
        Cut(what + " Fields: " + (r.Data is { ValueKind: JsonValueKind.Object } o ? string.Join(", ", o.EnumerateObject().Select(x => x.Name).Take(15)) : "not an object"), 300);

    private async Task<VerificationEvidence?> LatestForApplicationAsync(int companyId, string applicationId, string subject)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.VerificationEvidence.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.ApplicationId == applicationId && e.PartyId == null && e.Subject == subject)
            .OrderByDescending(e => e.Id).FirstOrDefaultAsync();
    }

    private async Task<KycOutcome> RecordApplicationEvidenceAsync(int companyId, string applicationId, int? partyId, string subject, string verdict, string summary, string requestId, DigioResult r, string fingerprint, decimal? score = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.VerificationEvidence.Add(new VerificationEvidence
        {
            CompanyId = companyId, ApplicationId = applicationId, PartyId = partyId, Subject = subject, Verdict = verdict, Score = score, Summary = Cut(summary, 300),
            DigioRequestId = requestId, Environment = r.Environment ?? "Sandbox", InputFingerprint = fingerprint, CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return new(verdict, summary, RequestId: requestId);
    }
}
