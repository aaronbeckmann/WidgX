using System;
using WidgX.Widgets.NowPlaying;
using Xunit;

namespace WidgX.Tests.Widgets.NowPlaying;

public class MediaProgressTests
{
    [Fact]
    public void Fraction_Half_Returns_0_5()
    {
        Assert.Equal(0.5, MediaProgress.Fraction(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(120)));
    }

    [Fact]
    public void Fraction_ZeroDuration_Returns_0()
    {
        Assert.Equal(0, MediaProgress.Fraction(TimeSpan.FromSeconds(30), TimeSpan.Zero));
    }

    [Fact]
    public void Fraction_PositionPastEnd_ClampsTo_1()
    {
        Assert.Equal(1, MediaProgress.Fraction(TimeSpan.FromSeconds(200), TimeSpan.FromSeconds(120)));
    }

    [Fact]
    public void Fraction_NegativePosition_ClampsTo_0()
    {
        Assert.Equal(0, MediaProgress.Fraction(TimeSpan.FromSeconds(-5), TimeSpan.FromSeconds(120)));
    }
}
