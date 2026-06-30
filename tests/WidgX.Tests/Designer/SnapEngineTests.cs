using System.Collections.Generic;
using WidgX.Designer;
using Xunit;
using Rect = System.Windows.Rect;

namespace WidgX.Tests.Designer;

public class SnapEngineTests
{
    private static readonly List<Rect> None = new();

    [Fact]
    public void NoTargetsNearby_ReturnsUnchanged()
    {
        // Far from canvas edges/center; no other widgets.
        var r = SnapEngine.Snap(500, 500, 100, 100, None, 4000, 4000, 8);
        Assert.Equal(500, r.X);
        Assert.Equal(500, r.Y);
        Assert.Null(r.GuideX);
        Assert.Null(r.GuideY);
    }

    [Fact]
    public void LeftEdge_SnapsToOtherWidgetLeft()
    {
        var other = new List<Rect> { new(300, 50, 100, 100) };
        // Dragged left at 305 -> within 8 of 300.
        var r = SnapEngine.Snap(305, 600, 80, 40, other, 4000, 4000, 8);
        Assert.Equal(300, r.X);
        Assert.Equal(300, r.GuideX);
    }

    [Fact]
    public void Center_SnapsToCanvasCenter()
    {
        // Canvas 2000 wide, center 1000. Box width 100 -> center at x+50.
        // x=948 -> center 998, within 8 of 1000 -> snap so center hits 1000 -> x=950.
        var r = SnapEngine.Snap(948, 600, 100, 40, None, 2000, 4000, 8);
        Assert.Equal(950, r.X);
        Assert.Equal(1000, r.GuideX);
    }

    [Fact]
    public void RightEdge_SnapsToOtherWidgetRight()
    {
        var other = new List<Rect> { new(200, 50, 100, 100) }; // right edge = 300
        // Dragged width 60, x=243 -> right=303, within 8 of 300 -> x=240.
        var r = SnapEngine.Snap(243, 600, 60, 40, other, 4000, 4000, 8);
        Assert.Equal(240, r.X);
        Assert.Equal(300, r.GuideX);
    }

    [Fact]
    public void TopEdge_SnapsToOtherWidgetTop()
    {
        var other = new List<Rect> { new(50, 400, 100, 100) };
        var r = SnapEngine.Snap(600, 404, 40, 40, other, 4000, 4000, 8);
        Assert.Equal(400, r.Y);
        Assert.Equal(400, r.GuideY);
    }

    [Fact]
    public void JustOutsideThreshold_DoesNotSnap()
    {
        var other = new List<Rect> { new(300, 50, 100, 100) }; // left 300, center 350, right 400
        // Narrow box so none of its edges/center fall within 8 of any target.
        var r = SnapEngine.Snap(312, 600, 10, 40, other, 4000, 4000, 8);
        Assert.Equal(312, r.X);
        Assert.Null(r.GuideX);
    }
}
