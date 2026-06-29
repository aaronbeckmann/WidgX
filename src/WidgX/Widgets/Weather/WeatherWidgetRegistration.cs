namespace WidgX.Widgets.Weather;

public static class WeatherWidgetRegistration
{
    public static void Register()
    {
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "Weather",
            DisplayName = "Weather + Forecast",
            CreateWidget = () => new WeatherWidget(),
            CreateDefaultInstance = () => new Models.WidgetInstance
            {
                WidgetType = "Weather",
                Width = 240,
                Height = 220,
                Opacity = 0.9,
                AccentColorHex = "#FFFFFF",
                FontSize = 13
            }
        });
    }
}
