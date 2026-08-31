using System.Windows.Forms;
using LosLms.Shell;

namespace LosLms.Server;

/// <summary>
/// Entry point for the Server package. Shows the shared desktop shell immediately (so the user always
/// sees a real window, never a terminal), then orchestrates MySQL → backend → tunnel behind a splash,
/// and finally points the shell at the local backend. Everything is torn down cleanly when the window
/// closes.
/// </summary>
internal static class Program
{
    // Guards against a second launch on the same machine — two servers sharing one MySQL data
    // directory would corrupt it.
    private static readonly string MutexName = "LosLms.Server.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var isNew);
        if (!isNew)
        {
            MessageBox.Show(
                "LOS/LMS Server is already running on this machine.",
                "LOS/LMS Server",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Log.Info("==== LOS/LMS Server launcher starting ====");

        var config = ServerConfig.Load();
        var shell = new ShellWindow("LOS/LMS");

        Orchestrator? orchestrator = null;
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
                    ex.Message + "\n\nSee server-launcher.log next to the app for details.",
                    "Quit",
                    () => shell.Close());
            }
        };

        shell.FormClosing += (_, _) =>
        {
            orchestrator!.Shutdown();
            tray.Dispose();
        };

        Application.Run(shell);
        Log.Info("==== LOS/LMS Server launcher exited ====");
    }
}
