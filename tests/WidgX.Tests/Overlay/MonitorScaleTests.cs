using WidgX.Overlay;
using Xunit;
using Rect = System.Windows.Rect;

namespace WidgX.Tests.Overlay;

public class MonitorScaleTests
{
    [Fact]
    public void GetScaleFactor_NeverThrows_AndIsPositive()
    {
        var scale = MonitorScale.GetScaleFactor(new Rect(0, 0, 1920, 1080));
        Assert.True(scale > 0);
    }
}
