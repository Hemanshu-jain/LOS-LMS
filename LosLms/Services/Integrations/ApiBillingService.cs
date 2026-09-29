using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LosLms.Services;

/// <summary>Postpaid billing knobs. Bound from the <c>ApiBilling</c> config section.</summary>
public sealed class ApiBillingOptions
{
    public const string Section = "ApiBilling";

    /// <summary>Days after a month closes that the client has to pay before API use is paused.</summary>
    public int GraceDays { get; set; } = 7;

    /// <summary>GST added on top of the provider rates (Digio quotes are ex-GST).</summary>
    public decimal GstPct { get; set; } = 18m;
}

/// <summary>One row of the monthly usage report: a single API at a single rate for a single company.</summary>
public sealed record UsageLine(
    int CompanyId, string ApiCode, string Name, string Product, decimal UnitRate, int Calls, int Failed, decimal Amount);

/// <summary>Whether a company may call provider APIs right now, and if a bill is pending, which.</summary>
public sealed record ApiAccess(bool Allowed, string? Reason, DateOnly? DueDate, decimal AmountDue)
{
    public static readonly ApiAccess Open = new(true, null, null, 0m);
}

/// <summary>
/// Metering + postpaid billing for provider (Digio) API use. Every call is logged; each month closes
/// into an invoice that the client must clear within the grace window or API use is paused until the
/// vendor marks it paid. Month boundaries are India time — that is what the client's bill is read in.
/// </summary>
/// <remarks>
/// Prepaid wallet is the planned successor: <see cref="CheckAccessAsync"/> is the single gate every
/// provider call goes through, so a balance check slots in there without touching call sites.
/// </remarks>
public sealed class ApiBillingService(IDbContextFactory<LosDbContext> dbFactory, IOptions<ApiBillingOptions> options)
{
    private static readonly TimeSpan IstOffset = TimeSpan.FromMinutes(330);

    private readonly ApiBillingOptions _opt = options.Value;

    /// <summary>Today's date in India.</summary>
    public static DateOnly TodayIst(DateTime? nowUtc = null) =>
        DateOnly.FromDateTime((nowUtc ?? DateTime.UtcNow) + IstOffset);

    private static DateOnly MonthOf(DateOnly d) => new(d.Year, d.Month, 1);

    /// <summary>UTC instant at which the given India-time month begins.</summary>
    private static DateTime MonthStartUtc(DateOnly month) =>
        DateTime.SpecifyKind(month.ToDateTime(TimeOnly.MinValue) - IstOffset, DateTimeKind.Utc);

    /// <summary>Records a provider call. Unknown codes throw — a typo must never bill nothing.</summary>
    public async Task RecordAsync(
        int companyId, string apiCode, bool success,
        string? applicationId = null, string? providerRef = null, DateTime? nowUtc = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var rate = await db.ApiRates.AsNoTracking().FirstOrDefaultAsync(r => r.Code == apiCode)
            ?? throw new InvalidOperationException($"Unknown API code '{apiCode}'.");

        db.ApiUsageLogs.Add(new ApiUsageLog
        {
            CompanyId = companyId,
            ApiCode = apiCode,
            ApplicationId = applicationId,
            ProviderRef = providerRef,
            IsSuccess = success,
            UnitRate = rate.UnitRate,
            CreatedAt = nowUtc ?? DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    /// <summary>Usage for one India-time month, optionally narrowed to a company.</summary>
    public async Task<IReadOnlyList<UsageLine>> UsageForMonthAsync(int? companyId, DateOnly month)
    {
        var from = MonthStartUtc(MonthOf(month));
        var to = MonthStartUtc(MonthOf(month).AddMonths(1));

        await using var db = await dbFactory.CreateDbContextAsync();
        var logs = db.ApiUsageLogs.AsNoTracking().Where(l => l.CreatedAt >= from && l.CreatedAt < to);
        if (companyId is { } id)
        {
            logs = logs.Where(l => l.CompanyId == id);
        }

        var grouped = await logs
            .GroupBy(l => new { l.CompanyId, l.ApiCode, l.UnitRate })
            .Select(g => new
            {
                g.Key.CompanyId,
                g.Key.ApiCode,
                g.Key.UnitRate,
                Calls = g.Count(l => l.IsSuccess),
                Failed = g.Count(l => !l.IsSuccess),
            })
            .ToListAsync();

        var rates = await db.ApiRates.AsNoTracking().ToDictionaryAsync(r => r.Code);

        return grouped
            .Select(g =>
            {
                rates.TryGetValue(g.ApiCode, out var r);
                return new UsageLine(g.CompanyId, g.ApiCode, r?.Name ?? g.ApiCode, r?.Product ?? "Other",
                    g.UnitRate, g.Calls, g.Failed, g.Calls * g.UnitRate);
            })
            .OrderBy(u => u.CompanyId).ThenBy(u => u.Product).ThenBy(u => u.Name)
            .ToList();
    }

    public decimal GstOn(decimal subtotal) => Math.Round(subtotal * _opt.GstPct / 100m, 2);

    /// <summary>
    /// Creates the invoice for every closed month that has billable usage and none yet. Idempotent and
    /// deterministic (the due date derives from the month, not from when this ran), so it is safe to
    /// call from anywhere, any number of times — no scheduler needed.
    /// </summary>
    public async Task EnsureInvoicesAsync(int? companyId, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var currentMonth = MonthOf(TodayIst(now));
        var closedBefore = MonthStartUtc(currentMonth);

        await using var db = await dbFactory.CreateDbContextAsync();

        var logs = db.ApiUsageLogs.AsNoTracking().Where(l => l.IsSuccess && l.CreatedAt < closedBefore);
        var existing = db.ApiInvoices.AsNoTracking().AsQueryable();
        if (companyId is { } id)
        {
            logs = logs.Where(l => l.CompanyId == id);
            existing = existing.Where(i => i.CompanyId == id);
        }

        var have = (await existing.Select(i => new { i.CompanyId, i.PeriodStart }).ToListAsync())
            .Select(i => (i.CompanyId, i.PeriodStart)).ToHashSet();

        // For one company (the hot path: every provider call) only months after its newest invoice can
        // be missing, so don't rescan all history. Across companies "newest" isn't comparable — scan all.
        var latest = companyId is null || have.Count == 0 ? (DateOnly?)null : have.Max(h => h.PeriodStart);
        if (latest is { } l0)
        {
            var since = MonthStartUtc(l0.AddMonths(1));
            logs = logs.Where(l => l.CreatedAt >= since);
        }

        var billable = (await logs.Select(l => new { l.CompanyId, l.CreatedAt, l.UnitRate }).ToListAsync())
            .GroupBy(l => (l.CompanyId, Month: MonthOf(DateOnly.FromDateTime(l.CreatedAt + IstOffset))))
            .Where(g => !have.Contains((g.Key.CompanyId, g.Key.Month)) && g.Key.Month < currentMonth);

        foreach (var g in billable)
        {
            var subtotal = g.Sum(l => l.UnitRate);
            db.ApiInvoices.Add(new ApiInvoice
            {
                CompanyId = g.Key.CompanyId,
                PeriodStart = g.Key.Month,
                Subtotal = subtotal,
                Gst = GstOn(subtotal),
                DueDate = g.Key.Month.AddMonths(1).AddDays(_opt.GraceDays),
                GeneratedAt = now,
            });
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Another request created the same invoice first (unique company+month) — that's fine.
        }
    }

    public async Task<IReadOnlyList<ApiInvoice>> InvoicesAsync(int? companyId, int take = 12)
    {
        await EnsureInvoicesAsync(companyId);
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.ApiInvoices.AsNoTracking().AsQueryable();
        if (companyId is { } id)
        {
            q = q.Where(i => i.CompanyId == id);
        }

        return await q.OrderByDescending(i => i.PeriodStart).ThenBy(i => i.CompanyId).Take(take).ToListAsync();
    }

    /// <summary>
    /// The gate. Blocked once any invoice is unpaid past its due date; while one is merely unpaid the
    /// company is still allowed (that is the grace window) and the pending bill is returned for a banner.
    /// </summary>
    public async Task<ApiAccess> CheckAccessAsync(int companyId, DateTime? nowUtc = null)
    {
        await EnsureInvoicesAsync(companyId, nowUtc);

        await using var db = await dbFactory.CreateDbContextAsync();
        var unpaid = (await db.ApiInvoices.AsNoTracking()
                .Where(i => i.CompanyId == companyId && i.PaidAt == null)
                .ToListAsync())
            .OrderBy(i => i.DueDate)
            .ToList();

        if (unpaid.Count == 0)
        {
            return ApiAccess.Open;
        }

        var today = TodayIst(nowUtc);
        var first = unpaid[0];
        var due = unpaid.Sum(i => i.Total);

        return first.DueDate < today
            ? new ApiAccess(false,
                $"API usage is paused: the {first.PeriodStart:MMMM yyyy} bill was due {first.DueDate:d MMM yyyy}. " +
                "It resumes once the payment is recorded.", first.DueDate, due)
            : new ApiAccess(true, null, first.DueDate, due);
    }

    /// <summary>Vendor records a payment — this is what lifts the pause.</summary>
    public async Task<bool> MarkPaidAsync(int invoiceId, string? note, DateTime? nowUtc = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var invoice = await db.ApiInvoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice is null || invoice.PaidAt is not null)
        {
            return false;
        }

        invoice.PaidAt = nowUtc ?? DateTime.UtcNow;
        invoice.PaymentNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        await db.SaveChangesAsync();
        return true;
    }
}
