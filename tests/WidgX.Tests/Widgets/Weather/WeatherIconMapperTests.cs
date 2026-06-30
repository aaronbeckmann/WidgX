using WidgX.Widgets.Weather;
using Xunit;

namespace WidgX.Tests.Widgets.Weather;

public class WeatherIconMapperTests
{
    [Theory]
    [InlineData(0, "☀️")]
    [InlineData(3, "☁️")]
    [InlineData(45, "🌫️")]
    [InlineData(63, "🌧️")]
    [InlineData(73, "🌨️")]
    [InlineData(95, "⛈️")]
    public void Icon_MapsKnownCodes(int code, string expected)
    {
        Assert.Equal(expected, WeatherIconMapper.Icon(code));
    }

    [Fact]
    public void Icon_UnknownCode_ReturnsFallback()
    {
        Assert.Equal("🌡️", WeatherIconMapper.Icon(12345));
    }
}
