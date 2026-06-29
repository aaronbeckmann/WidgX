using System;
using WidgX.Widgets.Uptime;
using Xunit;

namespace WidgX.Tests.Widgets.Uptime;

public class UptimeServiceTests
{
    [Theory]
    [InlineData(0, 0, 5, "0d 0h 5m")]
    [InlineData(3, 4, 12, "3d 4h 12m")]
    public void Format_Produces_Days_Hours_Minutes(int days, int hours, int minutes, string expected)
    {
        var span = new TimeSpan(days, hours, minutes, 0);
        Assert.Equal(expected, UptimeService.Format(span));
    }
}
