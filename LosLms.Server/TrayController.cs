using System.Drawing;
using System.Windows.Forms;

namespace LosLms.Server;

/// <summary>
/// A small system-tray presence that makes the current shareable tunnel URL easy to find and copy for
/// whoever runs this machine — and also writes it to a plainly-named text file next to the exe. All UI
/// thread only.
/// </summary>
internal sealed class TrayController : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _urlItem;
    private readonly ToolStripMenuItem _copyItem;
    private string? _url;

    public TrayController(Action onOpenWindow, Action onQuit)
    {
        var menu = new ContextMenuStrip();

        _urlItem = new ToolStripMenuItem("Tunnel: starting…") { Enabled = false };
        _copyItem = new ToolStripMenuItem("Copy shareable link", null, (_, _) => CopyUrl()) { Enabled = false };

        menu.Items.Add(_urlItem);
        menu.Items.Add(_copyItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Open app window", null, (_, _) => onOpenWindow()));
        menu.Items.Add(new ToolStripMenuItem("Quit", null, (_, _) => onQuit()));

        _icon = new NotifyIcon
        {
            Icon = TryLoadIcon(),
            Text = "LOS/LMS Server",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _icon.DoubleClick += (_, _) => onOpenWindow();
    }

    /// <summary>Updates the shown URL (null ⇒ the tunnel is unavailable and it is local-only).</summary>
    public void SetUrl(string? url)
    {
        _url = url;
        if (url is null)
        {
            _urlItem.Text = "Tunnel unavailable — local only";
            _copyItem.Enabled = false;
        }
        else
        {
            _urlItem.Text = $"Shareable link: {url}";
            _copyItem.Enabled = true;
            TryWriteFile(url);
            _icon.ShowBalloonTip(4000, "LOS/LMS Server", "Shareable link is ready. Right-click the tray icon to copy it.", ToolTipIcon.Info);
        }
    }

    private void CopyUrl()
    {
        if (_url is not null)
        {
            try { Clipboard.SetText(_url); } catch { /* clipboard can be transiently locked */ }
        }
    }

    private static void TryWriteFile(string url)
    {
        try
        {
            File.WriteAllText(Paths.ShareableUrlFile,
                $"LOS/LMS shareable link (changes each restart):{Environment.NewLine}{url}{Environment.NewLine}");
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not write {Paths.ShareableUrlFile}: {ex.Message}");
        }
    }

    private static Icon TryLoadIcon()
    {
        try
        {
            var path = Path.Combine(Paths.InstallRoot, "app.ico");
            return File.Exists(path) ? new Icon(path) : SystemIcons.Application;
        }
        catch
        {
            return SystemIcons.Application;
        }
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
