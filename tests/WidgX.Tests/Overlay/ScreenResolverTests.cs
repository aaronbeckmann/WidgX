using System.Collections.Generic;
using System.Windows;
using WidgX.Overlay;
using Xunit;

namespace WidgX.Tests.Overlay;

public class ScreenResolverTests
{
    private static List<ScreenInfo> TwoScreens() => new()
    {
        new ScreenInfo { Id = "DISPLAY1", FriendlyName = "Primary", BoundsDip = new Rect(0, 0, 1920, 1080), IsPrimary = true },
        new ScreenInfo { Id = "DISPLAY2", FriendlyName = "Secondary", BoundsDip = new Rect(1920, 0, 1920, 1080), IsPrimary = false }
    };

    [Fact]
    public void Resolves_Requested_Screen_When_Present()
    {
        var result = ScreenResolver.ResolveSelected("DISPLAY2", TwoScreens());
        Assert.Equal("DISPLAY2", result.Id);
    }

    [Fact]
    public void Falls_Back_To_Primary_When_Requested_Screen_Missing()
    {
        var result = ScreenResolver.ResolveSelected("DISPLAY9", TwoScreens());
        Assert.Equal("DISPLAY1", result.Id);
    }

    [Fact]
    public void Falls_Back_To_Primary_When_No_Screen_Requested()
    {
        var result = ScreenResolver.ResolveSelected(null, TwoScreens());
        Assert.Equal("DISPLAY1", result.Id);
    }
}
