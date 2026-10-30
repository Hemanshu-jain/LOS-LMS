using System.Collections.Concurrent;
using System.Text.Json;
using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// The e-NACH state machine (06-NACH.md section 5). States only move forward: Requested -> AuthSuccess -> InBankProcessing -> Registered.
/// Rejected, Expired and Cancelled are final; Registered can only become Cancelled. AuthFailed can be followed by a successful retry.
/// Digio's GET mandate answer is the source of truth, so a replayed or out-of-order webhook can never move a mandate backwards.
/// </summary>
public static class NachLifecycle
{
    public const string Requested = "Requested", AuthSuccess = "AuthSuccess", InBank = "InBankProcessing", Registered = "Registered",
        AuthFailed = "AuthFailed", Rejected = "Rejected", Expired = "Expired", Cancelled = "Cancelled";

    private static int Rank(string? s) => s switch { Requested => 1, AuthFailed => 1, AuthSuccess => 2, InBank => 3, Registered => 4, _ => 0 };

    public static bool IsLive(string? s) => s is Requested or AuthFailed or AuthSuccess or InBank or Registered;

    public static bool IsFinal(string? s) => s is Registered or Rejected or Expired or Cancelled;

    /// <summary>Digio's <c>state</c> + <c>bank_details.state</c> (either case) to our state. The bank state wins when it says more.</summary>
    public static string Map(string? state, string? bankState)
    {
        var bank = bankState?.Trim().ToLowerInvariant();
        var top = state?.Trim().ToLowerInvariant();
        return bank switch
        {
            "register_success" => Registered,
            "register_failed" or "nack_received" or "reject_spo_bank" or "transfer_failed" => Rejected,
            "revoked" => Cancelled,
            "transfer_success" or "accepted_spo_bank" or "awaiting_ack" or "ack_received" or "awaiting_res" => InBank,
            _ => top switch
            {
                "auth_success" or "signed" => AuthSuccess,
                "auth_failed" => AuthFailed,
                "expired" => Expired,
                "cancelled" or "revoked" => Cancelled,
                _ => Requested,
            },
        };
    }

    public static string Next(string? current, string incoming)
    {
        if (current is Rejected or Expired or Cancelled)
        {
            return current;
        }

        if (current == Registered)
        {
            return incoming == Cancelled ? Cancelled : Registered;
        }

        if (incoming is Rejected or Expired or Cancelled)
        {
            return incoming;
        }

        return Rank(incoming) > Rank(current) || current is null ? incoming
            : incoming == AuthFailed && Rank(current) <= 1 ? AuthFailed : current;
    }

    /// <summary>The Pending / Registered / Rejected the existing screens understand.</summary>
    public static string StatusFor(string state) => state switch { Registered => "Registered", Rejected or Cancelled => "Rejected", _ => "Pending" };

    /// <summary>The DigioRequest status for the same state.</summary>
    public static string RequestStatus(string state) => state switch
    {
        Registered => "Succeeded", Rejected => "Rejected", Expired => "Expired", Cancelled => "Cancelled", AuthFailed => "Failed",
        AuthSuccess or InBank => "Processing", _ => "WaitingForCustomer",
    };

    /// <summary>Applies Digio's mandate details to the row. UMRN is written only when Registered, and only from Digio. True when anything changed.</summary>
    public static bool Apply(INachMandate m, JsonElement d, DateTime nowUtc)
    {
        var bank = d.TryGetProperty("bank_details", out var b) && b.ValueKind == JsonValueKind.Object ? Str(b, "state") : null;
        var next = Next(m.DigioState, Map(Str(d, "state"), bank));
        var before = (m.DigioState, m.BankState, m.Umrn, m.RejectReason);

        m.DigioState = next;
        m.BankState = Cut(bank, 30);
        m.Status = StatusFor(next);
        if (next == Registered)
        {
            m.Umrn = Cut(Str(d, "umrn"), 40) ?? m.Umrn;
            m.RegisteredAt ??= nowUtc;
            m.RejectReason = null;
        }
        else if (next is Rejected or AuthFailed)
        {
            m.RejectReason = Cut(Str(d, "npci_auth_reject_reason") ?? Str(d, "npci_auth_failed_error") ?? m.RejectReason, 300);
        }

        return before != (m.DigioState, m.BankState, m.Umrn, m.RejectReason);
    }

    private static string? Str(JsonElement e, string n) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string? Cut(string? s, int max) => s is { Length: > 0 } ? (s.Length > max ? s[..max] : s) : null;
}

public sealed record NachBank(string Id, string Name, string IfscPrefix, bool NetBanking, bool DebitCard, bool Aadhaar, bool Esign, bool Physical)
{
    public bool Supports(string route) => route switch { "API" => NetBanking || DebitCard || Aadhaar, "ESIGN" => Esign, "PHYSICAL" => Physical, _ => false };

    public string Modes => string.Join(" ", new[] { NetBanking ? "Net banking" : null, DebitCard ? "Debit card" : null, Aadhaar ? "Aadhaar" : null, Esign ? "eSign" : null, Physical ? "Physical" : null }.Where(x => x is not null));
}

public sealed record NachTerms(decimal Emi, DateOnly FirstCollection, DateOnly LastEmi);

public sealed record NachResult(bool Ok, string? Error, string? AuthUrl = null, bool Existing = false);

/// <summary>DigiCollect e-NACH for the E-Nach and Security-Nach tabs (06-NACH.md). Every call goes through DigioClient.SendAsync.</summary>
public sealed class DigioNachService(
    IDbContextFactory<LosDbContext> dbFactory, DigioClient digio, IFileStore files, PasswordVault vault, DigioStatusNotifier notifier, ApiBillingService billing)
{
    public const string EnachTab = "Enach", SecurityTab = "Security";
    public static readonly string[] Routes = ["API", "ESIGN", "PHYSICAL"];

    // ponytail: process-local daily cache, one list per company and mode; swap for IMemoryCache if banks ever need invalidating.
    private static readonly ConcurrentDictionary<string, (DateOnly Day, List<NachBank> Banks)> BankCache = new();

    public async Task<(List<NachBank>? Banks, string? Error)> BanksAsync(int companyId, string mode = "API")
    {
        var key = $"{companyId}:{mode}";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (BankCache.TryGetValue(key, out var hit) && hit.Day == today)
        {
            return (hit.Banks, null);
        }

        var r = await digio.GetAsync(companyId, null, "/v3/client/mandate/banks?mode=" + Uri.EscapeDataString(mode), null);
        if (r.Status != DigioStatus.Ok)
        {
            return (null, r.Status == DigioStatus.NotConfigured ? "Digio is not configured for this company." : r.Error ?? "Could not load the bank list.");
        }

        var banks = new List<NachBank>();
        if (r.Data is { ValueKind: JsonValueKind.Array } arr)
        {
            foreach (var b in arr.EnumerateArray())
            {
                banks.Add(new(Str(b, "id") ?? "", Str(b, "name") ?? "", (Str(b, "ifscPrefix") ?? "").ToUpperInvariant(),
                    Flag(b, "apiMandateNetBanking"), Flag(b, "apiMandateDebitCard"), Flag(b, "apiMandateAadhaarCard"),
                    Flag(b, "esignMandate"), Flag(b, "physicalMandate")));
            }
        }

        BankCache[key] = (today, banks);
        return (banks, null);
    }

    public static NachBank? BankFor(IEnumerable<NachBank> banks, string? ifsc) =>
        ifsc is { Length: >= 4 } ? banks.FirstOrDefault(b => b.IfscPrefix == ifsc[..4].ToUpperInvariant()) : null;

    /// <summary>The maximum amount: EMI x multiplier rounded up to Rs 100, never under the EMI.</summary>
    public static decimal MaxAmount(decimal emi, decimal multiplier) => Math.Max(emi, Math.Ceiling(emi * multiplier / 100m) * 100m);

    public async Task<NachResult> CreateAsync(
        int companyId, string applicationId, string tab, string? userId, string route, bool onDevice,
        NachTerms terms, string customerName, string? mobile, string? email, string? requestId = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await FindAsync(db, companyId, applicationId, tab);
        if (row is null)
        {
            return new(false, "Save the mandate details first.");
        }

        if (row.DigioMandateId is not null && NachLifecycle.IsLive(row.DigioState))
        {
            return new(true, null, Existing: true); // idempotent: one live request per tab; "Try again" only after AuthFailed / Rejected / Expired
        }

        var settings = await db.DigioSettings.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(s => s.CompanyId == companyId);
        var creds = digio.Credentials(companyId);
        if (settings is not { Enach: true } || creds is null)
        {
            return new(false, "e-NACH is not enabled for this company.");
        }

        var configId = creds.Production ? settings.EnachConfigIdProduction : settings.EnachConfigIdSandbox;
        if (string.IsNullOrWhiteSpace(configId))
        {
            return new(false, $"The NACH config id for {DigioEndpoints.Environment(creds.Production)} is not set. Ask your vendor.");
        }

        var (banks, bankError) = await BanksAsync(companyId, route);
        if (banks is null)
        {
            return new(false, bankError);
        }

        var bank = BankFor(banks, row.Ifsc);
        var contact = Blank(mobile) ?? Blank(email);
        var block =
            !Routes.Contains(route) ? "Choose how the customer will authenticate."
            : string.IsNullOrWhiteSpace(row.AccountNumber) || string.IsNullOrWhiteSpace(row.Ifsc) ? "Enter the account number and IFSC."
            : !row.ConfirmationAccepted ? "Tick the confirmation first."
            : bank is null ? "This bank is not live for e-NACH."
            : !bank.Supports(route) ? $"This bank does not support the chosen route (it supports: {bank.Modes})."
            : terms.Emi <= 0 ? "The EMI is not known yet. Complete the repayment schedule."
            : terms.FirstCollection <= TodayIst() ? "The first EMI date must be tomorrow or later."
            : string.IsNullOrWhiteSpace(customerName) ? "The account holder name is missing."
            : contact is null ? "The customer has no mobile or email on Customer Details."
            : null;
        if (block is not null)
        {
            return new(false, block);
        }

        var id = requestId ?? Guid.NewGuid().ToString("N");
        var max = MaxAmount(terms.Emi, settings.NachMaxMultiplier);
        var final = terms.LastEmi.AddMonths(settings.NachFinalBufferMonths);
        // ponytail: date format yyyy-MM-dd and customer_ref_number = our 32-char id are assumptions until the Phase 0 sandbox sample (06 section 9).
        var body = new Dictionary<string, object?>
        {
            ["corporate_config_id"] = configId.Trim(), ["mandate_type"] = "CREATE", ["auth_mode"] = route,
            ["customer_identifier"] = contact, ["notify_customer"] = !onDevice, ["include_authentication_url"] = true,
            ["expire_in_days"] = settings.LinkExpiryDays,
            ["mandate_data"] = new Dictionary<string, object?>
            {
                ["customer_name"] = customerName.Trim(), ["customer_account_number"] = row.AccountNumber!.Trim(),
                ["customer_account_type"] = "SAVINGS", ["destination_bank_id"] = bank!.Id, ["customer_ref_number"] = id,
                ["scheme_ref_number"] = applicationId, ["management_category"] = settings.NachCategory, ["is_recurring"] = true,
                ["frequency"] = settings.NachFrequency, ["first_collection_date"] = terms.FirstCollection.ToString("yyyy-MM-dd"),
                ["final_collection_date"] = final.ToString("yyyy-MM-dd"), ["collection_amount"] = terms.Emi, ["maximum_amount"] = max,
                ["customer_mobile"] = Blank(mobile), ["customer_email"] = Blank(email),
            },
        };

        var result = await digio.SendAsync(new DigioCall(companyId, HttpMethod.Post, "/v3/client/mandate/create_form", body,
            Product: "MANDATE", Operation: tab == EnachTab ? "ENACH_CREATE" : "SECURITY_NACH_CREATE", ApplicationId: applicationId,
            RequestId: id, RequestedByUserId: userId, SuccessStatus: "WaitingForCustomer"));
        if (result.Status != DigioStatus.Ok || result.Str("id") is not { } ena)
        {
            return new(false, result.Status == DigioStatus.NotConfigured ? "Digio is not configured for this company." : MandateError(result));
        }

        row.DigioMandateId = ena;
        row.DigioRequestId = id;
        row.AuthMode = route;
        row.DestinationBankId = bank.Id;
        row.CollectionAmount = terms.Emi;
        row.MaximumAmount = max;
        row.DigioState = NachLifecycle.Requested;
        row.BankState = null;
        row.RejectReason = null;
        row.Status = "Pending";
        row.Umrn = null;
        row.RegisteredAt = null;
        row.MandateType = route == "PHYSICAL" ? "Physical" : "Digital";
        var req = await db.DigioRequests.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId);
        if (req is not null)
        {
            req.ExpiresAt = DateTime.UtcNow.AddDays(settings.LinkExpiryDays);
            req.CustomerChannel = onDevice ? "OnDevice" : "Link";
            req.NotifiedTo = onDevice ? null : DigioEsignService.Mask(contact!);
        }

        await db.SaveChangesAsync();
        await notifier.NotifyAsync(applicationId);
        return new(true, null, result.Str("authentication_url"));
    }

    /// <summary>The customer's authentication link for the live request, read back from the sealed Digio answer (never stored in the clear).</summary>
    public async Task<string?> AuthUrlAsync(int companyId, string applicationId, string tab)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await FindAsync(db, companyId, applicationId, tab);
        if (row?.DigioRequestId is not { } id)
        {
            return null;
        }

        var sealedJson = await db.DigioRequests.IgnoreQueryFilters().Where(r => r.Id == id && r.CompanyId == companyId).Select(r => r.ResultJson).FirstOrDefaultAsync();
        if (sealedJson is null)
        {
            return null;
        }

        using var doc = JsonDocument.Parse(vault.Unseal(sealedJson));
        return doc.RootElement.TryGetProperty("authentication_url", out var u) && u.ValueKind == JsonValueKind.String ? u.GetString() : null;
    }

    /// <summary>GET /v3/client/mandate/{id} is the source of truth. Applies it through the forward-only machine; returns true when Digio answered.</summary>
    public async Task<bool> ConfirmAsync(int companyId, string mandateId)
    {
        var r = await digio.GetAsync(companyId, null, $"/v3/client/mandate/{Uri.EscapeDataString(mandateId)}", null);
        if (r.Status != DigioStatus.Ok || r.Data is not { } details)
        {
            return false;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        string? application = null;
        var justRegistered = new List<(string Application, string? AuthMode)>();
        foreach (var m in await MandatesAsync(db, companyId, mandateId))
        {
            var was = m.DigioState;
            if (NachLifecycle.Apply(m, details, DateTime.UtcNow))
            {
                if (was != NachLifecycle.Registered && m.DigioState == NachLifecycle.Registered)
                {
                    justRegistered.Add((m switch { EnachMandate e => e.ApplicationId, SecurityNachMandate s => s.ApplicationId, _ => "" }, m.AuthMode));
                }

                application = m switch { EnachMandate e => e.ApplicationId, SecurityNachMandate s => s.ApplicationId, _ => null };
                if (m.DigioRequestId is { } rid)
                {
                    var status = NachLifecycle.RequestStatus(m.DigioState!);
                    var done = NachLifecycle.IsFinal(m.DigioState);
                    await db.DigioRequests.IgnoreQueryFilters().Where(x => x.Id == rid && x.CompanyId == companyId)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, status).SetProperty(x => x.ProviderStatus, m.BankState ?? m.DigioState)
                            .SetProperty(x => x.CompletedAt, done ? DateTime.UtcNow : (DateTime?)null));
                }
            }
        }

        await db.SaveChangesAsync();

        // Metered once, when the mandate is registered; the code follows how it was authorised (once per mandate id, so a replay adds none).
        foreach (var (app, mode) in justRegistered)
        {
            var code = mode switch { "ESIGN" => DigioRates.MandateEsign, "PHYSICAL" => DigioRates.MandatePhysical, _ => DigioRates.MandateApi };
            await billing.RecordOnceAsync(companyId, code, mandateId, app);
        }

        if (application is not null)
        {
            await notifier.NotifyAsync(application);
        }

        return true;
    }

    /// <summary>Physical NACH: the prefilled form to print (POST generate_file), kept with the file.</summary>
    public async Task<NachResult> GenerateFormAsync(int companyId, string applicationId, string tab)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await FindAsync(db, companyId, applicationId, tab);
        if (row?.DigioMandateId is not { } ena || row.AuthMode != "PHYSICAL")
        {
            return new(false, "Create a physical mandate request first.");
        }

        var r = await digio.SendAsync(new DigioCall(companyId, HttpMethod.Post, $"/v3/client/mandate/{Uri.EscapeDataString(ena)}/generate_file",
            ApplicationId: applicationId, ExpectBinary: true));
        if (r.Bytes is not { } bytes || !FileSignature.Matches(".pdf", bytes))
        {
            return new(false, r.Error ?? "Digio did not return the form.");
        }

        row.SignedFormPath = FileKey.Normalize(await files.SaveUploadAsync(applicationId, "nach", $"form-{ena}.pdf", bytes));
        await db.SaveChangesAsync();
        return new(true, null);
    }

    /// <summary>Physical NACH: the officer's scan of the signed form (POST submit_signed_mandate_raw). Status still comes only from Digio.</summary>
    public async Task<NachResult> SubmitSignedAsync(int companyId, string applicationId, string tab, byte[] scan)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await FindAsync(db, companyId, applicationId, tab);
        if (row?.DigioMandateId is not { } ena || row.AuthMode != "PHYSICAL")
        {
            return new(false, "Create a physical mandate request first.");
        }

        var r = await digio.SendAsync(new DigioCall(companyId, HttpMethod.Post, "/v3/client/mandate/submit_signed_mandate_raw",
            new { id = ena, file_data = Convert.ToBase64String(scan) }, ApplicationId: applicationId));
        return r.Status == DigioStatus.Ok && r.Bool("success") != false ? new(true, null) : new(false, r.Error ?? "Digio did not accept the scan.");
    }

    /// <summary>Webhook entry: any mandate event just triggers the confirming GET. The event name and payload shape are not needed.</summary>
    public static bool IsMandateEvent(string? evt) =>
        evt is not null && (evt.StartsWith("apimndt.", StringComparison.OrdinalIgnoreCase) || evt.StartsWith("mndt.", StringComparison.OrdinalIgnoreCase));

    /// <summary>The ENA id in a webhook body, wherever the payload nests it (the sample is not captured yet): the first object with an "id" starting ENA.</summary>
    public static string? MandateIdIn(JsonElement root)
    {
        if (!root.TryGetProperty("payload", out var payload) || payload.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var p in payload.EnumerateObject())
        {
            if (p.Value.ValueKind == JsonValueKind.Object && Str(p.Value, "id") is { } id && id.StartsWith("ENA", StringComparison.OrdinalIgnoreCase))
            {
                return id;
            }
        }

        return Str(payload, "id") is { } top && top.StartsWith("ENA", StringComparison.OrdinalIgnoreCase) ? top : null;
    }

    /// <summary>Non-final mandates for the reconciler.</summary>
    public async Task<List<(int CompanyId, string MandateId)>> OpenMandatesAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var e = await db.EnachMandates.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.DigioMandateId != null && (m.DigioState == "Requested" || m.DigioState == "AuthFailed" || m.DigioState == "AuthSuccess" || m.DigioState == "InBankProcessing"))
            .Select(m => new { m.Application!.CompanyId, Id = m.DigioMandateId! }).ToListAsync();
        var s = await db.SecurityNachMandates.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.DigioMandateId != null && (m.DigioState == "Requested" || m.DigioState == "AuthFailed" || m.DigioState == "AuthSuccess" || m.DigioState == "InBankProcessing"))
            .Select(m => new { m.Application!.CompanyId, Id = m.DigioMandateId! }).ToListAsync();
        return e.Concat(s).Select(x => (x.CompanyId, x.Id)).ToList();
    }

    // ---- helpers ----

    private static async Task<INachMandate?> FindAsync(LosDbContext db, int companyId, string applicationId, string tab) => tab == EnachTab
        ? await db.EnachMandates.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.ApplicationId == applicationId && m.Application!.CompanyId == companyId)
        : await db.SecurityNachMandates.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.ApplicationId == applicationId && m.Application!.CompanyId == companyId);

    private static async Task<List<INachMandate>> MandatesAsync(LosDbContext db, int companyId, string mandateId)
    {
        var list = new List<INachMandate>();
        list.AddRange(await db.EnachMandates.IgnoreQueryFilters().Where(m => m.DigioMandateId == mandateId && m.Application!.CompanyId == companyId).ToListAsync());
        list.AddRange(await db.SecurityNachMandates.IgnoreQueryFilters().Where(m => m.DigioMandateId == mandateId && m.Application!.CompanyId == companyId).ToListAsync());
        return list;
    }

    private static DateOnly TodayIst() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5.5));

    private static string MandateError(DigioResult r) => r.HttpStatus == 400 && r.ErrorCode is null
        ? "Digio rejected the mandate. Check the NACH config id, account details and dates."
        : r.Error ?? "Digio did not create the mandate.";

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string? Str(JsonElement e, string n) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static bool Flag(JsonElement e, string n) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.True;
}
