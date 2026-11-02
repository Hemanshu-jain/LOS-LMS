namespace LosLms.Services;

/// <summary>
/// Creates the day's EMI debits at 18:00 IST (Digio needs the request by late night on T-1 for a debit on day T) and once shortly after
/// start-up as a catch-up. Everything it does is idempotent (<see cref="DigioCollect.PresentDueAsync"/>), so a restart or a second
/// instance cannot double-present. Never throws out of the loop.
/// </summary>
public sealed class CollectionScheduler(IServiceScopeFactory scopes, ILogger<CollectionScheduler> log) : BackgroundService
{
    private static readonly TimeSpan RunAt = new(18, 0, 0);
    private static readonly TimeSpan Ist = TimeSpan.FromHours(5.5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(90), stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunOnceAsync();
                await Task.Delay(UntilNext(DateTime.UtcNow), stoppingToken);
            }
        }
        catch (OperationCanceledException) { /* shutdown */ }
    }

    /// <summary>The wait until the next 18:00 IST.</summary>
    public static TimeSpan UntilNext(DateTime nowUtc)
    {
        var ist = nowUtc + Ist;
        var next = ist.Date + RunAt;
        if (next <= ist)
        {
            next = next.AddDays(1);
        }

        return next - ist;
    }

    public async Task<int> RunOnceAsync()
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<DigioCollect>().PresentDueAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Collection scheduler run failed; will retry at the next run");
            return 0;
        }
    }
}
