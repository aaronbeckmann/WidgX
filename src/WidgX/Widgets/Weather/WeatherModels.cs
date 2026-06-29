using System;
using System.Collections.Generic;

namespace WidgX.Widgets.Weather;

public class WeatherForecast
{
    public double CurrentTempC { get; set; }
    public int CurrentWeatherCode { get; set; }
    public List<DailyForecast> Daily { get; set; } = new();
}

public class DailyForecast
{
    public DateTime Date { get; set; }
    public double MaxTempC { get; set; }
    public double MinTempC { get; set; }
    public int WeatherCode { get; set; }
}
