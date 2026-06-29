using WidgX.Overlay;
using Xunit;
using Rect = System.Windows.Rect;

namespace WidgX.Tests.Overlay;

public class MonitorNameResolverTests
{
    [Fact]
    public void FormatLabel_Primary_IncludesNameResolutionAndPrimaryTag()
    {
        var label = MonitorNameResolver.FormatLabel("Dell U2720Q", new Rect(0, 0, 2560, 1440), true);
        Assert.Equal("Dell U2720Q — 2560×1440 (Primary)", label);
    }

    [Fact]
    public void FormatLabel_NonPrimary_OmitsPrimaryTag()
    {
        var label = MonitorNameResolver.FormatLabel("DISPLAY2", new Rect(1920, 0, 1920, 1080), false);
        Assert.Equal("DISPLAY2 — 1920×1080", label);
    }

    [Fact]
    public void FormatLabel_TruncatesFractionalBounds()
    {
        var label = MonitorNameResolver.FormatLabel("Mon", new Rect(0, 0, 1707.5, 960.5), false);
        Assert.Equal("Mon — 1707×960", label);
    }

    [Fact]
    public void GetFriendlyNames_NeverThrows_AndReturnsNonNull()
    {
        // The DisplayConfig P/Invoke must degrade gracefully, never crash.
        var map = MonitorNameResolver.GetFriendlyNames();
        Assert.NotNull(map);
    }
}
