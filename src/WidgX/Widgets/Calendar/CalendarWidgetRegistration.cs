namespace WidgX.Widgets.Calendar;

public static class CalendarWidgetRegistration
{
    public static void Register()
    {
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "Calendar",
            DisplayName = "Calendar",
            CreateWidget = () => new CalendarWidget(),
            CreateDefaultInstance = () => new Models.WidgetInstance
            {
                WidgetType = "Calendar",
                Width = 260,
                Height = 220,
                Opacity = 0.9,
                AccentColorHex = "#FFFFFF",
                FontSize = 14
            }
        });
    }
}
