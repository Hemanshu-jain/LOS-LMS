using LosLms.Shell;

namespace LosLms.Server;

/// <summary>
/// The staff (client) role: no database, no backend. Opens the shared shell at the fixed hosted URL
/// (e.g. https://los-lms.bhodhix.com) and shows a friendly retry panel when it can't be reached. The URL
/// is permanent now, so there is no discovery or reconnect-on-URL-change to do — a restart of the host
/// keeps the very same address.
/// </summary>
internal sealed class ClientRunner
{
    private readonly ShellWindow _shell;
    private readonly string _hostedUrl;

    public ClientRunner(ShellWindow shell, string hostedUrl)
    {
        _shell = shell;
        _hostedUrl = hostedUrl;
        // A navigation that fails (server briefly down, no internet) gets the friendly panel, not a raw
        // browser error page.
        _shell.NavigationFailed += ShowCantReach;
    }

    public async Task StartAsync()
    {
        await _shell.InitializeAsync();
        await ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        _shell.ShowConnecting("Connecting…", "Opening LOS/LMS.");
        await _shell.NavigateAsync(_hostedUrl);
    }

    private void ShowCantReach() =>
        _shell.ShowError(
            "Can't reach the server right now",
            "Check your internet connection, or contact your admin.\n\n"
                + "The server may be restarting or temporarily offline — try again in a moment.",
            "Retry",
            () => _ = ConnectAsync());
}
