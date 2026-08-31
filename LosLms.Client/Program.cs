using System.Windows.Forms;
using LosLms.Shell;

namespace LosLms.Client;

/// <summary>
/// Entry point for the Client Shell package. No database, no backend, no local state. It reads the
/// current server URL from the fixed public location, opens the shared WebView2 shell against it, and
/// keeps an eye out for the server restarting (a new tunnel URL) so the user is prompted to reconnect
/// rather than left staring at a dead connection.
/// </summary>
internal static class Program
{
    private static readonly TimeSpan ReconnectPollInterval = TimeSpan.FromSeconds(30);

    private static ShellWindow _shell = null!;
    private static UrlSource _urlSource = null!;
    private static string? _currentUrl;
    private static readonly CancellationTokenSource Cts = new();

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var config = ClientConfig.Load();
        _urlSource = new UrlSource(config);
        _shell = new ShellWindow("LOS/LMS");

        // A navigation that fails (published URL not responding) gets the friendly panel, never a raw
        // browser error page.
        _shell.NavigationFailed += ShowCantReach;

        _shell.Shown += async (_, _) =>
        {
            await _shell.InitializeAsync();
            await ConnectAsync();
            _ = PollForServerRestartAsync();
        };

        _shell.FormClosing += (_, _) => Cts.Cancel();

        Application.Run(_shell);
    }

    private static async Task ConnectAsync()
    {
        _shell.ShowConnecting("Connecting…", "Finding the server.");

        var url = await _urlSource.FetchAsync(Cts.Token);
        if (url is null)
        {
            ShowCantReach();
            return;
        }

        _currentUrl = url;
        await _shell.NavigateAsync(url);
    }

    private static void ShowCantReach() =>
        _shell.ShowError(
            "Can't reach the server right now",
            "Check your internet connection, or contact your admin.\n\n"
                + "The server may be starting up or temporarily offline — try again in a moment.",
            "Retry",
            () => _ = ConnectAsync());

    // Detect the Server having restarted (which mints a new tunnel URL) and offer a reconnect rather
    // than leaving the user on a dead circuit with no explanation.
    private static async Task PollForServerRestartAsync()
    {
        try
        {
            while (!Cts.IsCancellationRequested)
            {
                await Task.Delay(ReconnectPollInterval, Cts.Token);

                var latest = await _urlSource.FetchAsync(Cts.Token);
                if (latest is not null && _currentUrl is not null && latest != _currentUrl)
                {
                    _shell.ShowReconnectBar(
                        "The server restarted. Reconnect to continue.",
                        () => _ = ConnectAsync());
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Window closing — stop quietly.
        }
    }
}
