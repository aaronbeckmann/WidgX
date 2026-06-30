using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WidgX.Models;

namespace WidgX.Widgets.Clock;

public partial class ClockWidget : System.Windows.Controls.UserControl, IWidget
{
    private readonly DispatcherTimer _timer;
    private string _dateFormat = "yyyy-MM-dd";

    public ClockWidget()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Refresh();
    }

    System.Windows.Controls.UserControl IWidget.View => this;

    /// <summary>The three clock items, resolved to a complete, de-duplicated order.</summary>
    public static List<string> ResolveItemOrder(string? order)
    {
        var all = new[] { "Time", "Weekday", "Date" };
        var result = (order ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => all.Contains(s, StringComparer.OrdinalIgnoreCase))
            .Select(s => all.First(a => a.Equals(s, StringComparison.OrdinalIgnoreCase)))
            .Distinct()
            .ToList();

        foreach (var item in all)
        {
            if (!result.Contains(item)) result.Add(item);
        }
        return result;
    }

    public static (string Time, string DayOfWeek, string Date) FormatDisplay(DateTime now)
        => FormatDisplay(now, "yyyy-MM-dd");

    public static (string Time, string DayOfWeek, string Date) FormatDisplay(DateTime now, string dateFormat)
    {
        var enCulture = CultureInfo.GetCultureInfo("en-US");
        string date;
        try { date = now.ToString(dateFormat, enCulture); }
        catch (FormatException) { date = now.ToString("yyyy-MM-dd", enCulture); }

        return (now.ToString("HH:mm", enCulture), now.ToString("dddd", enCulture), date);
    }

    public void Configure(WidgetInstance config)
    {
        Width = config.Width;
        Height = config.Height;
        WidgetChrome.ApplyBackgroundOpacity(this, config.Opacity);
        WidgetChrome.ApplyFont(this, config.FontFamily);
        WidgetChrome.ApplyTextShadow(this, config.TextShadow);
        FontSize = config.FontSize;

        _dateFormat = config.Settings.TryGetValue("dateFormat", out var fmt) && !string.IsNullOrWhiteSpace(fmt)
            ? fmt
            : "yyyy-MM-dd";

        TimeText.FontSize = ReadSize(config, "timeFontSize", 28);
        DayOfWeekText.FontSize = ReadSize(config, "weekdayFontSize", 14);
        DateText.FontSize = ReadSize(config, "dateFontSize", 14);

        if (System.Windows.Media.ColorConverter.ConvertFromString(config.AccentColorHex) is System.Windows.Media.Color color)
        {
            TimeText.Foreground = new SolidColorBrush(color);
            DayOfWeekText.Foreground = new SolidColorBrush(color);
            DateText.Foreground = new SolidColorBrush(color);
        }

        var enabled = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["Time"] = ReadBool(config, "showTime", true),
            ["Weekday"] = ReadBool(config, "showWeekday", true),
            ["Date"] = ReadBool(config, "showDate", true)
        };
        ApplyOrder(config.Settings.TryGetValue("order", out var order) ? order : null, enabled);
        Refresh();
    }

    private static bool ReadBool(WidgetInstance config, string key, bool fallback)
        => config.Settings.TryGetValue(key, out var raw) && bool.TryParse(raw, out var value) ? value : fallback;

    private static double ReadSize(WidgetInstance config, string key, double fallback)
        => config.Settings.TryGetValue(key, out var raw)
           && double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
           && value > 0
            ? value
            : fallback;

    private void ApplyOrder(string? order, IReadOnlyDictionary<string, bool> enabled)
    {
        var map = new Dictionary<string, TextBlock>(StringComparer.OrdinalIgnoreCase)
        {
            ["Time"] = TimeText,
            ["Weekday"] = DayOfWeekText,
            ["Date"] = DateText
        };

        RootStack.Children.Clear();
        foreach (var key in ResolveItemOrder(order))
        {
            if (enabled.TryGetValue(key, out var on) && !on) continue;
            RootStack.Children.Add(map[key]);
        }
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates() => _timer.Stop();

    private void Refresh()
    {
        var (time, dayOfWeek, date) = FormatDisplay(DateTime.Now, _dateFormat);
        TimeText.Text = time;
        DayOfWeekText.Text = dayOfWeek;
        DateText.Text = date;
    }
}
