namespace WidgX.Models;

public class AppSettings
{
    public bool AutostartEnabled { get; set; }
    public string WeatherLocationName { get; set; } = string.Empty;
    public double? WeatherLatitude { get; set; }
    public double? WeatherLongitude { get; set; }
}
