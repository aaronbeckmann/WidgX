namespace WidgX.Widgets.SystemStats;

public static class SystemStatsWidgetRegistration
{
    public static void Register()
    {
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "SystemStats",
            DisplayName = "System Stats (CPU/RAM/GPU/VRAM)",
            CreateWidget = () => new SystemStatsWidget(),
            CreateDefaultInstance = () => new Models.WidgetInstance
            {
                WidgetType = "SystemStats",
                Width = 310,
                Height = 100,
                Opacity = 0.9,
                AccentColorHex = "#FFFFFF",
                FontSize = 14
            }
        });
    }
}
