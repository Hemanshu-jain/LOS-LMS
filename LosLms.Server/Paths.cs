namespace LosLms.Server;

/// <summary>
/// Every path the app works with, all derived from the install root (the folder LOS-LMS.exe lives in).
///
/// The extracted package is deliberately tidy — the install root holds only the exe, a read-me, the
/// config template, and a single <c>server\</c> folder. Everything the host role needs (the bundled
/// MySQL, the backend, cloudflared) and everything it creates at runtime (the database, credentials)
/// lives under <c>server\</c>, so a staff machine that only ever runs the client role never sees any of
/// it as clutter.
/// </summary>
internal static class Paths
{
    /// <summary>Folder containing LOS-LMS.exe — the install root.</summary>
    public static string InstallRoot { get; } = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

    /// <summary>Host-role components + runtime state; untouched on a client-only machine.</summary>
    public static string ServerDir { get; } = Path.Combine(InstallRoot, "server");

    /// <summary>The backend server folder (swapped wholesale on update).</summary>
    public static string AppDir { get; } = Path.Combine(ServerDir, "backend");

    public static string BackendExe { get; } = Path.Combine(AppDir, "LosLms.exe");

    /// <summary>Bundled portable MySQL base dir (contains bin\mysqld.exe).</summary>
    public static string MysqlDir { get; } = Path.Combine(ServerDir, "mysql");

    public static string MysqldExe { get; } = Path.Combine(MysqlDir, "bin", "mysqld.exe");

    public static string MysqladminExe { get; } = Path.Combine(MysqlDir, "bin", "mysqladmin.exe");

    public static string MysqldumpExe { get; } = Path.Combine(MysqlDir, "bin", "mysqldump.exe");

    /// <summary>MySQL data dir — created on first host run, preserved forever.</summary>
    public static string MysqlDataDir { get; } = Path.Combine(ServerDir, "mysql-data");

    /// <summary>Nightly database dumps (gzipped), rotated locally and optionally uploaded offsite.</summary>
    public static string BackupsDir { get; } = Path.Combine(ServerDir, "backups");

    /// <summary>Generated database credentials, written once on the first host run.</summary>
    public static string CredentialsFile { get; } = Path.Combine(ServerDir, ".db-credentials");

    /// <summary>Bundled (or first-run-downloaded) Cloudflare tunnel client.</summary>
    public static string CloudflaredExe { get; } = Path.Combine(ServerDir, "cloudflared.exe");

    /// <summary>Operator-provided config (GitHub token). Top-level so the host operator finds it.</summary>
    public static string ServerConfigFile { get; } = Path.Combine(InstallRoot, "server-config.json");

    /// <summary>Plainly-named file holding the current shareable tunnel URL (host only).</summary>
    public static string ShareableUrlFile { get; } = Path.Combine(InstallRoot, "shareable-url.txt");

    /// <summary>App log — the windows are hidden, so this is the record of what happened.</summary>
    public static string LogFile { get; } = Path.Combine(InstallRoot, "LOS-LMS.log");

    /// <summary>Update staging the backend writes downloads + apply.signal into.</summary>
    public static string UpdatesDir { get; } = Path.Combine(AppDir, "updates");

    public static string ApplySignal { get; } = Path.Combine(UpdatesDir, "apply.signal");
}
