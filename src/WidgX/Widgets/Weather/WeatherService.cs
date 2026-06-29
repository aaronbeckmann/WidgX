using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace WidgX.Widgets.Weather;

public class WeatherService
{
    private readonly HttpClient _httpClient;

    public WeatherService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<(double Latitude, double Longitude)?> GeocodeAsync(string locationName)
    {
        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(locationName)}&count=1";
        var json = await _httpClient.GetStringAsync(url);

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
        {
            return null;
        }

        var first = results[0];
        return (first.GetProperty("latitude").GetDouble(), first.GetProperty("longitude").GetDouble());
    }

    public async Task<WeatherForecast> GetForecastAsync(double lat, double lon)
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}" +
                  "&current_weather=true&daily=temperature_2m_max,temperature_2m_min,weathercode&timezone=auto";
        var json = await _httpClient.GetStringAsync(url);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var forecast = new WeatherForecast
        {
            CurrentTempC = root.GetProperty("current_weather").GetProperty("temperature").GetDouble(),
            CurrentWeatherCode = root.GetProperty("current_weather").GetProperty("weathercode").GetInt32()
        };

        var daily = root.GetProperty("daily");
        var dates = daily.GetProperty("time");
        var maxTemps = daily.GetProperty("temperature_2m_max");
        var minTemps = daily.GetProperty("temperature_2m_min");
        var codes = daily.GetProperty("weathercode");

        for (var i = 0; i < dates.GetArrayLength(); i++)
        {
            forecast.Daily.Add(new DailyForecast
            {
                Date = DateTime.Parse(dates[i].GetString()!),
                MaxTempC = maxTemps[i].GetDouble(),
                MinTempC = minTemps[i].GetDouble(),
                WeatherCode = codes[i].GetInt32()
            });
        }

        return forecast;
    }
}
