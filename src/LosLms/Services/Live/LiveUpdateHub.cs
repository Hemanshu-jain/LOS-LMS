using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LosLms.Services;

/// <summary>What was just saved: the application it belongs to (when it belongs to one) and, when the row says so, the company.</summary>
public readonly record struct LiveChange(string? ApplicationId, int? CompanyId);

/// <summary>
/// Tells every open screen that something was saved, so each can refresh itself within a moment without anyone pressing reload.
/// Singleton, in-process: it reaches every circuit on THIS server, which is the deployment (one instance). If the site is ever run on
/// several instances at once, this needs a shared channel (Redis or the database) in place of the in-memory event.
/// </summary>
public sealed class LiveUpdateHub
{
    public event Func<LiveChange, Task>? Changed;

    public void Publish(LiveChange change)
    {
        var handlers = Changed;
        if (handlers is null) { return; }

        // Never on the saving thread: a slow or failing screen must not slow or fail a save.
        _ = Task.Run(async () =>
        {
            foreach (var handler in handlers.GetInvocationList().Cast<Func<LiveChange, Task>>())
            {
                try { await handler(change); }
                catch (Exception) { /* one screen's failure never stops the others being told */ }
            }
        });
    }
}

/// <summary>Publishes a <see cref="LiveChange"/> for every application a successful save touched. No screen has to remember to announce its own changes.</summary>
public sealed class LiveChangeInterceptor(LiveUpdateHub hub) : SaveChangesInterceptor
{
    // Rows that change constantly and no screen shows live.
    private static readonly HashSet<string> Quiet = new(StringComparer.Ordinal) { "ApiUsageLog", "ApiRate", "DigioRequest" };

    private readonly ConcurrentDictionary<Guid, HashSet<LiveChange>> _pending = new();

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Collect(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Collect(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Flush(eventData.Context);
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Flush(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => Drop(eventData.Context);

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Drop(eventData.Context);
        return Task.CompletedTask;
    }

    private void Collect(DbContext? context)
    {
        if (context is null) { return; }

        var set = new HashSet<LiveChange>();
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) { continue; }
            if (Quiet.Contains(entry.Metadata.ClrType.Name)) { continue; }

            var appId = entry.Metadata.ClrType.Name == "Application" ? entry.Property("Id").CurrentValue as string : Read<string>(entry, "ApplicationId");
            var companyId = Read<int?>(entry, "CompanyId");
            set.Add(new LiveChange(appId, companyId));
        }

        if (set.Count > 0) { _pending[context.ContextId.InstanceId] = set; }
    }

    private static T? Read<T>(EntityEntry entry, string name) =>
        entry.Metadata.FindProperty(name) is not null && entry.Property(name).CurrentValue is { } v
            ? (T?)(typeof(T) == typeof(int?) ? (object)Convert.ToInt32(v) : v)
            : default;

    private void Flush(DbContext? context)
    {
        if (context is null || !_pending.TryRemove(context.ContextId.InstanceId, out var set)) { return; }
        foreach (var change in set) { hub.Publish(change); }
    }

    private void Drop(DbContext? context)
    {
        if (context is not null) { _pending.TryRemove(context.ContextId.InstanceId, out _); }
    }
}
