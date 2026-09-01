using LosLms.Shell;

namespace LosLms.Server;

/// <summary>
/// The client role: no database, no backend. Reads the current server URL from the fixed public
/// location, opens the shared shell against it, shows a friendly retry panel when the server can't be
/// reached, and prompts a reconnect when the host restarts (its tunnel URL changes).
/// </summary>
internal sealed class ClientRunner
{
    private static readonly TimeSpan ReconnectPollInterval = TimeSpan.FromSeconds(30);

    private readonly ShellWindow _shell;
    private readonly UrlSource _urlSource = new(ClientConfig.Load());
    private readonly CancellationTokenSource _cts = new();
    private string? _currentUrl;

    public ClientRunner(ShellWindow shell)
    {
        _shell = shell;
        // A navigation that fails (published URL not responding) gets the friendly panel, never a raw
        // browser error page.
        _shell.NavigationFailed += ShowCantReach;
        _shell.FormClosing += (_, _) => _cts.Cancel();
    }

    public async Task StartAsync()
    {
        await _shell.InitializeAsync();
        await ConnectAsync();
        _ = PollForServerRestartAsync();
    }

    private async Task ConnectAsync()
    {
        _shell.ShowConnecting("Connecting…", "Finding the server.");

        var url = await _urlSource.FetchAsync(_cts.Token);
        if (url is null)
        {
            ShowCantReach();
            return;
        }

        _currentUrl = url;
        await _shell.NavigateAsync(url);
    }

    private void ShowCantReach() =>
        _shell.ShowError(
            "Can't reach the server right now",
            "Check your internet connection, or contact your admin.\n\n"
                + "The server may be starting up or temporarily offline — try again in a moment.",
            "Retry",
            () => _ = ConnectAsync());

    // Detect the host having restarted (which mints a new tunnel URL) and offer a reconnect rather than
    // leaving the user on a dead circuit with no explanation.
    private async Task PollForServerRestartAsync()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                await Task.Delay(ReconnectPollInterval, _cts.Token);

                var latest = await _urlSource.FetchAsync(_cts.Token);
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
