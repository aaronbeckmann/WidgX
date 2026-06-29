using WidgX.Controls;
using Xunit;
using Point = System.Windows.Point;

namespace WidgX.Tests.Controls;

public class GaugeGeometryTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(25, 90)]
    [InlineData(50, 180)]
    [InlineData(100, 360)]
    public void SweepAngle_MapsPercentToDegrees(double percent, double expected)
    {
        Assert.Equal(expected, GaugeGeometry.SweepAngle(percent), 3);
    }

    [Theory]
    [InlineData(-10, 0)]
    [InlineData(150, 360)]
    public void SweepAngle_ClampsOutOfRange(double percent, double expected)
    {
        Assert.Equal(expected, GaugeGeometry.SweepAngle(percent), 3);
    }

    [Fact]
    public void PointOnArc_Top_IsDirectlyAboveCenter()
    {
        var p = GaugeGeometry.PointOnArc(new Point(0, 0), 10, 0);
        Assert.Equal(0, p.X, 3);
        Assert.Equal(-10, p.Y, 3);
    }

    [Fact]
    public void PointOnArc_Right_IsToTheRightOfCenter()
    {
        var p = GaugeGeometry.PointOnArc(new Point(0, 0), 10, 90);
        Assert.Equal(10, p.X, 3);
        Assert.Equal(0, p.Y, 3);
    }

    [Fact]
    public void PointOnArc_Bottom_IsBelowCenter()
    {
        var p = GaugeGeometry.PointOnArc(new Point(0, 0), 10, 180);
        Assert.Equal(0, p.X, 3);
        Assert.Equal(10, p.Y, 3);
    }

    [Theory]
    [InlineData(90, false)]
    [InlineData(180, false)]
    [InlineData(181, true)]
    [InlineData(270, true)]
    public void IsLargeArc_TrueAboveHalfCircle(double sweep, bool expected)
    {
        Assert.Equal(expected, GaugeGeometry.IsLargeArc(sweep));
    }
}
