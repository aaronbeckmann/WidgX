namespace WidgX.Widgets.Bluetooth;

public static class BluetoothWidgetRegistration
{
    public static void Register()
    {
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "Bluetooth",
            DisplayName = "Bluetooth Battery",
            CreateWidget = () => new BluetoothWidget(),
            CreateDefaultInstance = () => new Models.WidgetInstance
            {
                WidgetType = "Bluetooth",
                Width = 180,
                Height = 70,
                Opacity = 0.9,
                AccentColorHex = "#FFFFFF",
                FontSize = 14
            }
        });
    }
}
