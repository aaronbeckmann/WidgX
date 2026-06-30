using System;
using System.Windows.Controls;
using System.Windows.Threading;
using WidgX.Models;

namespace WidgX.Widgets.Uptime;

public partial class UptimeWidget : System.Windows.Controls.UserControl, IWidget
{
    private readonly DispatcherTimer _timer;

    public UptimeWidget()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += (_, _) => Refresh();
    }

    System.Windows.Controls.UserControl IWidget.View => this;

    public void Configure(WidgetInstance config)
    {
        Width = config.Width;
        Height = config.Height;
        WidgetChrome.ApplyBackgroundOpacity(this, config.Opacity);
        WidgetChrome.ApplyFont(this, config.FontFamily);
        WidgetChrome.ApplyTextShadow(this, config.TextShadow);
        FontSize = config.FontSize;

        if (WidgetChrome.ParseAccent(config.AccentColorHex) is { } accent)
        {
            UptimeText.Foreground = accent;
        }

        Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates() => _timer.Stop();

    private void Refresh()
    {
        UptimeText.Text = UptimeService.Format(UptimeService.GetUptime());
    }
}
