using System.Windows.Forms;
using LosLms.Shell;

namespace LosLms.Server;

/// <summary>
/// Entry point for the single LOS/LMS app. On the very first launch it asks whether this computer is
/// the host or a staff client (once per device, then remembered). From then on it runs straight into
/// that role: the host sets up MySQL + backend + tunnel and shows the app locally; the client just
/// opens the fixed address. Either way the user only ever double-clicks LOS-LMS.exe.
/// </summary>
internal static class Program
{
    // Guards a second HOST launch on one machine — two hosts sharing one MySQL data dir would corrupt it.
    private const string HostMutexName = "LosLms.Host.SingleInstance";

    // A second launch of the host sets this so the already-running instance brings its window back.
    private const string ShowEventName = "LosLms.Host.ShowWindow";

    // Set only by tray "Quit" (or a fatal error): tells the close handler to really shut down rather
    // than hide to the tray.
    private static bool _reallyQuit;

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
            // Already running (most likely minimised to the tray). Signal that instance to show its
            // window, then exit quietly — reopening is instant because nothing was ever stopped.
            try
            {
                if (EventWaitHandle.TryOpenExisting(ShowEventName, out var existing))
                {
                    existing.Set();
                    existing.Dispose();
                }
            }
            catch { /* best effort */ }
            return;
        }

        Log.Info("==== LOS/LMS starting (host role) ====");

        var config = ServerConfig.Load();
        var shell = new ShellWindow("LOS/LMS — Server");

        Orchestrator orchestrator = null!;
        var tray = new TrayController(
            onOpenWindow: () => ShowWindow(shell),
            onQuit: () =>
            {
                _reallyQuit = true;
                shell.Close();
            });

        orchestrator = new Orchestrator(shell, tray, config);

        // Bring the window back when a second launch signals it. Background waiter; dies with the process.
        using var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        var showThread = new Thread(() =>
        {
            while (true)
            {
                try
                {
                    showEvent.WaitOne();
                    shell.BeginInvoke(() => ShowWindow(shell));
                }
                catch
                {
                    break; // window/event disposed on shutdown
                }
            }
        })
        {
            IsBackground = true,
            Name = "show-window-waiter",
        };
        showThread.Start();

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
                    ex.Message + "\n\nSee LOS-LMS.log inside the app folder for details.",
                    "Quit",
                    () => { _reallyQuit = true; shell.Close(); });
            }
        };

        shell.FormClosing += (_, e) =>
        {
            if (!_reallyQuit)
            {
                // Closing the window hides it to the tray and leaves MySQL, the backend, and the tunnel
                // running — so opening it again is instant. Only tray "Quit" (or a reboot) really stops.
                e.Cancel = true;
                shell.Hide();
                return;
            }

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

    private static void ShowWindow(ShellWindow shell)
    {
        shell.Show();
        shell.WindowState = FormWindowState.Normal;
        shell.BringToFront();
        shell.Activate();
    }
}
