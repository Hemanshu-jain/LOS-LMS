using System.Text.Json;
using System.Text.RegularExpressions;
using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>One row of the RC check list the officer sees. State is ok, warn (amber, needs a look) or bad (blocks a Verified verdict).</summary>
public sealed record RcCheck(string Label, string State, string Detail);

/// <param name="Applicant">The applicant's name: the owner of a new vehicle, or compared with the seller on a used one.</param>
/// <param name="Lender">This NBFC's name, to tell "financed by us" from "financed by someone else".</param>
public sealed record RcInputs(string? RegNo, string? Chassis, string? Engine, string? InsuranceProvider, string? Applicant, string? Lender, DateOnly Today);

/// <summary>What the officer may take over from a verified RC. Only blanks are ever filled.</summary>
public sealed record RcFill(string? MakeModel, string? MfgYear);

public sealed record RcOutcome(KycOutcome Outcome, JsonElement? Response, RcFill? Fill);

public sealed record KybOutcome(IReadOnlyList<KycOutcome> Results, BusinessKycData? Data);

/// <summary>
/// Phase 4: Vehicle RC verification (02 section 2.1) and business KYB by GSTIN / CIN (02 section 7.1) on <see cref="DigioClient"/>.
/// Each writes <see cref="VerificationEvidence"/> and never invents a result. RC field names come from docs/digio/samples/vehicle-rc.json.
/// The GST sandbox sample only shows a "No records found" answer, so the success shape of GST and CIN is read defensively:
/// nothing is Verified unless an Active status is actually found in the answer.
/// </summary>
public sealed class DigioAsset(DigioClient digio, PasswordVault vault, IDbContextFactory<LosDbContext> dbFactory)
{
    private static readonly Regex Gstin = new("^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$", RegexOptions.Compiled);
    private static readonly Regex Cin = new("^[LU][0-9]{5}[A-Z]{2}[0-9]{4}[A-Z]{3}[0-9]{6}$", RegexOptions.Compiled);

    public Task<bool> RcEnabledAsync(int companyId) => EnabledAsync(companyId, s => s.Rc);

    // ---- Vehicle RC ----

    /// <summary>
    /// Verifies the registration number. The same registration, chassis and engine as a verdict already on file: no second call, the
    /// stored answer is re-checked against the current entries instead.
    /// </summary>
    public async Task<RcOutcome> VerifyRcAsync(int companyId, string applicationId, RcInputs inputs, string? userId)
    {
        if (!await EnabledAsync(companyId, s => s.Rc))
        {
            return new(KycOutcome.Off("Vehicle RC verification"), null, null);
        }

        var reg = Normalise(inputs.RegNo);
        if (reg.Length < 6)
        {
            return new(new(null, "Enter the registration number first."), null, null);
        }

        var fingerprint = DigioKyc.Fingerprint(reg, Normalise(inputs.Chassis), Normalise(inputs.Engine));
        if (await LatestEvidenceAsync(companyId, applicationId, "VEHICLE_RC") is { } prior && prior.InputFingerprint == fingerprint
            && prior.Verdict is "Verified" or "Mismatch" && await LatestResponseAsync(companyId, applicationId, "VEHICLE_RC_VERIFY") is { } stored)
        {
            return new(new(prior.Verdict, prior.Summary ?? "Already checked."), stored, FillFrom(stored));
        }

        if (!await VerificationLock.TryBeginAsync(dbFactory, companyId, applicationId, null, "VEHICLE_RC"))
        {
            return new(new(null, VerificationLock.Refusal), null, null); // nothing is sent to Digio
        }

        var requestId = Guid.NewGuid().ToString("N");
        // ponytail: not metered. The rate card has no vehicle-RC price yet (Digio's quote lacks one); add KYC_VEHICLE_RC when it is known.
        var r = await digio.SendAsync(new DigioCall(companyId, HttpMethod.Post, "/client/v4/apis/verify/vehicle_rc",
            new { id = reg, unique_request_id = requestId }, null, "RC", "VEHICLE_RC_VERIFY", applicationId, null, requestId, fingerprint, userId));
        if (Failure(r) is { } fail)
        {
            return new(fail, null, null);
        }

        if (r.Data is not { } body || !body.TryGetProperty("vehicle_details", out var vd) || vd.ValueKind != JsonValueKind.Object)
        {
            var none = await RecordAsync(companyId, applicationId, "VEHICLE_RC", "Unavailable", "Digio has no record for this registration number.", requestId, r, fingerprint);
            return new(none, null, null);
        }

        var checks = RcChecks.Evaluate(body, inputs);
        var bad = checks.Where(c => c.State == "bad").Select(c => c.Label).ToList();
        var warn = checks.Count(c => c.State == "warn");
        var summary = bad.Count == 0
            ? "RC checks passed" + (warn > 0 ? $"; {warn} to look at" : "")
            : "RC problems: " + string.Join(", ", bad);
        var outcome = await RecordAsync(companyId, applicationId, "VEHICLE_RC", bad.Count == 0 ? "Verified" : "Mismatch", summary, requestId, r, fingerprint);
        return new(outcome, body.Clone(), FillFrom(body));
    }

    /// <summary>The newest RC answer on file, for re-checking against the current entries without another call.</summary>
    public Task<JsonElement?> LatestRcResponseAsync(int companyId, string applicationId) => LatestResponseAsync(companyId, applicationId, "VEHICLE_RC_VERIFY");

    private static RcFill? FillFrom(JsonElement body)
    {
        if (!body.TryGetProperty("vehicle_details", out var vd))
        {
            return null;
        }

        var make = string.Join(' ', new[] { Str(vd, "maker_description"), Str(vd, "maker_model") }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        var year = Str(vd, "registration_date") is { Length: >= 4 } d && int.TryParse(d[..4], out _) ? d[..4] : null;
        return new(make.Length > 0 ? make : null, year);
    }

    // ---- Business KYB ----

    /// <summary>
    /// Checks the GSTIN and/or CIN against Digio. The firm name is compared with what the register says; a status that is not Active,
    /// or names that do not match, is a Mismatch, never a pass. Returns the register's name and incorporation date for blank-filling.
    /// </summary>
    public async Task<KybOutcome> VerifyBusinessAsync(int companyId, string applicationId, string? gstin, string? cin, string? firmName, string? userId)
    {
        if (!await EnabledAsync(companyId, s => s.Kyb))
        {
            return new([KycOutcome.Off("Business verification")], null);
        }

        gstin = gstin?.Trim().ToUpperInvariant();
        cin = cin?.Trim().ToUpperInvariant();
        var results = new List<KycOutcome>();
        BusinessKycData? data = null;

        if (!string.IsNullOrEmpty(gstin))
        {
            if (!Gstin.IsMatch(gstin))
            {
                results.Add(new(null, "The GSTIN is not in the right format (15 characters)."));
            }
            else
            {
                var (o, d) = await LookupAsync(companyId, applicationId, "GST", "GSTIN", gstin, firmName, userId,
                    names: ["legal_name", "legal_name_of_business", "lgnm", "trade_name", "tradeNam", "business_name"],
                    status: ["gstin_status", "status", "sts"], date: []); // the GST registration date is not the incorporation date
                results.Add(o);
                data ??= d;
            }
        }

        if (!string.IsNullOrEmpty(cin))
        {
            if (!Cin.IsMatch(cin))
            {
                results.Add(new(null, "The CIN is not in the right format (21 characters)."));
            }
            else
            {
                var (o, d) = await LookupAsync(companyId, applicationId, "CIN", "CIN", cin, firmName, userId,
                    names: ["company_name", "name", "legal_name"], status: ["company_status", "status", "active_compliance"],
                    date: ["date_of_incorporation", "incorporation_date", "registration_date"]);
                results.Add(o);
                data = d ?? data;
            }
        }

        if (results.Count == 0)
        {
            results.Add(new(null, "Enter the firm's GSTIN or CIN first."));
        }

        return new(results, data);
    }

    private async Task<(KycOutcome, BusinessKycData?)> LookupAsync(
        int companyId, string applicationId, string type, string subject, string idNo, string? firmName, string? userId,
        string[] names, string[] status, string[] date)
    {
        var fingerprint = DigioKyc.Fingerprint(idNo, firmName?.Trim().ToUpperInvariant());
        if (!await VerificationLock.TryBeginAsync(dbFactory, companyId, applicationId, null, subject))
        {
            return (new(null, $"{subject} {VerificationLock.Refusal[0..1].ToLowerInvariant()}{VerificationLock.Refusal[1..]}"), null);
        }

        var requestId = Guid.NewGuid().ToString("N");
        var r = await digio.SendAsync(new DigioCall(companyId, HttpMethod.Post, $"/v3/client/kyc/fetch_id_data/{type}",
            new { id_no = idNo, unique_request_id = requestId }, DigioRates.BusinessKyc, "KYB", type + "_VERIFY", applicationId, null, requestId, fingerprint, userId));
        if (Failure(r) is { } fail)
        {
            return (fail, null);
        }

        // 200 with an error_message ("No records found") is Digio's way of saying the id is not on the register.
        if (r.Str("error_message") is { Length: > 0 } err)
        {
            return (await RecordAsync(companyId, applicationId, subject, "Failed", $"{subject} not verified: {err}", requestId, r, fingerprint,
                $"{subject} was not found. Check the number."), null);
        }

        var body = r.Data ?? default;
        var registerName = First(body, names);
        var registerStatus = First(body, status);
        if (registerStatus is null)
        {
            // ponytail: the success shape is not in a sample yet; without a recognisable status nothing is called Verified.
            return (await RecordAsync(companyId, applicationId, subject, "Unavailable", $"Digio answered, but the {subject} status was not in a form we can read.", requestId, r, fingerprint), null);
        }

        var active = registerStatus.Equals("active", StringComparison.OrdinalIgnoreCase);
        var nameOk = string.IsNullOrWhiteSpace(firmName) || registerName is null || DigioKyc.NameScore(firmName, registerName) >= 60;
        var (verdict, summary) = !active ? ("Mismatch", $"{subject} status is {registerStatus}, not Active")
            : !nameOk ? ("Mismatch", $"{subject} is Active but registered under a different name")
            : ("Verified", $"{subject} is Active" + (registerName is null ? "" : "; name matches"));
        DateOnly? when = First(body, date) is { } ds && DateOnly.TryParse(ds, out var parsed) ? parsed : null;
        var data = new BusinessKycData(registerName, registerStatus, when, null);
        return (await RecordAsync(companyId, applicationId, subject, verdict, summary, requestId, r, fingerprint,
            verdict == "Mismatch" && registerName is not null ? summary + $" (register: {registerName})" : null), data);
    }

    // ---- shared ----

    private async Task<KycOutcome> RecordAsync(int companyId, string applicationId, string subject, string verdict, string summary, string requestId, DigioResult r, string fingerprint, string? message = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.VerificationEvidence.Add(new VerificationEvidence
        {
            CompanyId = companyId, ApplicationId = applicationId, Subject = subject, Verdict = verdict, Summary = summary.Length > 300 ? summary[..300] : summary,
            DigioRequestId = requestId, Environment = r.Environment ?? "Sandbox", InputFingerprint = fingerprint, CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return new(verdict, message ?? summary, RequestId: requestId);
    }

    private async Task<VerificationEvidence?> LatestEvidenceAsync(int companyId, string applicationId, string subject)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.VerificationEvidence.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.ApplicationId == applicationId && e.Subject == subject)
            .OrderByDescending(e => e.Id).FirstOrDefaultAsync();
    }

    private async Task<JsonElement?> LatestResponseAsync(int companyId, string applicationId, string operation)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var sealedJson = await db.DigioRequests.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.ApplicationId == applicationId && x.Operation == operation && x.ResultJson != null)
            .OrderByDescending(x => x.RequestedAt).Select(x => x.ResultJson).FirstOrDefaultAsync();
        return sealedJson is null ? null : JsonDocument.Parse(vault.Unseal(sealedJson)).RootElement.Clone();
    }

    private async Task<bool> EnabledAsync(int companyId, Func<DigioSettings, bool> product)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var s = await db.DigioSettings.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId);
        return s is not null && product(s);
    }

    private static KycOutcome? Failure(DigioResult r)
    {
        if (r.Status == DigioStatus.Ok)
        {
            return null;
        }

        if (r.Status == DigioStatus.NotConfigured)
        {
            return new(null, "Digio keys are not saved for this company yet. Ask the system administrator.", RequestId: r.RequestId);
        }

        var notEnabled = r.HttpStatus is 403 or 404;
        return new(null, notEnabled ? "Not enabled on your Digio account. Ask Digio to switch it on." : r.Error ?? "Digio could not complete this.", NotEnabled: notEnabled, RequestId: r.RequestId);
    }

    internal static string Normalise(string? s) => new string((s ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    private static string? First(JsonElement body, string[] names) =>
        body.ValueKind == JsonValueKind.Object ? names.Select(n => Str(body, n)).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) : null;

    private static string? Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) ? v.ValueKind == JsonValueKind.String ? v.GetString() : v.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False ? v.ToString() : null : null;
}

/// <summary>The six RC checks (02 section 2.1), a pure function of Digio's answer and the entries, so changing an entry re-checks at once.</summary>
public static class RcChecks
{
    public static List<RcCheck> Evaluate(JsonElement body, RcInputs i)
    {
        var vd = Obj(body, "vehicle_details");
        var user = Obj(body, "user_details");
        var fin = Obj(body, "finance_details");
        var permit = Obj(body, "permit_details");
        var rows = new List<RcCheck>();

        var returnedReg = DigioAsset.Normalise(S(vd, "rc_number"));
        if (returnedReg.Length > 0 && DigioAsset.Normalise(i.RegNo) != returnedReg)
        {
            rows.Add(new("Registration", "bad", $"Digio returned a different registration number ({S(vd, "rc_number")})."));
        }

        // 1. owner
        var owner = S(user, "owner_name");
        rows.Add(string.IsNullOrWhiteSpace(owner) ? new("Owner", "warn", "Digio gave no owner name.")
            : B(vd, "masked_name") ? new("Owner", "warn", "Digio masked the owner's name; confirm it on the paper RC.")
            : DigioKyc.NameScore(owner, i.Applicant) >= 70 ? new("Owner", "ok", "The registered owner matches the applicant.")
            : new("Owner", "warn", $"Registered owner is {owner}, not the applicant. Expected on a used vehicle: confirm they are the seller."));

        // 2 + 3. chassis and engine (last 5 characters, because Digio may mask the rest)
        rows.Add(Tail("Chassis", i.Chassis, S(vd, "vehicle_chassis_number")));
        rows.Add(Tail("Engine", i.Engine, S(vd, "vehicle_engine_number")));

        // 4. existing hypothecation
        var financier = S(fin, "financier");
        rows.Add(!B(fin, "financed") ? new("Hypothecation", "ok", "No existing loan on this vehicle.")
            : !string.IsNullOrWhiteSpace(i.Lender) && DigioKyc.NameScore(financier, i.Lender) >= 60 ? new("Hypothecation", "ok", "Financed by this lender.")
            : new("Hypothecation", "bad", $"Already financed by {(string.IsNullOrWhiteSpace(financier) ? "another lender" : financier)}. Needs an NOC or closure of that loan."));

        // 5. insurance
        if (!DateOnly.TryParse(S(fin, "insurance_upto"), out var insured))
        {
            rows.Add(new("Insurance", "warn", "Digio gave no insurance expiry."));
        }
        else if (insured < i.Today)
        {
            rows.Add(new("Insurance", "bad", $"Insurance expired on {insured:d MMM yyyy}."));
        }
        else
        {
            var insurer = S(fin, "insurance_company");
            rows.Add(!string.IsNullOrWhiteSpace(i.InsuranceProvider) && !string.IsNullOrWhiteSpace(insurer) && DigioKyc.NameScore(insurer, i.InsuranceProvider) < 50
                ? new("Insurance", "warn", $"Valid till {insured:d MMM yyyy}, but the insurer on the RC is {insurer}, not {i.InsuranceProvider}.")
                : new("Insurance", "ok", $"Valid till {insured:d MMM yyyy}."));
        }

        // 6. RC status, blacklist, challans
        var status = S(vd, "rc_status");
        var blacklist = S(permit, "blacklist_status");
        rows.Add(string.IsNullOrWhiteSpace(status) ? new("RC status", "warn", "Digio gave no RC status.")
            : !status.StartsWith("ACTIVE", StringComparison.OrdinalIgnoreCase) ? new("RC status", "bad", $"RC status is {status}.")
            : blacklist is { Length: > 0 } && blacklist.Contains("BLACKLIST", StringComparison.OrdinalIgnoreCase) && !blacklist.Contains("NOT", StringComparison.OrdinalIgnoreCase)
                ? new("RC status", "bad", "The vehicle is blacklisted.")
            : new("RC status", "ok", "RC is active."));
        if (vd.ValueKind == JsonValueKind.Object && vd.TryGetProperty("challan_details", out var ch)
            && ch.ValueKind == JsonValueKind.Array && ch.GetArrayLength() > 0)
        {
            rows.Add(new("Challans", "warn", "Pending challans are on record for this vehicle."));
        }

        // fitness (commercial vehicles)
        rows.Add(DateOnly.TryParse(S(vd, "fit_up_to"), out var fit)
            ? fit >= i.Today ? new("Fitness", "ok", $"Fit till {fit:d MMM yyyy}.") : new("Fitness", "warn", $"Fitness expired on {fit:d MMM yyyy}.")
            : new("Fitness", "warn", "Digio gave no fitness date."));
        return rows;
    }

    private static RcCheck Tail(string label, string? entered, string? returned)
    {
        var a = DigioAsset.Normalise(entered);
        var b = DigioAsset.Normalise(returned);
        if (b.Length == 0)
        {
            return new(label, "warn", $"Digio gave no {label.ToLowerInvariant()} number.");
        }

        if (a.Length == 0)
        {
            return new(label, "warn", $"No {label.ToLowerInvariant()} number entered to compare.");
        }

        var n = Math.Min(5, Math.Min(a.Length, b.Length));
        return a[^n..] == b[^n..] ? new(label, "ok", $"{label} number matches.") : new(label, "bad", $"{label} number does not match the RC.");
    }

    private static JsonElement Obj(JsonElement e, string n) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(n, out var v) ? v : default;

    private static string? S(JsonElement e, string n) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static bool B(JsonElement e, string n) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(n, out var v) && (v.ValueKind == JsonValueKind.True
            || v.ValueKind == JsonValueKind.String && bool.TryParse(v.GetString(), out var b) && b);
}
