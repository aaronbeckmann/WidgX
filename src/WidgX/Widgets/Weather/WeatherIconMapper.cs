namespace WidgX.Widgets.Weather;

/// <summary>
/// Maps WMO weather codes to a colourful emoji glyph (rendered with Segoe UI Emoji).
/// </summary>
public static class WeatherIconMapper
{
    public static string Icon(int wmoCode) => wmoCode switch
    {
        0 => "☀️",            // clear sky
        1 => "🌤️",            // mainly clear
        2 => "⛅",            // partly cloudy
        3 => "☁️",            // overcast
        45 or 48 => "🌫️",     // fog
        51 or 53 or 55 => "🌦️", // drizzle
        56 or 57 => "🌧️",     // freezing drizzle
        61 or 63 or 65 => "🌧️", // rain
        66 or 67 => "🌧️",     // freezing rain
        71 or 73 or 75 or 77 => "🌨️", // snow
        80 or 81 => "🌦️",     // rain showers
        82 => "🌧️",           // violent showers
        85 or 86 => "🌨️",     // snow showers
        95 => "⛈️",           // thunderstorm
        96 or 99 => "⛈️",     // thunderstorm with hail
        _ => "🌡️"
    };
}
