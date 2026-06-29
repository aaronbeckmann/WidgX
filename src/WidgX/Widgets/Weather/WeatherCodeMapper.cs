using System.Collections.Generic;

namespace WidgX.Widgets.Weather;

public static class WeatherCodeMapper
{
    private static readonly Dictionary<int, string> Descriptions = new()
    {
        [0] = "Clear sky",
        [1] = "Mainly clear",
        [2] = "Partly cloudy",
        [3] = "Overcast",
        [45] = "Fog",
        [48] = "Freezing fog",
        [51] = "Light drizzle",
        [53] = "Drizzle",
        [55] = "Dense drizzle",
        [61] = "Light rain",
        [63] = "Rain",
        [65] = "Heavy rain",
        [71] = "Light snow",
        [73] = "Snow",
        [75] = "Heavy snow",
        [80] = "Light showers",
        [81] = "Showers",
        [82] = "Violent showers",
        [95] = "Thunderstorm",
        [96] = "Thunderstorm with hail",
        [99] = "Severe thunderstorm with hail"
    };

    public static string Describe(int wmoCode)
    {
        return Descriptions.TryGetValue(wmoCode, out var description) ? description : "Unknown";
    }
}
