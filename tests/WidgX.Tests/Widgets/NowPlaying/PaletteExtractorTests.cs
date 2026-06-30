using System.Collections.Generic;
using WidgX.Widgets.NowPlaying;
using Xunit;
using Color = System.Windows.Media.Color;

namespace WidgX.Tests.Widgets.NowPlaying;

public class PaletteExtractorTests
{
    // BGRA pixel helper.
    private static void Add(List<byte> px, byte r, byte g, byte b)
    {
        px.Add(b); px.Add(g); px.Add(r); px.Add(255);
    }

    [Fact]
    public void Extract_RedAndBlue_ReturnsBoth()
    {
        var px = new List<byte>();
        Add(px, 255, 0, 0); // red
        Add(px, 0, 0, 255); // blue

        var colors = PaletteExtractor.Extract(px.ToArray(), 2, 1, 4);

        Assert.Equal(2, colors.Count);
        Assert.Contains(colors, c => c.R > c.G && c.R > c.B); // a reddish color
        Assert.Contains(colors, c => c.B > c.R && c.B > c.G); // a bluish color
    }

    [Fact]
    public void Extract_SkipsTransparentPixels()
    {
        var px = new List<byte>
        {
            0, 0, 255, 0,   // transparent red (alpha 0) -> skipped
            0, 255, 0, 255  // opaque green
        };

        var colors = PaletteExtractor.Extract(px.ToArray(), 2, 1, 4);

        Assert.Single(colors);
        Assert.True(colors[0].G > colors[0].R && colors[0].G > colors[0].B);
    }

    [Fact]
    public void Extract_HonorsMaxColors()
    {
        var px = new List<byte>();
        Add(px, 255, 0, 0);
        Add(px, 0, 255, 0);
        Add(px, 0, 0, 255);
        Add(px, 255, 255, 0);

        var colors = PaletteExtractor.Extract(px.ToArray(), 4, 1, 2);

        Assert.Equal(2, colors.Count);
    }

    [Fact]
    public void Extract_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(PaletteExtractor.Extract(System.Array.Empty<byte>(), 0, 0, 4));
    }
}
