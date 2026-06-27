using System;
using WidgX.Widgets.Clock;
using Xunit;

namespace WidgX.Tests.Widgets.Clock;

public class ClockWidgetFormattingTests
{
    [Fact]
    public void FormatDisplay_Produces_Time_Day_And_Date()
    {
        var now = new DateTime(2026, 6, 27, 14, 5, 0); // Saturday

        var (time, dayOfWeek, date) = ClockWidget.FormatDisplay(now);

        Assert.Equal("14:05", time);
        Assert.Equal("Saturday", dayOfWeek);
        Assert.Equal("2026-06-27", date);
    }
}
