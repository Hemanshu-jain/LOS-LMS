using System.Diagnostics;
using System.IO.Compression;

namespace LosLms.Server;

/// <summary>
/// Starts and supervises the backend (app\LosLms.exe), windowless, pointed at the local MySQL via an
/// environment variable so the connection string is never written to a committed file. Restarts it if
/// it exits unexpectedly, and applies self-updates by swapping the app\ folder with backup + rollback
/// — the responsibility formerly held by LosLms.Watchdog, which now lives here because this launcher is
/// the never-swapped exe sitting next to app\.
/// </summary>
internal sealed class BackendSupervisor
{
    // Generous on purpose: the FIRST run initialises a fresh MySQL data directory (~50s on its own),
    // then applies every migration and seeds on a cold database — which on a loaded machine runs well
    // past two minutes before Kestrel binds. Later runs reuse the warmed database and come up in
    // seconds, so this ceiling only ever matters once.
    private static readonly TimeSpan HttpReadyTimeout = TimeSpan.FromSeconds(300);

    // Preserved across an update swap: the operator's config and their data/uploaded PII.
    private static readonly string[] Preserve = { "appsettings.json", "App_Data" };

    private readonly string _applyTempDir = Path.Combine(Paths.InstallRoot, "_apply");
    private readonly string _backupDir = Path.Combine(Paths.InstallRoot, "_backup");

    // Crash-loop guard: if the backend keeps dying within this window of being started, it is not a
    // transient crash to restart through — it is broken (a bad config or, once they are added, a bad
    // SDK/API key). Stop after this many rapid failures instead of respawning forever.
    private static readonly TimeSpan RapidFailureWindow = TimeSpan.FromSeconds(15);
    private const int MaxRapidFailures = 5;

    /// <summary>The fixed local port the backend binds. Must match the Cloudflare tunnel's public-hostname
    /// service mapping (http://localhost:5037).</summary>
    private const int LocalPort = 5037;

    private string _connectionString = "";
    private string _publicUrl = "";
    private bool _webMode;
    private Process? _backend;
    private DateTime _backendStartedAt;
    private int _rapidFailures;
    private volatile bool _stopping;

    public int Port { get; private set; }

    public string LocalUrl => $"http://127.0.0.1:{Port}";

    /// <summary>Raised (on a background thread) after the backend is restarted, so the shell can reload.</summary>
    public event Action? BackendRestarted;

    /// <summary>Raised when the backend crash-loops and the supervisor gives up, so the shell can say so.</summary>
    public event Action? BackendFailedPermanently;

    public async Task StartAsync(
        string connectionString, string publicUrl, bool webMode, Action<string> progress, CancellationToken ct)
    {
        if (!File.Exists(Paths.BackendExe))
        {
            throw new FileNotFoundException(
                $"Backend not found at {Paths.BackendExe}. The Server package is incomplete.", Paths.BackendExe);
        }

        _connectionString = connectionString;
        _publicUrl = publicUrl;
        _webMode = webMode;
        // Fixed, not free-scanned: the named tunnel's hostname → http://localhost:5037 mapping (set once
        // in the Cloudflare dashboard) has to keep matching, so the backend must always bind the same
        // local port across restarts.
        Port = LocalPort;
        Log.Info($"Backend will use port {Port}.");

        progress("Starting the application server…");
        _backend = StartBackend();

        progress("Waiting for the application server…");
        await WaitUntilServingAsync(ct);
    }

    /// <summary>
    /// The supervise loop: watch for an apply signal and swap in updates, and restart the backend if
    /// it dies. Runs until <see cref="Stop"/> is called. Mirrors the old watchdog's behaviour.
    /// </summary>
    public void RunSuperviseLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && !_stopping)
        {
            try
            {
                Thread.Sleep(2000);

                if (File.Exists(Paths.ApplySignal))
                {
                    ApplyUpdate();
                    continue;
                }

                if (_backend is { HasExited: true } && !_stopping)
                {
                    _rapidFailures = DateTime.UtcNow - _backendStartedAt < RapidFailureWindow
                        ? _rapidFailures + 1
                        : 0;

                    if (_rapidFailures >= MaxRapidFailures)
                    {
                        Log.Error(
                            $"Backend crash-looped ({MaxRapidFailures} rapid exits). Giving up — it is "
                            + "misconfigured, not crashing transiently. Check server-launcher.log.");
                        _stopping = true;
                        BackendFailedPermanently?.Invoke();
                        break;
                    }

                    Log.Warn($"Backend exited unexpectedly (code {_backend.ExitCode}). Restarting.");
                    _backend = StartBackend();
                    BackendRestarted?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Supervise loop error", ex);
            }
        }
    }

    public void Stop()
    {
        _stopping = true;
        StopBackend(_backend);
    }

    // ---- process lifecycle -------------------------------------------------------------------------

    private Process StartBackend()
    {
        var info = new ProcessStartInfo
        {
            FileName = Paths.BackendExe,
            WorkingDirectory = Paths.AppDir,
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        info.ArgumentList.Add("--urls");
        info.ArgumentList.Add(LocalUrl); // bind 127.0.0.1 only — the tunnel, not the LAN, carries remote users
        info.Environment["ConnectionStrings__LosDb"] = _connectionString;

        // Public multi-tenant web instance: load appsettings.Web.json, which turns on self-service
        // company registration and seeds the demo tenant. Absent for a normal client install.
        if (_webMode)
        {
            info.Environment["ASPNETCORE_ENVIRONMENT"] = "Web";
        }

        // The client's fixed public address, so the app can show "share this URL with your staff"
        // without the backend having to know how it was reached. Empty on a LAN-only host.
        if (!string.IsNullOrWhiteSpace(_publicUrl))
        {
            info.Environment["LOSLMS_PUBLIC_URL"] = _publicUrl;
        }

        var process = Process.Start(info)
            ?? throw new InvalidOperationException($"Could not start {Paths.BackendExe}.");
        _backendStartedAt = DateTime.UtcNow;
        Log.Info($"Backend started (PID {process.Id}) on {LocalUrl}.");
        return process;
    }

    private static void StopBackend(Process? process)
    {
        try
        {
            if (process is { HasExited: false })
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(30_000);
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Error stopping the backend: {ex.Message}");
        }
    }

    private async Task WaitUntilServingAsync(CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow + HttpReadyTimeout;
        Exception? last = null;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            if (_backend is { HasExited: true })
            {
                throw new InvalidOperationException(
                    $"The backend exited during startup (code {_backend.ExitCode}). See the log.");
            }

            try
            {
                // Any HTTP response — even a redirect to the login page — proves it is actually serving.
                using var response = await http.GetAsync(LocalUrl, ct);
                Log.Info($"Backend is serving ({(int)response.StatusCode}).");
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                await Task.Delay(500, ct);
            }
        }

        throw new TimeoutException(
            $"The backend did not serve a response within {HttpReadyTimeout.TotalSeconds:0}s. Last error: {last?.Message}");
    }

    // ---- update swap, with rollback (lifted from LosLms.Watchdog) ----------------------------------

    private void ApplyUpdate()
    {
        string zipPath;
        try
        {
            zipPath = File.ReadAllText(Paths.ApplySignal).Trim();
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not read the apply signal: {ex.Message}. Ignoring it.");
            TryDelete(Paths.ApplySignal);
            return;
        }

        TryDelete(Paths.ApplySignal); // consume up front so a failure can't re-trigger forever
        Log.Info($"Apply requested. Staged build: {zipPath}");

        if (!File.Exists(zipPath))
        {
            Log.Error($"Staged update not found at {zipPath}. Keeping the current version running.");
            return;
        }

        Directory.CreateDirectory(_applyTempDir);
        var stagedZip = Path.Combine(_applyTempDir, "update.zip");
        try
        {
            TryDelete(stagedZip);
            File.Copy(zipPath, stagedZip, overwrite: true);
        }
        catch (Exception ex)
        {
            Log.Error($"Could not stage the update zip: {ex.Message}. Keeping the current version running.");
            return;
        }

        Log.Info("Stopping the backend to apply the update…");
        StopBackend(_backend);
        DeleteDirIfExists(_backupDir);

        try
        {
            Directory.Move(Paths.AppDir, _backupDir);
        }
        catch (Exception ex)
        {
            Log.Error($"Could not move the current install aside: {ex.Message}. Restarting the current version.");
            _backend = StartBackend();
            BackendRestarted?.Invoke();
            DeleteDirIfExists(_applyTempDir);
            return;
        }

        try
        {
            Directory.CreateDirectory(Paths.AppDir);
            ZipFile.ExtractToDirectory(stagedZip, Paths.AppDir, overwriteFiles: true);

            foreach (var name in Preserve)
            {
                var from = Path.Combine(_backupDir, name);
                var to = Path.Combine(Paths.AppDir, name);
                if (File.Exists(from))
                {
                    File.Copy(from, to, overwrite: true);
                }
                else if (Directory.Exists(from))
                {
                    CopyDirectory(from, to);
                }
            }

            if (!File.Exists(Paths.BackendExe))
            {
                throw new FileNotFoundException("the extracted build has no LosLms.exe", Paths.BackendExe);
            }

            _backend = StartBackend();
            Thread.Sleep(8000);
            if (_backend.HasExited)
            {
                throw new InvalidOperationException($"the updated backend exited immediately (code {_backend.ExitCode}).");
            }

            DeleteDirIfExists(_backupDir);
            DeleteDirIfExists(_applyTempDir);
            BackendRestarted?.Invoke();
            Log.Info("Update applied successfully. Now running the new version.");
        }
        catch (Exception ex)
        {
            Log.Error($"Applying the update failed: {ex.Message}. Rolling back.");
            try
            {
                DeleteDirIfExists(Paths.AppDir);
                Directory.Move(_backupDir, Paths.AppDir);
                _backend = StartBackend();
                BackendRestarted?.Invoke();
                DeleteDirIfExists(_applyTempDir);
                Log.Info("Rollback complete. The previous version is running again.");
            }
            catch (Exception rollbackEx)
            {
                Log.Error(
                    $"CRITICAL: rollback failed: {rollbackEx.Message}. The previous install is preserved at "
                    + $"{_backupDir} — restore it manually (rename it back to 'app').");
            }
        }
    }

    // ---- helpers -----------------------------------------------------------------------------------

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }

    private static void DeleteDirIfExists(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }
    }
}
