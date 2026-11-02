namespace LosLms.Services;

/// <summary>
/// Whether the app is in maintenance mode. A single process-wide flag a SuperAdmin flips from the
/// System Updates page (or that starts on via <c>Maintenance:Enabled</c>), read by the maintenance
/// middleware in <c>Program.cs</c> on every request.
/// </summary>
/// <remarks>
/// Singleton and deliberately trivial: one <c>volatile bool</c>, no persistence. It is a live operator
/// switch, not durable state — a restart clears it back to the configured default, which is the safe
/// direction (a server that just came back up should be serving, not stuck behind a maintenance page
/// nobody remembers turning on). Single-server by design, like the rest of this app.
/// </remarks>
public sealed class MaintenanceState
{
    private volatile bool _isOn;

    public MaintenanceState(bool initiallyOn) => _isOn = initiallyOn;

    /// <summary>True while the app is showing the maintenance page to everyone but SuperAdmins.</summary>
    public bool IsOn => _isOn;

    public void Set(bool on) => _isOn = on;
}
