namespace LosLms.Services;

/// <summary>
/// Tells every Admin currently looking at the Inbox that their company's pending list has changed.
/// </summary>
/// <remarks>
/// Blazor Server already holds a live SignalR connection per signed-in user, so a new request can
/// reach an open Inbox without polling: the page subscribes on load, this fires when a request is
/// created or reviewed, and the page re-queries. Nothing is pushed through the event itself except
/// the company id — the page reloads from the database, so it can never render a stale or
/// half-constructed row.
///
/// SINGLE-SERVER ONLY. This is an in-process event, so it reaches subscribers inside this process and
/// nowhere else. Deployed across two or more instances, an Admin connected to instance A would not
/// see a request raised on instance B until they refreshed. Making that work needs a real backplane —
/// Redis pub/sub, or Azure SignalR — publishing the same notification across instances. Flagged here
/// rather than discovered later: the app is correct on one box and quietly wrong on several.
///
/// Registered as a singleton, so it outlives any one circuit. Subscribers MUST unsubscribe on
/// dispose, or a closed page keeps a dead circuit alive through the delegate.
/// </remarks>
public sealed class AdminRequestNotifier
{
    /// <summary>Raised with the company whose request list changed.</summary>
    public event Func<int, Task>? Changed;

    public async Task NotifyAsync(int companyId)
    {
        if (Changed is null)
        {
            return;
        }

        // Each subscriber is a separate circuit; one throwing must not stop the others being told.
        foreach (var handler in Changed.GetInvocationList().Cast<Func<int, Task>>())
        {
            try
            {
                await handler(companyId);
            }
            catch (Exception)
            {
                // A circuit that has gone away mid-notification is expected, not exceptional.
            }
        }
    }
}
