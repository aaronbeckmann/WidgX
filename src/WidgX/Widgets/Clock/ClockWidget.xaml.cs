using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WidgX.Models;

namespace WidgX.Widgets.Clock;

public partial class ClockWidget : System.Windows.Controls.UserControl, IWidget
{
    private readonly DispatcherTimer _timer;

    public ClockWidget()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Refresh();
    }

    System.Windows.Controls.UserControl IWidget.View => this;

    public static (string Time, string DayOfWeek, string Date) FormatDisplay(DateTime now)
    {
        var enCulture = CultureInfo.GetCultureInfo("en-US");
        return (
            now.ToString("HH:mm", enCulture),
            now.ToString("dddd", enCulture),
            now.ToString("yyyy-MM-dd", enCulture)
        );
    }

    public void Configure(WidgetInstance config)
    {
        Width = config.Width;
        Height = config.Height;
        Opacity = config.Opacity;
        FontSize = config.FontSize;

        if (System.Windows.Media.ColorConverter.ConvertFromString(config.AccentColorHex) is System.Windows.Media.Color color)
        {
            TimeText.Foreground = new SolidColorBrush(color);
            DayOfWeekText.Foreground = new SolidColorBrush(color);
            DateText.Foreground = new SolidColorBrush(color);
        }

        Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates() => _timer.Stop();

    private void Refresh()
    {
        var (time, dayOfWeek, date) = FormatDisplay(DateTime.Now);
        TimeText.Text = time;
        DayOfWeekText.Text = dayOfWeek;
        DateText.Text = date;
    }
}
