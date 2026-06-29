using System;

namespace WidgX.Widgets.Uptime;

public static class UptimeService
{
    public static TimeSpan GetUptime() => TimeSpan.FromMilliseconds(Environment.TickCount64);

    public static string Format(TimeSpan uptime)
    {
        return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
    }
}
