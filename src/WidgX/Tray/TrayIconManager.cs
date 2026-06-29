using System;
using System.IO;
using System.Windows.Forms;

namespace WidgX.Tray;

public class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public TrayIconManager(Action onEditLayout, Action onToggleVisibility, Action onExit)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Edit Layout", null, (_, _) => onEditLayout());
        menu.Items.Add("Toggle Overlay Visibility", null, (_, _) => onToggleVisibility());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => onExit());

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadEmbeddedIcon(),
            Text = "WidgX",
            ContextMenuStrip = menu,
            Visible = false
        };
    }

    private static System.Drawing.Icon LoadEmbeddedIcon()
    {
        var uri = new Uri("pack://application:,,,/Resources/tray-icon.ico");
        var resourceStream = System.Windows.Application.GetResourceStream(uri);
        if (resourceStream != null)
        {
            return new System.Drawing.Icon(resourceStream.Stream);
        }

        return System.Drawing.Icon.ExtractAssociatedIcon(
            System.Reflection.Assembly.GetExecutingAssembly().Location)!;
    }

    public void SetTooltip(string text)
    {
        // NotifyIcon.Text has a 63-character limit.
        _notifyIcon.Text = text.Length > 63 ? text[..63] : text;
    }

    public void Show()
    {
        _notifyIcon.Visible = true;
    }

    public void Dispose()
    {
        _notifyIcon.Dispose();
    }
}
