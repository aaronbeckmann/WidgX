namespace WidgX.Widgets.Clock;

public static class ClockWidgetRegistration
{
    public static void Register()
    {
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "Clock",
            DisplayName = "Clock",
            CreateWidget = () => new ClockWidget(),
            CreateDefaultInstance = () => new Models.WidgetInstance
            {
                WidgetType = "Clock",
                Width = 220,
                Height = 110,
                Opacity = 0.9,
                AccentColorHex = "#FFFFFF",
                FontSize = 14,
                Settings = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["order"] = "Time,Weekday,Date",
                    ["timeFontSize"] = "28",
                    ["weekdayFontSize"] = "14",
                    ["dateFontSize"] = "14",
                    ["dateFormat"] = "yyyy-MM-dd"
                }
            }
        });
    }
}
