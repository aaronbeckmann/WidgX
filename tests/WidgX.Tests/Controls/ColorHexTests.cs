using WidgX.Controls;
using Xunit;

namespace WidgX.Tests.Controls;

public class ColorHexTests
{
    [Theory]
    [InlineData("#4FC3F7", 0x4F, 0xC3, 0xF7)]
    [InlineData("4FC3F7", 0x4F, 0xC3, 0xF7)]
    [InlineData("#ffffff", 0xFF, 0xFF, 0xFF)]
    [InlineData("#FF4FC3F7", 0x4F, 0xC3, 0xF7)]
    public void TryParseRgb_ParsesValidForms(string hex, int r, int g, int b)
    {
        Assert.True(ColorHex.TryParseRgb(hex, out var pr, out var pg, out var pb));
        Assert.Equal((byte)r, pr);
        Assert.Equal((byte)g, pg);
        Assert.Equal((byte)b, pb);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("#12345")]
    [InlineData("nothex")]
    [InlineData("#GGGGGG")]
    public void TryParseRgb_RejectsInvalid(string? hex)
    {
        Assert.False(ColorHex.TryParseRgb(hex, out _, out _, out _));
    }

    [Fact]
    public void Format_ProducesUppercaseHashHex()
    {
        Assert.Equal("#4FC3F7", ColorHex.Format(0x4F, 0xC3, 0xF7));
        Assert.Equal("#FFFFFF", ColorHex.Format(255, 255, 255));
        Assert.Equal("#000000", ColorHex.Format(0, 0, 0));
    }

    [Fact]
    public void ParseThenFormat_RoundTrips()
    {
        Assert.True(ColorHex.TryParseRgb("#A1B2C3", out var r, out var g, out var b));
        Assert.Equal("#A1B2C3", ColorHex.Format(r, g, b));
    }
}
