using System;
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
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location),
            Text = "WidgX",
            ContextMenuStrip = menu,
            Visible = false
        };
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
