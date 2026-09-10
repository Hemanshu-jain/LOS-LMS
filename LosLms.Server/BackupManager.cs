using System.Diagnostics;
using System.IO.Compression;
using System.Net;

namespace LosLms.Server;

/// <summary>
/// Takes an automatic gzipped mysqldump of the database on a schedule, keeps a rotating set locally, and
/// — when FTP details are configured in server-config.json — uploads each dump offsite so a copy survives
/// even if this machine is lost. The live database itself never leaves localhost; only the backup copy
/// goes out.
/// </summary>
internal sealed class BackupManager
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan FirstDelay = TimeSpan.FromMinutes(5); // let startup settle first
    private const int KeepLocal = 14;

    private readonly int _port;
    private readonly DbCredentials _credentials;
    private readonly ServerConfig _config;
    private System.Threading.Timer? _timer;

    public BackupManager(int port, DbCredentials credentials, ServerConfig config)
    {
        _port = port;
        _credentials = credentials;
        _config = config;
    }

    public void Start()
    {
        if (!File.Exists(Paths.MysqldumpExe))
        {
            Log.Warn($"mysqldump not found at {Paths.MysqldumpExe}; automatic backups are disabled.");
            return;
        }

        _timer = new System.Threading.Timer(_ => RunSafely(), null, FirstDelay, Interval);
        Log.Info(
            $"Automatic backups on (every {Interval.TotalHours:0}h). Local: {Paths.BackupsDir}. "
            + (_config.HasBackupUpload ? "Offsite: FTP configured." : "Offsite: not configured (local copies only)."));
    }

    public void Stop() => _timer?.Dispose();

    private void RunSafely()
    {
        try
        {
            RunOnce();
        }
        catch (Exception ex)
        {
            Log.Error("Automatic backup failed", ex);
        }
    }

    private void RunOnce()
    {
        Directory.CreateDirectory(Paths.BackupsDir);
        var name = $"los_lms-{DateTime.Now:yyyyMMdd-HHmmss}.sql.gz";
        var path = Path.Combine(Paths.BackupsDir, name);

        Dump(path);
        Log.Info($"Backup written: {name} ({new FileInfo(path).Length / 1024} KB).");

        Rotate();

        if (_config.HasBackupUpload)
        {
            Upload(path, name);
        }
    }

    private void Dump(string gzPath)
    {
        var info = new ProcessStartInfo
        {
            FileName = Paths.MysqldumpExe,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        info.ArgumentList.Add("--host=127.0.0.1");
        info.ArgumentList.Add($"--port={_port}");
        info.ArgumentList.Add($"--user={DbCredentials.AppUser}");
        info.ArgumentList.Add("--single-transaction"); // consistent snapshot without locking the app out
        info.ArgumentList.Add("--routines");
        info.ArgumentList.Add("--databases");
        info.ArgumentList.Add(DbCredentials.Database);
        // Password via env, not the argument list, so it never shows in the process table.
        info.Environment["MYSQL_PWD"] = _credentials.AppPassword;

        using var proc = Process.Start(info)
            ?? throw new InvalidOperationException("Could not start mysqldump.");

        using (var file = File.Create(gzPath))
        using (var gz = new GZipStream(file, CompressionLevel.Optimal))
        {
            proc.StandardOutput.BaseStream.CopyTo(gz);
        }

        var err = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            TryDelete(gzPath);
            throw new InvalidOperationException($"mysqldump exited {proc.ExitCode}: {err.Trim()}");
        }
    }

    private static void Rotate()
    {
        var stale = new DirectoryInfo(Paths.BackupsDir)
            .GetFiles("los_lms-*.sql.gz")
            .OrderByDescending(f => f.Name)
            .Skip(KeepLocal);

        foreach (var file in stale)
        {
            TryDelete(file.FullName);
        }
    }

    private void Upload(string localPath, string name)
    {
        try
        {
            var dir = string.IsNullOrWhiteSpace(_config.BackupFtpDir)
                ? string.Empty
                : _config.BackupFtpDir!.Trim('/') + "/";
            var uri = $"ftp://{_config.BackupFtpHost}/{dir}{name}";

#pragma warning disable SYSLIB0014 // FtpWebRequest is obsolete but is the only BCL FTP client; no dependency wanted here.
            var request = (FtpWebRequest)WebRequest.Create(uri);
#pragma warning restore SYSLIB0014
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(_config.BackupFtpUser, _config.BackupFtpPassword);
            request.UseBinary = true;
            request.KeepAlive = false;

            using (var reqStream = request.GetRequestStream())
            using (var file = File.OpenRead(localPath))
            {
                file.CopyTo(reqStream);
            }

            using var response = (FtpWebResponse)request.GetResponse();
            Log.Info($"Backup uploaded offsite: {name} ({response.StatusDescription?.Trim()}).");
        }
        catch (Exception ex)
        {
            Log.Warn($"Offsite backup upload failed ({ex.Message}). The local copy is kept.");
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // best effort
        }
    }
}
