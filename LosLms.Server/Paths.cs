namespace LosLms.Server;

/// <summary>
/// Every path the launcher works with, all derived from the install root (the folder the
/// double-clicked exe lives in). The backend, its state, and the operator's config all sit under
/// here in a fixed layout the update swap and MySQL both depend on.
/// </summary>
internal static class Paths
{
    /// <summary>Folder containing "LOS-LMS Server.exe" — the install root.</summary>
    public static string InstallRoot { get; } = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

    /// <summary>The backend server folder (swapped wholesale on update).</summary>
    public static string AppDir { get; } = Path.Combine(InstallRoot, "app");

    /// <summary>The backend executable the launcher starts as a child.</summary>
    public static string BackendExe { get; } = Path.Combine(InstallRoot, "app", "LosLms.exe");

    /// <summary>Bundled portable MySQL base dir (contains bin\mysqld.exe).</summary>
    public static string MysqlDir { get; } = Path.Combine(InstallRoot, "mysql");

    public static string MysqldExe { get; } = Path.Combine(MysqlDir, "bin", "mysqld.exe");

    public static string MysqladminExe { get; } = Path.Combine(MysqlDir, "bin", "mysqladmin.exe");

    /// <summary>MySQL data dir — created on first run, preserved forever (outside app\).</summary>
    public static string MysqlDataDir { get; } = Path.Combine(InstallRoot, "mysql-data");

    /// <summary>Generated database credentials, written once on first run.</summary>
    public static string CredentialsFile { get; } = Path.Combine(InstallRoot, ".db-credentials");

    /// <summary>Bundled (or first-run-downloaded) Cloudflare tunnel client.</summary>
    public static string CloudflaredExe { get; } = Path.Combine(InstallRoot, "cloudflared.exe");

    /// <summary>Operator-provided config (GitHub token + where to publish the URL).</summary>
    public static string ServerConfigFile { get; } = Path.Combine(InstallRoot, "server.json");

    /// <summary>Plainly-named file holding the current shareable tunnel URL.</summary>
    public static string ShareableUrlFile { get; } = Path.Combine(InstallRoot, "shareable-url.txt");

    /// <summary>Launcher log — the windows are hidden, so this is where its progress is recorded.</summary>
    public static string LogFile { get; } = Path.Combine(InstallRoot, "server-launcher.log");

    /// <summary>Update staging the backend writes downloads + apply.signal into.</summary>
    public static string UpdatesDir { get; } = Path.Combine(AppDir, "updates");

    public static string ApplySignal { get; } = Path.Combine(UpdatesDir, "apply.signal");
}
