using System.Diagnostics;
using MySqlConnector;

namespace LosLms.Server;

/// <summary>
/// Owns the bundled, portable MySQL. On first run it initialises a fresh data directory and bootstraps
/// the database and application user with generated credentials (the operator configures nothing). On
/// every run it starts <c>mysqld</c> windowless, bound to 127.0.0.1 only, on a free port, and waits
/// for a REAL connection to succeed before reporting ready. It also shuts MySQL down gracefully.
/// </summary>
internal sealed class MySqlManager
{
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(60);

    private Process? _mysqld;
    private DbCredentials _credentials = null!;

    public int Port { get; private set; }

    public DbCredentials Credentials => _credentials;

    /// <summary>
    /// Brings MySQL up and returns once a genuine connection has succeeded. Throws on any failure so
    /// the orchestrator stops before starting the backend against an absent database.
    /// </summary>
    public async Task StartAsync(Action<string> progress, CancellationToken ct)
    {
        if (!File.Exists(Paths.MysqldExe))
        {
            throw new FileNotFoundException(
                $"Bundled MySQL not found at {Paths.MysqldExe}. The Server package is incomplete.",
                Paths.MysqldExe);
        }

        Port = PortFinder.FindFree(3306);
        Log.Info($"MySQL will use port {Port}.");

        var firstRun = !Directory.Exists(Paths.MysqlDataDir) || IsEmptyDir(Paths.MysqlDataDir);

        string? bootstrapFile = null;
        if (firstRun)
        {
            progress("Setting up the database for the first time…");
            _credentials = DbCredentials.Generate();
            Initialize();
            bootstrapFile = WriteBootstrapFile(_credentials);
        }
        else
        {
            _credentials = DbCredentials.TryLoad()
                ?? throw new InvalidOperationException(
                    "The MySQL data directory exists but its credentials file is missing. "
                    + $"Delete {Paths.MysqlDataDir} to re-initialise, or restore {Paths.CredentialsFile}.");
        }

        progress("Starting the database…");
        _mysqld = StartMysqld(bootstrapFile);

        await WaitUntilAcceptingConnectionsAsync(progress, ct);

        if (firstRun)
        {
            // The bootstrap file held the generated passwords in clear text — remove it now that the
            // accounts exist, and persist the credentials for every future run.
            TryDelete(bootstrapFile!);
            _credentials.Save();
            Log.Info("First-run database bootstrap complete; credentials saved.");
        }
    }

    public string AppConnectionString => _credentials.AppConnectionString(Port);

    /// <summary>Graceful shutdown via mysqladmin, falling back to a kill if it does not stop in time.</summary>
    public void Stop()
    {
        if (_mysqld is null || _mysqld.HasExited)
        {
            return;
        }

        try
        {
            if (File.Exists(Paths.MysqladminExe))
            {
                var info = new ProcessStartInfo
                {
                    FileName = Paths.MysqladminExe,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };
                info.ArgumentList.Add("--protocol=tcp");
                info.ArgumentList.Add("--host=127.0.0.1");
                info.ArgumentList.Add($"--port={Port}");
                info.ArgumentList.Add($"--user={_credentials.AppUser}");
                info.ArgumentList.Add("shutdown");
                info.Environment["MYSQL_PWD"] = _credentials.AppPassword; // keep the password off the command line
                var admin = Process.Start(info);
                admin?.WaitForExit(10_000);
            }

            if (!_mysqld.WaitForExit(15_000))
            {
                Log.Warn("MySQL did not stop gracefully in time; killing it.");
                _mysqld.Kill(entireProcessTree: true);
            }
            else
            {
                Log.Info("MySQL stopped gracefully.");
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error stopping MySQL; killing it", ex);
            try { _mysqld.Kill(entireProcessTree: true); } catch { /* best effort */ }
        }
    }

    // ---- first-run initialisation ------------------------------------------------------------------

    private static void Initialize()
    {
        Log.Info("Initialising a fresh MySQL data directory (first run).");
        Directory.CreateDirectory(Paths.MysqlDataDir);

        var info = new ProcessStartInfo
        {
            FileName = Paths.MysqldExe,
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardError = true,
        };
        info.ArgumentList.Add($"--basedir={Paths.MysqlDir}");
        info.ArgumentList.Add($"--datadir={Paths.MysqlDataDir}");
        info.ArgumentList.Add("--initialize-insecure"); // root@localhost with no password, secured by --init-file next
        info.ArgumentList.Add("--console");

        var process = Process.Start(info)
            ?? throw new InvalidOperationException("Could not start mysqld to initialise the data directory.");
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"MySQL initialisation failed (exit {process.ExitCode}). Details:\n{stderr}");
        }
    }

    // A one-shot SQL script mysqld runs, as a privileged internal user, on its first normal start.
    // Doing the bootstrap this way avoids the Windows "localhost vs 127.0.0.1" auth mismatch that a
    // TCP root connection would hit right after an insecure initialise.
    private static string WriteBootstrapFile(DbCredentials creds)
    {
        var path = Path.Combine(Paths.InstallRoot, "_bootstrap.sql");
        var sql =
            $"ALTER USER 'root'@'localhost' IDENTIFIED BY '{creds.RootPassword}';\n" +
            $"CREATE DATABASE IF NOT EXISTS {DbCredentials.Database} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;\n" +
            // The app connects over TCP from 127.0.0.1, so the account host must be the literal IP.
            $"CREATE USER IF NOT EXISTS '{creds.AppUser}'@'127.0.0.1' IDENTIFIED BY '{creds.AppPassword}';\n" +
            $"GRANT ALL PRIVILEGES ON {DbCredentials.Database}.* TO '{creds.AppUser}'@'127.0.0.1';\n" +
            // SHUTDOWN is global — it lets the launcher stop MySQL gracefully as the app user, so no
            // root-over-TCP connection is ever needed.
            $"GRANT SHUTDOWN ON *.* TO '{creds.AppUser}'@'127.0.0.1';\n" +
            "FLUSH PRIVILEGES;\n";
        File.WriteAllText(path, sql);
        return path;
    }

    // ---- run ---------------------------------------------------------------------------------------

    private Process StartMysqld(string? bootstrapFile)
    {
        var info = new ProcessStartInfo
        {
            FileName = Paths.MysqldExe,
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        info.ArgumentList.Add($"--basedir={Paths.MysqlDir}");
        info.ArgumentList.Add($"--datadir={Paths.MysqlDataDir}");
        info.ArgumentList.Add("--bind-address=127.0.0.1"); // localhost ONLY — never network-exposed
        info.ArgumentList.Add($"--port={Port}");
        info.ArgumentList.Add("--skip-name-resolve");
        // Disable the X Protocol plugin entirely. The app uses only the classic protocol, and left on,
        // mysqlx defaults to binding '::' (all interfaces) on 33060 — which would expose the database
        // beyond localhost. The whole point is that other machines reach the app, never the database.
        info.ArgumentList.Add("--skip-mysqlx");
        info.ArgumentList.Add("--console");
        if (bootstrapFile is not null)
        {
            info.ArgumentList.Add($"--init-file={bootstrapFile}");
        }

        var process = Process.Start(info)
            ?? throw new InvalidOperationException("Could not start mysqld.");

        // Drain the pipes so the process never blocks on a full buffer; route anything interesting to the log.
        process.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log.Info($"[mysqld] {e.Data}"); };
        process.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log.Info($"[mysqld] {e.Data}"); };
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        Log.Info($"mysqld started (PID {process.Id}) on 127.0.0.1:{Port}.");
        return process;
    }

    private async Task WaitUntilAcceptingConnectionsAsync(Action<string> progress, CancellationToken ct)
    {
        progress("Waiting for the database to be ready…");
        var connectionString = AppConnectionString;
        var deadline = DateTime.UtcNow + ReadyTimeout;
        Exception? last = null;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            if (_mysqld is { HasExited: true })
            {
                throw new InvalidOperationException(
                    $"mysqld exited unexpectedly during startup (code {_mysqld.ExitCode}). See the log.");
            }

            try
            {
                await using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync(ct);
                await using var command = new MySqlCommand("SELECT 1;", connection);
                await command.ExecuteScalarAsync(ct);
                Log.Info("MySQL is accepting connections.");
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                await Task.Delay(500, ct);
            }
        }

        throw new TimeoutException(
            $"MySQL did not accept a connection within {ReadyTimeout.TotalSeconds:0}s. Last error: {last?.Message}");
    }

    // ---- helpers -----------------------------------------------------------------------------------

    private static bool IsEmptyDir(string dir) =>
        !Directory.EnumerateFileSystemEntries(dir).Any();

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }
}
