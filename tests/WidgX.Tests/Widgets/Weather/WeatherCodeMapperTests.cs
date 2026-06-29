using WidgX.Widgets.Weather;
using Xunit;

namespace WidgX.Tests.Widgets.Weather;

public class WeatherCodeMapperTests
{
    [Theory]
    [InlineData(0, "Clear sky")]
    [InlineData(61, "Light rain")]
    [InlineData(95, "Thunderstorm")]
    [InlineData(9999, "Unknown")]
    public void Describe_Maps_Known_Codes(int code, string expected)
    {
        Assert.Equal(expected, WeatherCodeMapper.Describe(code));
    }
}
