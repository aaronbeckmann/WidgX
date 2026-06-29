namespace WidgX.Widgets.Todos;

public static class TodoWidgetRegistration
{
    public static void Register()
    {
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "Todos",
            DisplayName = "Todos",
            CreateWidget = () => new TodoWidget(),
            CreateDefaultInstance = () => new Models.WidgetInstance
            {
                WidgetType = "Todos",
                Width = 240,
                Height = 260,
                Opacity = 0.9,
                AccentColorHex = "#FFFFFF",
                FontSize = 13
            }
        });
    }
}
