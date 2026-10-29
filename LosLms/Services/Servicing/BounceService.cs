using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>Called by the debit confirm path (slice 2) when a presentation has just been marked Failed. Safe to call again for the same presentation.</summary>
public interface IBounceHandler
{
    Task OnFailedAsync(string presentationId);
}

/// <summary>Starts the next debit attempt for an instalment (slice 2's presentation code implements it). Until it does, nothing is re-presented automatically.</summary>
public interface IPresentationStarter
{
    /// <returns>True when a new presentation was created.</returns>
    Task<bool> StartAsync(int instalmentId, int attempt);
}

public sealed class NoopPresentationStarter : IPresentationStarter
{
    public Task<bool> StartAsync(int instalmentId, int attempt) => Task.FromResult(false);
}

/// <summary>A bounced instalment waiting to be presented again.</summary>
public sealed record RepresentItem(Instalment Instalment, Presentation Last, BounceDecision Decision, int Attempts);

/// <summary>
/// What happens after a failed debit: the instalment is marked Bounced, a bounce charge is raised (once per failed presentation), and
/// <see cref="BounceBook"/> decides between presenting again later and handing it to a person (a <see cref="CollectionTask"/> for the
/// application's assigned officer). Runs without a user (webhook / scheduler), so it bypasses the tenant filter and scopes by CompanyId.
/// </summary>
public sealed class BounceService(IDbContextFactory<LosDbContext> dbFactory) : IBounceHandler
{
    public async Task OnFailedAsync(string presentationId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var p = await db.Presentations.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == presentationId);
        if (p is not { Status: "Failed" })
        {
            return;
        }

        var inst = await db.Instalments.IgnoreQueryFilters().Include(i => i.LoanAccount).FirstOrDefaultAsync(i => i.Id == p.InstalmentId && i.CompanyId == p.CompanyId);
        if (inst?.LoanAccount is not { } account)
        {
            return;
        }

        if (inst.Status is not ("Paid" or "Waived"))
        {
            inst.Status = "Bounced";
        }

        var settings = await db.DigioSettings.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(s => s.CompanyId == p.CompanyId) ?? new DigioSettings();
        var marker = $"[{p.Id}]";
        // ponytail: two simultaneous calls for the same presentation could both pass this check; the confirm path calls once, so no unique index.
        if (!await db.LoanCharges.IgnoreQueryFilters().AnyAsync(c => c.InstalmentId == inst.Id && c.Kind == "Bounce" && c.Note!.Contains(marker)))
        {
            db.LoanCharges.Add(new LoanCharge
            {
                CompanyId = p.CompanyId, LoanAccountId = account.Id, InstalmentId = inst.Id, Kind = "Bounce",
                Amount = settings.BounceChargeAmount, Gst = Math.Round(settings.BounceChargeAmount * settings.BounceChargeGstPct / 100m, 2),
                Note = $"Bounce of instalment {inst.No}, attempt {p.Attempt} {marker}", CreatedAt = DateTime.UtcNow,
            });
        }

        var attempts = await db.Presentations.IgnoreQueryFilters().CountAsync(x => x.InstalmentId == inst.Id && x.Status != "Cancelled");
        var decision = BounceBook.Decide(p.FailureCode, attempts, settings.MaxAttempts, DateOnly.FromDateTime(p.UpdatedAt), DateOnly.FromDateTime(DateTime.UtcNow), settings.RepresentAfterDays);
        if (decision.Action == BounceBook.Escalate && decision.Kind is { } kind
            && !await db.CollectionTasks.IgnoreQueryFilters().AnyAsync(t => t.InstalmentId == inst.Id && t.Kind == kind && t.Status == "Open"))
        {
            var officer = await db.Applications.IgnoreQueryFilters().Where(a => a.Id == account.ApplicationId).Select(a => a.AssignedOfficerId).FirstOrDefaultAsync();
            db.CollectionTasks.Add(new CollectionTask
            {
                CompanyId = p.CompanyId, ApplicationId = account.ApplicationId, LoanAccountId = account.Id, InstalmentId = inst.Id, Kind = kind,
                AssignedOfficerId = officer, Reason = Cut($"Instalment {inst.No}: {decision.Reason}", 300), CreatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Bounced instalments on active loans whose LAST attempt failed, with what to do next. Items with a Represent decision are the
    /// "re-present queue"; <paramref name="dueOnly"/> keeps only those whose wait is over.
    /// </summary>
    public static async Task<List<RepresentItem>> RepresentQueueAsync(LosDbContext db, DateOnly today, bool dueOnly, bool allCompanies = false)
    {
        var rows = await Open(db.Instalments, allCompanies).AsNoTracking().Where(i => i.Status == "Bounced" && i.LoanAccount!.Status == "Active").ToListAsync();
        var ids = rows.Select(i => i.Id).ToList();
        var presentations = await Open(db.Presentations, allCompanies).AsNoTracking().Where(p => ids.Contains(p.InstalmentId) && p.Status != "Cancelled").ToListAsync();
        var settings = await Open(db.DigioSettings, allCompanies).AsNoTracking().ToListAsync();
        var list = new List<RepresentItem>();
        foreach (var i in rows)
        {
            var mine = presentations.Where(p => p.InstalmentId == i.Id).OrderBy(p => p.Attempt).ToList();
            if (mine.Count == 0 || mine[^1] is not { Status: "Failed" } last)
            {
                continue; // a newer attempt is already running
            }

            var s = settings.FirstOrDefault(x => x.CompanyId == i.CompanyId) ?? new DigioSettings();
            var d = BounceBook.Decide(last.FailureCode, mine.Count, s.MaxAttempts, DateOnly.FromDateTime(last.UpdatedAt), today, s.RepresentAfterDays);
            if (d.Action == BounceBook.Represent && (!dueOnly || d.EarliestOn <= today))
            {
                list.Add(new RepresentItem(i, last, d, mine.Count));
            }
        }

        return list;
    }

    // The scheduler has no signed-in user, so it reads every company (each row still carries its own CompanyId).
    private static IQueryable<T> Open<T>(DbSet<T> set, bool all) where T : class => all ? set.IgnoreQueryFilters() : set;

    /// <summary>An Admin waives a bounce charge (Open → Waived), with the reason kept on the charge.</summary>
    public async Task<bool> WaiveChargeAsync(int chargeId, string note, string adminUserId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var charge = await db.LoanCharges.FirstOrDefaultAsync(c => c.Id == chargeId);
        if (!db.IsElevatedUser || charge is not { Status: "Open" } || string.IsNullOrWhiteSpace(note))
        {
            return false;
        }

        charge.Status = "Waived";
        charge.Note = Cut($"{charge.Note} | Waived by {adminUserId}: {note.Trim()}", 200);
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>A collections officer marks a task done, with a note.</summary>
    public async Task<bool> ResolveTaskAsync(int taskId, string note, string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var task = await db.CollectionTasks.FirstOrDefaultAsync(t => t.Id == taskId && t.Status == "Open");
        if (task is null || string.IsNullOrWhiteSpace(note))
        {
            return false;
        }

        task.Status = "Done";
        task.ResolvedAt = DateTime.UtcNow;
        task.ResolvedByUserId = userId;
        task.ResolutionNote = Cut(note.Trim(), 300);
        await db.SaveChangesAsync();
        return true;
    }

    private static string Cut(string s, int max) => s.Length > max ? s[..max] : s;
}

/// <summary>Daily: presents every bounced instalment whose wait is over, through <see cref="IPresentationStarter"/> (slice 2 supplies the real one).</summary>
public sealed class RepresentationJob(IServiceScopeFactory scopes, ILogger<RepresentationJob> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
            do
            {
                try { await RunOnceAsync(DateOnly.FromDateTime(DateTime.UtcNow)); }
                catch (Exception ex) when (ex is not OperationCanceledException) { log.LogError(ex, "Re-presentation run failed; will retry"); }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) { /* shutdown */ }
    }

    public async Task<int> RunOnceAsync(DateOnly today)
    {
        await using var scope = scopes.CreateAsyncScope();
        await using var db = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<LosDbContext>>().CreateDbContextAsync();
        var starter = scope.ServiceProvider.GetRequiredService<IPresentationStarter>();
        var started = 0;
        foreach (var item in await BounceService.RepresentQueueAsync(db, today, dueOnly: true, allCompanies: true))
        {
            if (await starter.StartAsync(item.Instalment.Id, item.Attempts + 1))
            {
                started++;
            }
        }

        return started;
    }
}
