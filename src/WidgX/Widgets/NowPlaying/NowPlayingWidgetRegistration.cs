namespace WidgX.Widgets.NowPlaying;

public static class NowPlayingWidgetRegistration
{
    public static void Register()
    {
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "NowPlaying",
            DisplayName = "Now Playing",
            CreateWidget = () => new NowPlayingWidget(),
            CreateDefaultInstance = () => new Models.WidgetInstance
            {
                WidgetType = "NowPlaying",
                Width = 240,
                Height = 70,
                Opacity = 0.9,
                AccentColorHex = "#FFFFFF",
                FontSize = 13
            }
        });
    }
}
