namespace WidgX.Widgets.Uptime;

public static class UptimeWidgetRegistration
{
    public static void Register()
    {
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "Uptime",
            DisplayName = "Uptime",
            CreateWidget = () => new UptimeWidget(),
            CreateDefaultInstance = () => new Models.WidgetInstance
            {
                WidgetType = "Uptime",
                Width = 160,
                Height = 70,
                Opacity = 0.9,
                AccentColorHex = "#FFFFFF",
                FontSize = 14
            }
        });
    }
}
