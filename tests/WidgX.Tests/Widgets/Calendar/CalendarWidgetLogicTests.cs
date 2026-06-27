using System;
using System.Linq;
using WidgX.Widgets.Calendar;
using Xunit;

namespace WidgX.Tests.Widgets.Calendar;

public class CalendarWidgetLogicTests
{
    [Fact]
    public void BuildMonthGrid_Places_First_Day_On_Correct_Weekday()
    {
        // June 2026: June 1 is a Monday.
        var grid = CalendarWidget.BuildMonthGrid(new DateTime(2026, 6, 15));

        // Row 0 should contain June 1 at the Monday column (index 1, since
        // the grid's columns are Sun..Sat, index 0..6).
        Assert.Equal(1, grid[0][1]);
        Assert.Null(grid[0][0]); // Sunday before June 1 is blank
    }

    [Fact]
    public void BuildMonthGrid_Contains_Last_Day_Of_Month()
    {
        var grid = CalendarWidget.BuildMonthGrid(new DateTime(2026, 6, 15));

        var flatDays = grid.SelectMany(row => row).Where(d => d.HasValue).Select(d => d!.Value);
        Assert.Contains(30, flatDays); // June has 30 days
        Assert.DoesNotContain(31, flatDays);
    }
}
