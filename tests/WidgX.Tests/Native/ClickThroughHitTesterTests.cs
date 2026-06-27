using System.Collections.Generic;
using System.Windows;
using WidgX.Native;
using Xunit;

namespace WidgX.Tests.Native;

public class ClickThroughHitTesterTests
{
    [Fact]
    public void Point_Outside_All_Widgets_Is_Not_A_Hit()
    {
        var widgets = new List<Rect> { new Rect(0, 0, 100, 50) };
        var result = ClickThroughHitTester.HitTest(new Point(200, 200), widgets);
        Assert.False(result);
    }

    [Fact]
    public void Point_Inside_A_Widget_Is_A_Hit()
    {
        var widgets = new List<Rect> { new Rect(0, 0, 100, 50) };
        var result = ClickThroughHitTester.HitTest(new Point(10, 10), widgets);
        Assert.True(result);
    }

    [Fact]
    public void No_Widgets_Means_Never_A_Hit()
    {
        var result = ClickThroughHitTester.HitTest(new Point(10, 10), new List<Rect>());
        Assert.False(result);
    }
}
