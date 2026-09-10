using LosLms.Shell;

namespace LosLms.Server;

/// <summary>
/// The startup sequence for the Server machine, in order, each step awaited to genuine success before
/// the next: MySQL → backend → local shell → (additive) tunnel → publish. Owns the child services and
/// tears them all down cleanly on shutdown. Progress is surfaced on the shell's splash overlay; the
/// full record goes to the launcher log.
/// </summary>
internal sealed class Orchestrator
{
    private readonly ShellWindow _shell;
    private readonly TrayController _tray;
    private readonly ServerConfig _config;

    private readonly MySqlManager _mysql = new();
    private readonly BackendSupervisor _backend = new();
    private readonly TunnelManager _tunnel = new();
    private BackupManager? _backup;

    private readonly CancellationTokenSource _cts = new();
    private Thread? _superviseThread;
    private bool _stopped;

    public Orchestrator(ShellWindow shell, TrayController tray, ServerConfig config)
    {
        _shell = shell;
        _tray = tray;
        _config = config;
    }

    public async Task RunAsync()
    {
        var ct = _cts.Token;
        void Progress(string message) => _shell.ShowConnecting("Starting LOS/LMS…", message);

        // ---- 1. MySQL: fail-stop. Never start the backend against an absent database. ----
        try
        {
            await _mysql.StartAsync(Progress, ct);
        }
        catch (Exception ex)
        {
            Log.Error("MySQL failed to start", ex);
            _shell.ShowError(
                "The database couldn't start",
                "LOS/LMS could not start its database, so it can't run on this machine.\n\n"
                    + ex.Message + "\n\nSee server-launcher.log next to the app for details.",
                "Quit",
                QuitFromError);
            return;
        }

        // ---- 2. Backend, pointed at local MySQL. Migrations apply automatically on this boot. ----
        try
        {
            await _backend.StartAsync(_mysql.AppConnectionString, Progress, ct);
        }
        catch (Exception ex)
        {
            Log.Error("Backend failed to start", ex);
            _shell.ShowError(
                "The application server couldn't start",
                "The database is running, but the application server did not come up.\n\n"
                    + ex.Message + "\n\nSee server-launcher.log next to the app for details.",
                "Quit",
                QuitFromError);
            return;
        }

        // ---- 3. Local shell works now — do this BEFORE the tunnel so local use never waits on it. ----
        _backend.BackendRestarted += () => _shell.NavigateAsync(_backend.LocalUrl);
        _backend.BackendFailedPermanently += () => _shell.ShowError(
            "The application server stopped",
            "The server kept crashing on startup and has been stopped — this usually means a "
                + "configuration problem. See server-launcher.log next to the app.",
            "Quit",
            QuitFromError);
        await _shell.NavigateAsync(_backend.LocalUrl);

        // ---- 4. Supervise (restart-on-crash + apply updates) on a background thread. ----
        _superviseThread = new Thread(() => _backend.RunSuperviseLoop(ct))
        {
            IsBackground = true,
            Name = "backend-supervisor",
        };
        _superviseThread.Start();

        // ---- 5. Automatic database backups (local + optional offsite). Additive. ----
        _backup = new BackupManager(_mysql.Port, _mysql.Credentials, _config);
        _backup.Start();

        // ---- 6. Named tunnel: additive. Failure here never blocks the working local machine. The public
        //          URL is fixed (config), so there is nothing to publish or discover. ----
        if (_config.HasTunnel)
        {
            var started = await _tunnel.StartAsync(_config.TunnelToken!, Progress, ct);
            _tray.SetUrl(started ? _config.HostedUrl : null);
        }
        else
        {
            _tray.SetUrl(null);
            Log.Info(
                "No tunnel token configured — running LAN-only. Add TunnelToken to server-config.json to "
                + $"publish this machine at {_config.HostedUrl}.");
        }
    }

    /// <summary>Stops everything in the safe order: tunnel, backend, then MySQL. Idempotent.</summary>
    public void Shutdown()
    {
        if (_stopped)
        {
            return;
        }

        _stopped = true;
        Log.Info("Shutting down — stopping tunnel, backups, backend, and database.");
        try { _cts.Cancel(); } catch { /* ignore */ }

        _tunnel.Stop();
        _backup?.Stop();
        _backend.Stop();
        _mysql.Stop();

        try { _superviseThread?.Join(5000); } catch { /* ignore */ }
        Log.Info("Shutdown complete.");
    }

    private void QuitFromError()
    {
        Shutdown();
        _shell.BeginInvoke(() => _shell.Close());
    }
}
