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

    [Fact]
    public void FormatDisplay_RespectsCustomDateFormat()
    {
        var now = new DateTime(2026, 6, 27, 14, 5, 0);
        var (_, _, date) = ClockWidget.FormatDisplay(now, "MMMM d, yyyy");
        Assert.Equal("June 27, 2026", date);
    }

    [Fact]
    public void FormatDisplay_InvalidFormat_FallsBackToIso()
    {
        var now = new DateTime(2026, 6, 27, 14, 5, 0);
        // A single unknown standard-format specifier throws; we fall back to ISO.
        var (_, _, date) = ClockWidget.FormatDisplay(now, "q");
        Assert.Equal("2026-06-27", date);
    }

    [Theory]
    [InlineData("Date,Time,Weekday", new[] { "Date", "Time", "Weekday" })]
    [InlineData("Weekday", new[] { "Weekday", "Time", "Date" })] // missing items appended
    [InlineData("", new[] { "Time", "Weekday", "Date" })] // default order
    [InlineData("Time,Time,Date", new[] { "Time", "Date", "Weekday" })] // de-duplicated
    public void ResolveItemOrder_CompletesAndDeduplicates(string order, string[] expected)
    {
        Assert.Equal(expected, ClockWidget.ResolveItemOrder(order));
    }
}
