using System;
using System.Threading;
using System.Threading.Tasks;

namespace LosLms.Services;

/// <summary>
/// Fires a save callback on a fixed interval until disposed, so the stage screens persist
/// data entry on their own — no manual "Save draft" click required. One instance per page;
/// <see cref="Start"/> is idempotent (safe to call from a re-runnable lifecycle method), and
/// overlapping ticks are skipped so a slow save never stacks up.
/// </summary>
public sealed class AutoSaveLoop : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private int _started;
    private int _running;

    /// <summary>Begin the loop. Only the first call per instance takes effect.</summary>
    public void Start(Func<Task> saveTick, TimeSpan interval)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        _ = RunAsync(saveTick, interval, _cts.Token);
    }

    private async Task RunAsync(Func<Task> saveTick, TimeSpan interval, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                // Skip if the previous tick's save is still in flight.
                if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
                {
                    continue;
                }

                try { await saveTick(); }
                catch { /* ponytail: a transient save failure just waits for the next tick */ }
                finally { Interlocked.Exchange(ref _running, 0); }
            }
        }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
