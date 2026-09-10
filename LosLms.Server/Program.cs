using System.Windows.Forms;
using LosLms.Shell;

namespace LosLms.Server;

/// <summary>
/// Entry point for the single LOS/LMS app. On the very first launch it asks whether this computer is
/// the host or a staff client (once per device, then remembered). From then on it runs straight into
/// that role: the host sets up MySQL + backend + tunnel and shows the app locally; the client just
/// finds the host and connects. Either way the user only ever double-clicks LOS-LMS.exe.
/// </summary>
internal static class Program
{
    // Guards a second HOST launch on one machine — two hosts sharing one MySQL data dir would corrupt it.
    private const string HostMutexName = "LosLms.Host.SingleInstance";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var role = RoleStore.Get();
        if (role is null)
        {
            role = RolePicker.Ask();
            if (role is null)
            {
                return; // first run, closed without choosing
            }

            RoleStore.Set(role);
        }

        if (role == RoleStore.Host)
        {
            RunHost();
        }
        else
        {
            RunClient();
        }
    }

    // ---- Host role: this computer runs the whole system ----------------------------------------------

    private static void RunHost()
    {
        using var mutex = new Mutex(initiallyOwned: true, HostMutexName, out var isNew);
        if (!isNew)
        {
            MessageBox.Show(
                "LOS/LMS is already running on this computer.",
                "LOS/LMS", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Log.Info("==== LOS/LMS starting (host role) ====");

        var config = ServerConfig.Load();
        var shell = new ShellWindow("LOS/LMS — Server");

        Orchestrator orchestrator = null!;
        var tray = new TrayController(
            onOpenWindow: () =>
            {
                shell.Show();
                shell.WindowState = FormWindowState.Normal;
                shell.BringToFront();
                shell.Activate();
            },
            onQuit: () => shell.Close());

        orchestrator = new Orchestrator(shell, tray, config);

        shell.Shown += async (_, _) =>
        {
            try
            {
                await shell.InitializeAsync();
                await orchestrator.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Error("Fatal error during startup", ex);
                shell.ShowError(
                    "Something went wrong starting LOS/LMS",
                    ex.Message + "\n\nSee LOS-LMS.log next to the app for details.",
                    "Quit",
                    () => shell.Close());
            }
        };

        shell.FormClosing += (_, _) =>
        {
            orchestrator.Shutdown();
            tray.Dispose();
        };

        Application.Run(shell);
        Log.Info("==== LOS/LMS exited (host role) ====");
    }

    // ---- Client role: this computer connects to the host --------------------------------------------

    private static void RunClient()
    {
        var config = ServerConfig.Load();
        var shell = new ShellWindow("LOS/LMS");
        var client = new ClientRunner(shell, config.HostedUrl);
        shell.Shown += async (_, _) => await client.StartAsync();
        Application.Run(shell);
    }
}
