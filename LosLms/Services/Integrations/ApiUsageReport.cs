using System.Globalization;
using System.Text;
using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>One provider call as the "stamp card" shows it. <see cref="Amount"/> is what was charged (0 for a failed call).</summary>
/// <param name="What">The specific name of the call ("Aadhaar via DigiLocker").</param>
/// <param name="Group">What the per-application breakdown calls it (a DigiLocker request that fetched both documents reads "Aadhaar + PAN via DigiLocker (2 documents)").</param>
public sealed record UsageEntry(
    long Id, DateTime At, int CompanyId, string? ApplicationId, string? Party, string What, string Group, string? User, decimal Amount, bool Success, string ApiCode);

/// <summary>One application's usage in the month; <see cref="ApplicationId"/> is null for calls made for no application.</summary>
public sealed record AppUsage(string? ApplicationId, decimal Total, IReadOnlyList<UsageEntry> Entries);

/// <summary>
/// The detailed side of the API usage page: every call of a month with its application, party, user and a readable name, and the same
/// numbers grouped by application. The names come from the usage row's code, its provider reference (matched to the Digio request, signer or
/// debit it paid for) and the request's operation. The totals are the same successful-call rates the monthly summary adds up.
/// </summary>
public sealed class ApiUsageReport(IDbContextFactory<LosDbContext> dbFactory)
{
    private static readonly TimeSpan Ist = TimeSpan.FromMinutes(330);

    public async Task<IReadOnlyList<UsageEntry>> EntriesAsync(int? companyId, DateOnly month)
    {
        var first = new DateOnly(month.Year, month.Month, 1);
        var from = DateTime.SpecifyKind(first.ToDateTime(TimeOnly.MinValue) - Ist, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(first.AddMonths(1).ToDateTime(TimeOnly.MinValue) - Ist, DateTimeKind.Utc);

        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.ApiUsageLogs.IgnoreQueryFilters().AsNoTracking().Where(l => l.CreatedAt >= from && l.CreatedAt < to);
        if (companyId is { } id)
        {
            q = q.Where(l => l.CompanyId == id);
        }

        var logs = await q.OrderBy(l => l.CreatedAt).ThenBy(l => l.Id).ToListAsync();
        if (logs.Count == 0)
        {
            return [];
        }

        var companies = logs.Select(l => l.CompanyId).Distinct().ToList();
        var lefts = logs.Select(l => Left(l.ProviderRef)).Where(r => r is not null).Select(r => r!).Distinct().ToList();
        var requests = await db.DigioRequests.IgnoreQueryFilters().AsNoTracking()
            .Where(r => companies.Contains(r.CompanyId) && ((r.DigioId != null && lefts.Contains(r.DigioId)) || (r.Status == "Failed" && r.RequestedAt >= from && r.RequestedAt < to)))
            .ToListAsync();
        var byDigioId = requests.Where(r => r.DigioId != null).GroupBy(r => (r.CompanyId, r.DigioId!)).ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.RequestedAt).First());

        // A failed call has no provider id: pair it with the nearest earlier unmatched failed request for the same file.
        var claimed = new HashSet<string>();
        var rates = await db.ApiRates.AsNoTracking().ToDictionaryAsync(r => r.Code, r => r.Name);
        var appIds = logs.Select(l => l.ApplicationId).Where(a => a is not null).Distinct().ToList();
        var parties = await db.Parties.IgnoreQueryFilters().AsNoTracking()
            .Where(p => appIds.Contains(p.ApplicationId)).ToDictionaryAsync(p => p.Id, p => p.FullName);
        var userIds = requests.Select(r => r.RequestedByUserId).Where(u => u is not null).Distinct().ToList();
        var users = await db.Users.IgnoreQueryFilters().AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName);
        var signers = await db.EsignSigners.IgnoreQueryFilters().AsNoTracking().Where(s => companies.Contains(s.CompanyId)).ToDictionaryAsync(s => s.Id.ToString(), s => s.Name);
        var presentations = await db.Presentations.IgnoreQueryFilters().AsNoTracking().Where(p => companies.Contains(p.CompanyId) && p.DigioPlanId != null)
            .Select(p => new { p.DigioPlanId, p.Attempt, p.InstalmentId }).ToListAsync();
        var instalmentNos = await db.Instalments.IgnoreQueryFilters().AsNoTracking().Where(i => companies.Contains(i.CompanyId)).ToDictionaryAsync(i => i.Id, i => i.No);

        var docs = logs.Where(l => l.ApiCode == DigioRates.DigiLockerFetch).GroupBy(l => (l.CompanyId, Left(l.ProviderRef)))
            .ToDictionary(g => g.Key, g => g.Select(l => Right(l.ProviderRef)?.ToLowerInvariant()).Distinct().ToList());

        var entries = new List<UsageEntry>();
        foreach (var l in logs)
        {
            DigioRequest? req = null;
            if (Left(l.ProviderRef) is { } left)
            {
                byDigioId.TryGetValue((l.CompanyId, left), out req);
            }
            else if (!l.IsSuccess)
            {
                req = requests.Where(r => r.CompanyId == l.CompanyId && r.ApplicationId == l.ApplicationId && r.Status == "Failed" && r.RequestedAt <= l.CreatedAt
                        && r.RequestedAt >= l.CreatedAt.AddMinutes(-2) && !claimed.Contains(r.Id)).OrderByDescending(r => r.RequestedAt).FirstOrDefault();
                if (req is not null)
                {
                    claimed.Add(req.Id);
                }
            }

            var right = Right(l.ProviderRef);
            var pres = l.ApiCode == DigioRates.MandateDebit ? presentations.FirstOrDefault(p => p.DigioPlanId == l.ProviderRef) : null;
            var what = Name(l.ApiCode, req?.Operation, right, signers, pres is null ? null : (pres.Attempt, instalmentNos.GetValueOrDefault(pres.InstalmentId)), rates);
            var group = l.ApiCode == DigioRates.DigiLockerFetch && docs.GetValueOrDefault((l.CompanyId, Left(l.ProviderRef))) is { Count: > 1 } both
                && both.Contains("aadhaar") && both.Contains("pan") ? "Aadhaar + PAN via DigiLocker (2 documents)" : what;
            entries.Add(new UsageEntry(l.Id, l.CreatedAt, l.CompanyId, l.ApplicationId,
                req?.PartyId is { } pid ? parties.GetValueOrDefault(pid) : null, what, group,
                req?.RequestedByUserId is { } uid ? users.GetValueOrDefault(uid) : null, l.IsSuccess ? l.UnitRate : 0m, l.IsSuccess, l.ApiCode));
        }

        return entries;
    }

    /// <summary>The readable name of a call from its code, its request's operation and what its provider reference points at.</summary>
    public static string Name(string code, string? operation, string? refRight, IReadOnlyDictionary<string, string> signers, (int Attempt, int Emi)? debit, IReadOnlyDictionary<string, string>? rates = null) => code switch
    {
        "KYC_ID_VERIFY" => operation == "PAN_VERIFY" ? "PAN check (standalone)" : "ID check",
        "KYC_DIGILOCKER" => refRight?.ToLowerInvariant() switch { "aadhaar" => "Aadhaar via DigiLocker", "pan" => "PAN via DigiLocker", _ => "DigiLocker document" },
        "KYC_AADHAAR_MASK" => "Aadhaar masking",
        "AML_SCREEN" => "AML screening",
        "KYC_VEHICLE_RC" => "RC check",
        "KYC_BUSINESS" => operation switch { "GST_VERIFY" => "GSTIN check", "CIN_VERIFY" => "CIN check", _ => "Business check" },
        "BANK_PENNY_DROP" => "Penny drop",
        "ESIGN_AADHAAR" => refRight is { Length: > 0 } && signers.TryGetValue(refRight, out var n) ? $"e-Sign: signer ({n})" : "e-Sign: signer",
        "ESIGN_SEND" => "e-Sign: send",
        "ESIGN_DOWNLOAD" => "e-Sign: signed copy",
        "MANDATE_API" => "e-Mandate (netbanking/debit/Aadhaar)",
        "MANDATE_ESIGN" => "e-Mandate (eSign)",
        "MANDATE_PHYSICAL" => "Physical NACH mandate",
        "MANDATE_DEBIT" => debit is { } d ? $"EMI debit attempt {d.Attempt} (EMI {d.Emi})" : "EMI debit attempt",
        "MANDATE_CANCEL" => "Mandate cancel",
        _ => rates is not null && rates.TryGetValue(code, out var rate) ? rate : code,
    };

    /// <summary>The same calls grouped by application. The successful amounts add up to the monthly summary's subtotal.</summary>
    public static IReadOnlyList<AppUsage> ByApplication(IEnumerable<UsageEntry> entries) =>
        entries.GroupBy(e => e.ApplicationId).OrderBy(g => g.Key is null).ThenBy(g => g.Key)
            .Select(g => new AppUsage(g.Key, g.Sum(e => e.Amount), g.OrderBy(e => e.At).ToList())).ToList();

    /// <summary>The stamp-card log as CSV (UTF-8, invariant numbers, IST times). Cells that could be read as a formula are defused.</summary>
    public static string Csv(IEnumerable<UsageEntry> entries, Func<int, string> companyName)
    {
        var sb = new StringBuilder("Date (IST),Company,Application,Party,What,User,Amount (Rs),Result,Code\n");
        foreach (var e in entries)
        {
            sb.AppendJoin(',', Cell((e.At + Ist).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)), Cell(companyName(e.CompanyId)), Cell(e.ApplicationId),
                Cell(e.Party), Cell(e.What), Cell(e.User), Cell(e.Amount.ToString("0.00", CultureInfo.InvariantCulture)), Cell(e.Success ? "Success" : "Failed"), Cell(e.ApiCode)).Append('\n');
        }

        return sb.ToString();
    }

    private static string Cell(string? v)
    {
        var s = v ?? "";
        if (s.Length > 0 && s[0] is '=' or '+' or '-' or '@' or '\t' or '\r' && !decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
        {
            s = "'" + s;
        }

        return s.Contains(',') || s.Contains('"') || s.Contains('\n') ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
    }

    private static string? Left(string? providerRef) => providerRef is { Length: > 0 } r ? (r.Contains(':') ? r[..r.IndexOf(':')] : r) : null;

    private static string? Right(string? providerRef) => providerRef is { Length: > 0 } r && r.Contains(':') ? r[(r.IndexOf(':') + 1)..] : null;
}
