using System;
using System.Globalization;
using System.Linq;
using System.Windows.Threading;
using WidgX.Models;
using WidgX.Persistence;

namespace WidgX.Widgets.Weather;

public record ForecastRow(string Day, string Icon, string Range);

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
        WidgetChrome.ApplyFont(this, config.FontFamily);
        WidgetChrome.ApplyTextShadow(this, config.TextShadow);
        FontSize = config.FontSize;

        if (WidgetChrome.ParseAccent(config.AccentColorHex) is { } accent)
        {
            CurrentTemp.Foreground = accent;
        }

        _ = Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates() => _timer.Stop();

    private async System.Threading.Tasks.Task Refresh()
    {
        var settings = SettingsStore.Load(AppPaths.SettingsFilePath);

        if (settings.WeatherLatitude is null || settings.WeatherLongitude is null)
        {
            ShowMessage("📍", "Set a location in Settings");
            return;
        }

        try
        {
            var forecast = await _service.GetForecastAsync(settings.WeatherLatitude.Value, settings.WeatherLongitude.Value);
            var enCulture = CultureInfo.GetCultureInfo("en-US");

            CurrentIcon.Text = WeatherIconMapper.Icon(forecast.CurrentWeatherCode);
            CurrentTemp.Text = $"{forecast.CurrentTempC:0}°C";
            CurrentDesc.Text = WeatherCodeMapper.Describe(forecast.CurrentWeatherCode);

            ForecastList.ItemsSource = forecast.Daily.Take(5).Select(d => new ForecastRow(
                d.Date.ToString("ddd", enCulture),
                WeatherIconMapper.Icon(d.WeatherCode),
                $"{d.MinTempC:0}–{d.MaxTempC:0}°")).ToList();
        }
        catch (Exception)
        {
            ShowMessage("⚠️", "Weather unavailable");
        }
    }

    private void ShowMessage(string icon, string message)
    {
        CurrentIcon.Text = icon;
        CurrentTemp.Text = string.Empty;
        CurrentDesc.Text = message;
        ForecastList.ItemsSource = null;
    }
}
