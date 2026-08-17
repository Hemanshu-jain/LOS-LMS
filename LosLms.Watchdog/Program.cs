using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

// LOS/LMS watchdog. Launches the main server as a child process, keeps it running, and — only when the
// app writes an apply signal (which it does only after an explicit SuperAdmin confirmation) — swaps in
// a downloaded build and restarts.
//
// The whole reason this process exists: a running Windows exe cannot overwrite itself, so the server
// can't apply its own update. The watchdog can, because it is a separate, never-swapped executable.
//
// FAIL SAFE IS THE POINT. Every swap backs up the current install first and, on ANY failure, restores
// it and restarts the old build. The install is never left half-swapped.

// ---- Layout (overridable by args) --------------------------------------------------------------
//   <install-root>\
//       LosLms.Watchdog.exe        <- this process; never swapped
//       app\                       <- the main server (LosLms.exe, wwwroot, appsettings.json, DLLs)
//       app\updates\               <- staging the app writes downloads + apply.signal into
//
var baseDir = AppContext.BaseDirectory;
var appDir = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(baseDir, "app");
var exeName = args.Length > 1 ? args[1] : "LosLms.exe";
var exePath = Path.Combine(appDir, exeName);

var updatesDir = Path.Combine(appDir, "updates");
var signalPath = Path.Combine(updatesDir, "apply.signal");   // must match UpdateService.ApplySignalFileName
var applyTempDir = Path.Combine(baseDir, "_apply");           // outside app\ so the swap can't lose it
var backupDir = Path.Combine(baseDir, "_backup");            // outside app\

// Files/folders that carry the operator's own state and must survive an update (the published zip
// ships a fresh appsettings.json with a placeholder connection string — clobbering it would wipe the
// client's real one; App_Data holds uploaded PII documents).
string[] preserve = { "appsettings.json", "App_Data" };

Log($"Watchdog starting. Managing: {exePath}");

if (!File.Exists(exePath))
{
    Log($"ERROR: main server exe not found at {exePath}. Put the published app in an 'app' subfolder " +
        "next to this watchdog, or pass its folder as the first argument. Exiting.");
    return 1;
}

var child = StartApp();

// Open the operator's browser to the app once, on this first launch only — not on crash-restarts or
// update-restarts, which would keep popping new tabs. This is what makes it a true double-click:
// click the exe, the app opens.
OpenBrowserOnce();

// ---- Supervise loop ----------------------------------------------------------------------------
while (true)
{
    Thread.Sleep(2000);

    if (File.Exists(signalPath))
    {
        child = ApplyUpdate(child);
        continue;
    }

    if (child is { HasExited: true })
    {
        Log($"Main server exited unexpectedly (code {child.ExitCode}). Restarting.");
        child = StartApp();
    }
}

// ---- Child process lifecycle -------------------------------------------------------------------

Process StartApp()
{
    var info = new ProcessStartInfo
    {
        FileName = exePath,
        WorkingDirectory = appDir,
        UseShellExecute = false, // inherit this console, so the server's own startup log (incl. the LAN URL) shows
    };

    var process = Process.Start(info)
        ?? throw new InvalidOperationException($"Could not start {exePath}.");
    Log($"Main server started (PID {process.Id}).");
    return process;
}

void StopApp(Process process)
{
    try
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(30_000);
        }
    }
    catch (Exception ex)
    {
        Log($"WARNING while stopping the server: {ex.Message}");
    }
}

// ---- The update swap, with rollback ------------------------------------------------------------

Process ApplyUpdate(Process current)
{
    string zipPath;
    try
    {
        zipPath = File.ReadAllText(signalPath).Trim();
    }
    catch (Exception ex)
    {
        Log($"Could not read the apply signal: {ex.Message}. Ignoring it.");
        TryDelete(signalPath);
        return current;
    }

    // Consume the signal up front so a failure can't leave it re-triggering forever.
    TryDelete(signalPath);

    Log($"Apply requested. Staged build: {zipPath}");

    if (!File.Exists(zipPath))
    {
        Log($"ERROR: staged update not found at {zipPath}. Keeping the current version running.");
        return current;
    }

    // Move the zip out of app\ before we touch app\ — otherwise moving app\ takes the zip with it.
    Directory.CreateDirectory(applyTempDir);
    var stagedZip = Path.Combine(applyTempDir, "update.zip");
    try
    {
        TryDelete(stagedZip);
        File.Copy(zipPath, stagedZip, overwrite: true);
    }
    catch (Exception ex)
    {
        Log($"ERROR: could not stage the update zip: {ex.Message}. Keeping the current version running.");
        return current;
    }

    // From here the old install is stopped and moved aside. Any failure past this point rolls back.
    Log("Stopping the server to apply the update…");
    StopApp(current);

    DeleteDirIfExists(backupDir); // a leftover from a previous interrupted run

    try
    {
        Directory.Move(appDir, backupDir);
    }
    catch (Exception ex)
    {
        // Nothing has been replaced yet — the old install is still whole. Restart it and bail.
        Log($"ERROR: could not move the current install aside: {ex.Message}. Restarting the current version.");
        var restarted = StartApp();
        DeleteDirIfExists(applyTempDir);
        return restarted;
    }

    try
    {
        Directory.CreateDirectory(appDir);
        ZipFile.ExtractToDirectory(stagedZip, appDir, overwriteFiles: true);

        // Carry the operator's own state across from the backup.
        foreach (var name in preserve)
        {
            var from = Path.Combine(backupDir, name);
            var to = Path.Combine(appDir, name);
            if (File.Exists(from))
            {
                File.Copy(from, to, overwrite: true);
            }
            else if (Directory.Exists(from))
            {
                CopyDirectory(from, to);
            }
        }

        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException($"the extracted build has no {exeName}", exePath);
        }

        var updated = StartApp();

        // Confirm it actually came up rather than crash-looping on the new binaries.
        Thread.Sleep(8000);
        if (updated.HasExited)
        {
            throw new InvalidOperationException($"the updated server exited immediately (code {updated.ExitCode}).");
        }

        // Success — drop the backup and staging.
        DeleteDirIfExists(backupDir);
        DeleteDirIfExists(applyTempDir);
        Log("Update applied successfully. Now running the new version.");
        return updated;
    }
    catch (Exception ex)
    {
        // ROLL BACK: throw away the half-applied app\, put the backup back, restart the old build.
        Log($"ERROR applying the update: {ex.Message}. Rolling back to the previous version.");

        try
        {
            DeleteDirIfExists(appDir);
            Directory.Move(backupDir, appDir);
            var restored = StartApp();
            DeleteDirIfExists(applyTempDir);
            Log("Rollback complete. The previous version is running again.");
            return restored;
        }
        catch (Exception rollbackEx)
        {
            // Worst case: report loudly. The backup is still on disk for manual recovery.
            Log($"CRITICAL: rollback failed: {rollbackEx.Message}. The previous install is preserved at " +
                $"{backupDir} — restore it manually (rename it back to 'app'). The server is NOT running.");
            return current; // exited; the supervise loop will keep trying to restart from appDir if it reappears
        }
    }
}

// ---- First-launch browser open -----------------------------------------------------------------

void OpenBrowserOnce()
{
    var url = $"http://localhost:{ReadPort()}";

    // Fire-and-forget on a background thread: give Kestrel a few seconds to bind before we open the
    // page, without blocking the supervise loop.
    _ = Task.Run(async () =>
    {
        await Task.Delay(4000);
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            Log($"Opened {url} in the default browser.");
        }
        catch (Exception ex)
        {
            Log($"Could not open a browser automatically ({ex.Message}). Open {url} manually.");
        }
    });
}

// The port the server listens on, read from the app's own appsettings.json "Urls" so a changed port
// still opens the right page. Falls back to the default if anything about that is missing.
int ReadPort()
{
    try
    {
        var settings = Path.Combine(appDir, "appsettings.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(settings));
        if (doc.RootElement.TryGetProperty("Urls", out var urls)
            && Uri.TryCreate(urls.GetString()?.Replace("0.0.0.0", "localhost"), UriKind.Absolute, out var u))
        {
            return u.Port;
        }
    }
    catch
    {
        // fall through to the default
    }

    return 5037;
}

// ---- Helpers -----------------------------------------------------------------------------------

static void Log(string message) =>
    Console.WriteLine($"[watchdog {DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");

static void TryDelete(string path)
{
    try { if (File.Exists(path)) { File.Delete(path); } } catch { /* best effort */ }
}

static void DeleteDirIfExists(string dir)
{
    try { if (Directory.Exists(dir)) { Directory.Delete(dir, recursive: true); } } catch { /* best effort */ }
}

static void CopyDirectory(string source, string destination)
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
