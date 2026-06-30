using System;
using System.Linq;
using System.Windows.Threading;
using WidgX.Models;
using WidgX.Persistence;

namespace WidgX.Widgets.Weather;

public partial class WeatherWidget : System.Windows.Controls.UserControl, IWidget
{
    private readonly DispatcherTimer _timer;
    private readonly WeatherService _service = new();

    public WeatherWidget()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(20) };
        _timer.Tick += async (_, _) => await Refresh();
    }

    System.Windows.Controls.UserControl IWidget.View => this;

    public void Configure(WidgetInstance config)
    {
        Width = config.Width;
        Height = config.Height;
        WidgetChrome.ApplyBackgroundOpacity(this, config.Opacity);
        FontSize = config.FontSize;
        _ = Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates() => _timer.Stop();

    private async System.Threading.Tasks.Task Refresh()
    {
        var settings = SettingsStore.Load(AppPaths.SettingsFilePath);

        if (settings.WeatherLatitude is null || settings.WeatherLongitude is null)
        {
            CurrentText.Text = "Set a location in Settings";
            return;
        }

        try
        {
            var forecast = await _service.GetForecastAsync(settings.WeatherLatitude.Value, settings.WeatherLongitude.Value);

            CurrentText.Text = $"{forecast.CurrentTempC:0}°C, {WeatherCodeMapper.Describe(forecast.CurrentWeatherCode)}";

            ForecastList.ItemsSource = forecast.Daily.Take(5).Select(d =>
                $"{d.Date:ddd}: {d.MinTempC:0}–{d.MaxTempC:0}°C, {WeatherCodeMapper.Describe(d.WeatherCode)}");
        }
        catch (Exception)
        {
            CurrentText.Text = "Weather unavailable";
        }
    }
}
