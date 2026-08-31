using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace LosLms.Shell;

/// <summary>
/// The shared, chrome-less desktop window used by both packages — the Server's local window and the
/// thin Client window. It is a real native window hosting a <see cref="WebView2"/> that fills it
/// completely: no address bar, no tabs, no browser menus. Both callers differ only in which URL they
/// point it at and how they react to connection failures, which they drive through this class's
/// public surface (<see cref="NavigateAsync"/>, <see cref="ShowConnecting"/>,
/// <see cref="ShowError"/>, <see cref="ShowReconnectBar"/>) and the <see cref="NavigationFailed"/>
/// event.
/// </summary>
public sealed class ShellWindow : Form
{
    private const string RuntimeDownloadUrl = "https://developer.microsoft.com/microsoft-edge/webview2/";

    private readonly WebView2 _web = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly Panel _overlay;
    private readonly Label _overlayTitle;
    private readonly Label _overlayMessage;
    private readonly Button _overlayButton;
    private Action? _overlayButtonAction;

    private readonly Panel _reconnectBar;
    private readonly Label _reconnectLabel;
    private readonly Button _reconnectButton;
    private Action? _reconnectAction;

    private readonly string _userDataFolder;
    private bool _coreReady;
    private string? _pendingUrl;

    /// <summary>
    /// Raised when a navigation finishes without success (unreachable host, DNS failure, TLS error,
    /// etc.). The Client uses this to swap in its friendly "can't reach the server" panel instead of
    /// letting a raw browser error page show.
    /// </summary>
    public event Action? NavigationFailed;

    /// <summary>Raised once the WebView2 core is initialised and ready to navigate.</summary>
    public event Action? Ready;

    public ShellWindow(string windowTitle)
    {
        Text = windowTitle;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1280, 820);
        MinimumSize = new Size(900, 600);
        BackColor = Color.FromArgb(17, 24, 39);
        Icon = TryLoadIcon();

        // ---- Full-window overlay (splash / error), drawn on top of the WebView2 ----
        _overlayTitle = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 56,
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        _overlayMessage = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 90,
            Font = new Font("Segoe UI", 11F),
            ForeColor = Color.FromArgb(203, 213, 225),
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(40, 0, 40, 0),
        };
        _overlayButton = new Button
        {
            AutoSize = true,
            Anchor = AnchorStyles.None,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(37, 99, 235),
            FlatStyle = FlatStyle.Flat,
            Padding = new Padding(18, 8, 18, 8),
            Visible = false,
            Cursor = Cursors.Hand,
        };
        _overlayButton.FlatAppearance.BorderSize = 0;
        _overlayButton.Click += (_, _) => _overlayButtonAction?.Invoke();

        var overlayButtonHost = new Panel { Dock = DockStyle.Fill };
        overlayButtonHost.Controls.Add(_overlayButton);
        overlayButtonHost.Resize += (_, _) => CenterOverlayButton(overlayButtonHost);

        _overlay = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(17, 24, 39),
            Visible = false,
        };
        _overlay.Controls.Add(overlayButtonHost);
        _overlay.Controls.Add(_overlayMessage);
        _overlay.Controls.Add(_overlayTitle);

        // ---- Thin, non-blocking top bar used to prompt a reconnect after the server restarts ----
        _reconnectLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 27, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
        };
        _reconnectButton = new Button
        {
            Dock = DockStyle.Right,
            Width = 130,
            Text = "Reconnect",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(180, 83, 9),
            Cursor = Cursors.Hand,
        };
        _reconnectButton.FlatAppearance.BorderSize = 0;
        _reconnectButton.Click += (_, _) => _reconnectAction?.Invoke();
        _reconnectBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Color.FromArgb(251, 191, 36),
            Visible = false,
        };
        _reconnectBar.Controls.Add(_reconnectLabel);
        _reconnectBar.Controls.Add(_reconnectButton);

        // Z-order: WebView2 at the back, reconnect bar above it, overlay on top of everything.
        Controls.Add(_web);
        Controls.Add(_reconnectBar);
        Controls.Add(_overlay);
        _overlay.BringToFront();

        _userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LOS-LMS", "WebView2");

        _web.NavigationCompleted += OnNavigationCompleted;
        _web.CoreWebView2InitializationCompleted += OnCoreInitializationCompleted;
    }

    /// <summary>
    /// Creates the WebView2 environment against a per-user data folder and initialises the control.
    /// If the Evergreen runtime is missing, shows a clear message with a link rather than crashing.
    /// Safe to call once, after the window handle exists (e.g. from <c>OnShown</c> or after Show()).
    /// </summary>
    public async Task InitializeAsync()
    {
        ShowConnecting("Starting…", "Preparing the application window.");
        try
        {
            Directory.CreateDirectory(_userDataFolder);
            var environment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null, userDataFolder: _userDataFolder);
            await _web.EnsureCoreWebView2Async(environment);
        }
        catch (Exception ex)
        {
            ShowError(
                "Can't start the app window",
                "The Microsoft Edge WebView2 runtime is required and could not be started.\n\n"
                    + ex.Message,
                "Get the runtime",
                () => OpenExternally(RuntimeDownloadUrl));
        }
    }

    private void OnCoreInitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            return; // the catch in InitializeAsync has already shown the runtime message
        }

        // Strip the last of the browser feel: no default right-click menu, no dev tools, no status bar.
        var settings = _web.CoreWebView2.Settings;
        settings.AreDefaultContextMenusEnabled = false;
        settings.AreDevToolsEnabled = false;
        settings.IsStatusBarEnabled = false;
        settings.AreBrowserAcceleratorKeysEnabled = false;

        // Keep everything inside this one window: a target=_blank or window.open lands in-place rather
        // than trying to spawn a second, chrome-less popup the user can't navigate.
        _web.CoreWebView2.NewWindowRequested += (_, args) =>
        {
            args.Handled = true;
            if (!string.IsNullOrEmpty(args.Uri))
            {
                _web.CoreWebView2.Navigate(args.Uri);
            }
        };

        _coreReady = true;
        Ready?.Invoke();

        if (_pendingUrl is { } url)
        {
            _pendingUrl = null;
            _ = NavigateAsync(url);
        }
    }

    /// <summary>
    /// Navigates to <paramref name="url"/>, showing the connecting overlay until it loads. Safe to call
    /// from any thread — the WebView2 call is marshalled onto the UI thread.
    /// </summary>
    public Task NavigateAsync(string url)
    {
        if (!_coreReady)
        {
            // Called before the core finished initialising — remember it and go as soon as it is ready.
            _pendingUrl = url;
            return Task.CompletedTask;
        }

        RunOnUi(() =>
        {
            ShowConnecting("Connecting…", "Reaching the server.");
            HideReconnectBar();
            _web.CoreWebView2.Navigate(url);
        });
        return Task.CompletedTask;
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            HideOverlay();
            _web.Visible = true;
            return;
        }

        // A failed navigation (host unreachable, DNS, TLS). Let the caller decide what to show — the
        // Client swaps in its friendly retry panel; the Server rarely hits this (it points at localhost).
        _web.Visible = false;
        NavigationFailed?.Invoke();
    }

    // ---- Overlay control -----------------------------------------------------------------------

    /// <summary>Shows the full-window "working" overlay with no button.</summary>
    public void ShowConnecting(string title, string message)
    {
        RunOnUi(() =>
        {
            _overlayTitle.Text = title;
            _overlayMessage.Text = message;
            _overlayButton.Visible = false;
            _overlayButtonAction = null;
            _overlay.Visible = true;
            _overlay.BringToFront();
        });
    }

    /// <summary>Shows the full-window error overlay with an action button.</summary>
    public void ShowError(string title, string message, string buttonText, Action onButton)
    {
        RunOnUi(() =>
        {
            _overlayTitle.Text = title;
            _overlayMessage.Text = message;
            _overlayButton.Text = buttonText;
            _overlayButton.Visible = true;
            _overlayButtonAction = onButton;
            _overlay.Visible = true;
            _overlay.BringToFront();
            CenterOverlayButton((Panel)_overlayButton.Parent!);
        });
    }

    public void HideOverlay() => RunOnUi(() => _overlay.Visible = false);

    // ---- Reconnect bar -------------------------------------------------------------------------

    /// <summary>Shows the thin, non-blocking top bar prompting the user to reconnect.</summary>
    public void ShowReconnectBar(string message, Action onReconnect)
    {
        RunOnUi(() =>
        {
            _reconnectLabel.Text = message;
            _reconnectAction = onReconnect;
            _reconnectBar.Visible = true;
            _reconnectBar.BringToFront();
        });
    }

    public void HideReconnectBar() => RunOnUi(() => _reconnectBar.Visible = false);

    // ---- Helpers -------------------------------------------------------------------------------

    private static void CenterOverlayButton(Panel host)
    {
        if (host.Controls.Count == 0)
        {
            return;
        }

        var button = host.Controls[0];
        button.Location = new Point(
            Math.Max(0, (host.Width - button.Width) / 2),
            Math.Max(0, (host.Height - button.Height) / 2 - 20));
    }

    private void RunOnUi(Action action)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(action);
        }
        else
        {
            action();
        }
    }

    private static void OpenExternally(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
            {
                UseShellExecute = true,
            });
        }
        catch
        {
            // Best effort — nothing useful to do if the shell can't open a browser.
        }
    }

    private static Icon? TryLoadIcon()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "app.ico");
            return File.Exists(path) ? new Icon(path) : null;
        }
        catch
        {
            return null;
        }
    }
}
