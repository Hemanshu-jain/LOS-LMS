using System.Drawing;
using System.Windows.Forms;

namespace LosLms.Server;

/// <summary>
/// The one-time first-run screen that asks whether this computer hosts the system or connects to it.
/// Shown only when no role has been chosen for the device. Returns the chosen role, or null if the
/// window was closed without choosing.
/// </summary>
internal sealed class RolePicker : Form
{
    private static readonly Color Bg = Color.FromArgb(15, 23, 42);
    private static readonly Color Card = Color.FromArgb(30, 41, 59);
    private static readonly Color CardHover = Color.FromArgb(51, 65, 85);
    private static readonly Color Accent = Color.FromArgb(96, 165, 250);

    private string? _choice;

    private RolePicker()
    {
        Text = "LOS/LMS — Setup";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 430);
        BackColor = Bg;
        Icon = TryLoadIcon();

        Controls.Add(Heading("Set up this computer", 28, 18F, FontStyle.Bold, Color.White));
        Controls.Add(Heading("A one-time choice — it's remembered for this PC.", 66, 10.5F, FontStyle.Regular,
            Color.FromArgb(148, 163, 184)));

        Controls.Add(BuildCard(
            top: 110,
            title: "Set up the SERVER on this computer",
            body: "Host the system here. The database and app run on this machine for your whole team. "
                + "Pick this on ONE computer only.",
            role: RoleStore.Host));

        Controls.Add(BuildCard(
            top: 250,
            title: "Connect to the server  (staff)",
            body: "Just use the app. It finds your office's server automatically — nothing to set up. "
                + "Pick this on every staff computer.",
            role: RoleStore.Client));
    }

    /// <summary>Shows the picker modally and returns the chosen role, or null if cancelled.</summary>
    public static string? Ask()
    {
        using var picker = new RolePicker();
        picker.ShowDialog();
        return picker._choice;
    }

    private static Label Heading(string text, int top, float size, FontStyle style, Color color) => new()
    {
        Text = text,
        AutoSize = false,
        Bounds = new Rectangle(40, top, 480, size > 14 ? 34 : 24),
        Font = new Font("Segoe UI", size, style),
        ForeColor = color,
        TextAlign = ContentAlignment.MiddleLeft,
    };

    private Panel BuildCard(int top, string title, string body, string role)
    {
        var card = new Panel
        {
            Bounds = new Rectangle(40, top, 480, 120),
            BackColor = Card,
            Cursor = Cursors.Hand,
        };

        var titleLabel = new Label
        {
            Text = title,
            AutoSize = false,
            Bounds = new Rectangle(20, 16, 440, 26),
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Accent,
            BackColor = Color.Transparent,
        };
        var bodyLabel = new Label
        {
            Text = body,
            AutoSize = false,
            Bounds = new Rectangle(20, 46, 440, 60),
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(203, 213, 225),
            BackColor = Color.Transparent,
        };

        void Choose(object? s, EventArgs e) { _choice = role; Close(); }
        void Enter(object? s, EventArgs e) => card.BackColor = CardHover;
        void Leave(object? s, EventArgs e) => card.BackColor = Card;

        foreach (Control c in new Control[] { card, titleLabel, bodyLabel })
        {
            c.Click += Choose;
            c.MouseEnter += Enter;
            c.MouseLeave += Leave;
        }

        card.Controls.Add(titleLabel);
        card.Controls.Add(bodyLabel);
        return card;
    }

    private static Icon? TryLoadIcon()
    {
        try
        {
            return Environment.ProcessPath is { } exe ? Icon.ExtractAssociatedIcon(exe) : null;
        }
        catch
        {
            return null;
        }
    }
}
