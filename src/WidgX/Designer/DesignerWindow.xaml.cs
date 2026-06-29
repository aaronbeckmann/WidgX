using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WidgX.Controls;
using WidgX.Models;
using WidgX.Overlay;
using WidgX.Widgets;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Cursors = System.Windows.Input.Cursors;

namespace WidgX.Designer;

public partial class DesignerWindow : Window
{
    private readonly Layout _workingLayout;
    private readonly Action<Layout> _onSaved;
    private ScreenInfo _selectedScreen;
    private WidgetInstance? _selectedInstance;
    private bool _suppressColorSync;

    private static readonly string[] SwatchColors =
    {
        "#FFFFFF", "#4FC3F7", "#81C784", "#BA68C8", "#FFB74D", "#E57373",
        "#64B5F6", "#FFD54F", "#4DB6AC", "#F06292", "#A1887F", "#90A4AE"
    };

    public DesignerWindow(Layout initialLayout, Action<Layout> onSaved)
    {
        InitializeComponent();

        _workingLayout = new Layout
        {
            SelectedScreenId = initialLayout.SelectedScreenId,
            Widgets = initialLayout.Widgets
                .Select(w => new WidgetInstance
                {
                    Id = w.Id, WidgetType = w.WidgetType, X = w.X, Y = w.Y,
                    Width = w.Width, Height = w.Height, Opacity = w.Opacity,
                    AccentColorHex = w.AccentColorHex, FontSize = w.FontSize,
                    Settings = new System.Collections.Generic.Dictionary<string, string>(w.Settings)
                })
                .ToList()
        };
        _onSaved = onSaved;

        var screens = ScreenResolver.GetAllScreens();
        ScreenPicker.ItemsSource = screens;
        _selectedScreen = ScreenResolver.ResolveSelected(_workingLayout.SelectedScreenId, screens);
        ScreenPicker.SelectedItem = screens.FirstOrDefault(s => s.Id == _selectedScreen.Id);

        WidgetPalette.ItemsSource = WidgetRegistry.All;

        var settings = Persistence.SettingsStore.Load(Persistence.AppPaths.SettingsFilePath);
        WeatherLocationBox.Text = settings.WeatherLocationName;
        AutostartCheckBox.IsChecked = Startup.AutostartManager.IsEnabled();

        BuildSwatches();
        RebuildCanvas();
    }

    private void BuildSwatches()
    {
        foreach (var hex in SwatchColors)
        {
            if (!ColorHex.TryParseRgb(hex, out var r, out var g, out var b)) continue;

            var swatch = new Border
            {
                Width = 22,
                Height = 22,
                Margin = new Thickness(0, 0, 6, 6),
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Color.FromRgb(r, g, b)),
                BorderBrush = (System.Windows.Media.Brush)FindResource("ControlBorderBrush"),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                ToolTip = hex
            };
            swatch.MouseLeftButtonUp += (_, _) => AccentColorBox.Text = hex;
            SwatchPanel.Children.Add(swatch);
        }
    }

    private void OnScreenChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ScreenPicker.SelectedItem is ScreenInfo screen)
        {
            _selectedScreen = screen;
            _workingLayout.SelectedScreenId = screen.Id;
            RebuildCanvas();
        }
    }

    private void RebuildCanvas()
    {
        // Size the canvas to the selected monitor's true pixel resolution so the
        // surrounding Viewbox renders the layout to scale.
        DesignCanvas.Width = _selectedScreen.Bounds.Width;
        DesignCanvas.Height = _selectedScreen.Bounds.Height;
        CanvasResolutionLabel.Text = $"{(int)_selectedScreen.Bounds.Width} × {(int)_selectedScreen.Bounds.Height}";

        DesignCanvas.Children.Clear();

        foreach (var instance in _workingLayout.Widgets)
        {
            AddBoxForInstance(instance);
        }
    }

    private void AddBoxForInstance(WidgetInstance instance)
    {
        var definition = WidgetRegistry.Get(instance.WidgetType);
        var widget = definition.CreateWidget();
        widget.Configure(instance);

        var box = new DesignerWidgetBox(instance, widget);
        box.Selected += OnWidgetSelected;
        box.BoundsChanged += _ => { if (_selectedInstance == instance) PopulatePropertiesPanel(instance); };

        DesignCanvas.Children.Add(box);
    }

    private void OnWidgetSelected(WidgetInstance instance)
    {
        _selectedInstance = instance;
        PropertiesPanel.IsEnabled = true;
        PopulatePropertiesPanel(instance);
    }

    private void PopulatePropertiesPanel(WidgetInstance instance)
    {
        XBox.Text = instance.X.ToString("0");
        YBox.Text = instance.Y.ToString("0");
        WidthBox.Text = instance.Width.ToString("0");
        HeightBox.Text = instance.Height.ToString("0");
        OpacityBox.Text = instance.Opacity.ToString("0.00");
        AccentColorBox.Text = instance.AccentColorHex;
        FontSizeBox.Text = instance.FontSize.ToString("0");

        var isNowPlaying = instance.WidgetType == "NowPlaying";
        NowPlayingOptions.Visibility = isNowPlaying ? Visibility.Visible : Visibility.Collapsed;
        if (isNowPlaying)
        {
            ShowCoverCheck.IsChecked = ReadBoolSetting(instance, "showCover", true);
            SpinCoverCheck.IsChecked = ReadBoolSetting(instance, "spinCover", true);
        }

        var isBluetooth = instance.WidgetType == "Bluetooth";
        BluetoothOptions.Visibility = isBluetooth ? Visibility.Visible : Visibility.Collapsed;
        if (isBluetooth)
        {
            _ = PopulateBluetoothDevicesAsync(instance.Settings.TryGetValue("deviceId", out var id) ? id : null);
        }
    }

    private async System.Threading.Tasks.Task PopulateBluetoothDevicesAsync(string? selectedId)
    {
        var devices = await new Widgets.Bluetooth.BluetoothDeviceService().GetPairedDevicesAsync();
        BluetoothDevicePicker.ItemsSource = devices;
        BluetoothDevicePicker.SelectedItem = devices.FirstOrDefault(d => d.Id == selectedId);
    }

    private void OnRefreshBluetooth(object sender, RoutedEventArgs e)
    {
        var currentId = _selectedInstance?.Settings.TryGetValue("deviceId", out var id) == true ? id : null;
        _ = PopulateBluetoothDevicesAsync(currentId);
    }

    private static bool ReadBoolSetting(WidgetInstance instance, string key, bool fallback)
        => instance.Settings.TryGetValue(key, out var raw) && bool.TryParse(raw, out var value) ? value : fallback;

    private void OnApplyProperties(object sender, RoutedEventArgs e)
    {
        if (_selectedInstance == null) return;

        if (double.TryParse(XBox.Text, out var x)) _selectedInstance.X = x;
        if (double.TryParse(YBox.Text, out var y)) _selectedInstance.Y = y;
        if (double.TryParse(WidthBox.Text, out var w)) _selectedInstance.Width = w;
        if (double.TryParse(HeightBox.Text, out var h)) _selectedInstance.Height = h;
        if (double.TryParse(OpacityBox.Text, out var o)) _selectedInstance.Opacity = o;
        if (double.TryParse(FontSizeBox.Text, out var fs)) _selectedInstance.FontSize = fs;
        _selectedInstance.AccentColorHex = AccentColorBox.Text;

        if (_selectedInstance.WidgetType == "NowPlaying")
        {
            _selectedInstance.Settings["showCover"] = (ShowCoverCheck.IsChecked == true).ToString();
            _selectedInstance.Settings["spinCover"] = (SpinCoverCheck.IsChecked == true).ToString();
        }

        if (_selectedInstance.WidgetType == "Bluetooth"
            && BluetoothDevicePicker.SelectedItem is Widgets.Bluetooth.BluetoothDeviceEntry device)
        {
            _selectedInstance.Settings["deviceId"] = device.Id;
            _selectedInstance.Settings["deviceName"] = device.Name;
        }

        RebuildCanvas();
    }

    private void OnAccentHexChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressColorSync) return;
        if (!ColorHex.TryParseRgb(AccentColorBox.Text, out var r, out var g, out var b)) return;

        _suppressColorSync = true;
        RSlider.Value = r;
        GSlider.Value = g;
        BSlider.Value = b;
        _suppressColorSync = false;

        UpdateAccentPreview(r, g, b);
    }

    private void OnRgbSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressColorSync) return;

        var r = (byte)RSlider.Value;
        var g = (byte)GSlider.Value;
        var b = (byte)BSlider.Value;

        _suppressColorSync = true;
        AccentColorBox.Text = ColorHex.Format(r, g, b);
        _suppressColorSync = false;

        UpdateAccentPreview(r, g, b);
    }

    private void UpdateAccentPreview(byte r, byte g, byte b)
        => AccentPreview.Background = new SolidColorBrush(Color.FromRgb(r, g, b));

    private void OnAddWidget(object sender, RoutedEventArgs e)
    {
        if (WidgetPalette.SelectedItem is not WidgetTypeDefinition definition) return;

        var instance = definition.CreateDefaultInstance();
        instance.Id = Guid.NewGuid().ToString();
        instance.X = 20;
        instance.Y = 20;

        _workingLayout.Widgets.Add(instance);
        AddBoxForInstance(instance);
    }

    private void OnApply(object sender, RoutedEventArgs e)
    {
        // Persist the working layout and live-update the overlay, but keep the
        // Designer window open so the user can continue editing.
        _onSaved(_workingLayout);
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _onSaved(_workingLayout);
        Close();
    }

    private void OnDiscard(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnSaveLocation(object sender, RoutedEventArgs e)
    {
        var locationName = WeatherLocationBox.Text;
        if (string.IsNullOrWhiteSpace(locationName)) return;

        (double Latitude, double Longitude)? coords;
        try
        {
            var weatherService = new Widgets.Weather.WeatherService();
            coords = await weatherService.GeocodeAsync(locationName);
        }
        catch
        {
            // Network/parse failure: keep the typed name but leave coordinates unresolved.
            coords = null;
        }

        var settings = Persistence.SettingsStore.Load(Persistence.AppPaths.SettingsFilePath);
        settings.WeatherLocationName = locationName;
        if (coords != null)
        {
            settings.WeatherLatitude = coords.Value.Latitude;
            settings.WeatherLongitude = coords.Value.Longitude;
        }
        Persistence.SettingsStore.Save(Persistence.AppPaths.SettingsFilePath, settings);

        LocationStatus.Text = coords != null
            ? $"Saved: {locationName} ({coords.Value.Latitude:0.00}, {coords.Value.Longitude:0.00})"
            : $"Saved \"{locationName}\", but its coordinates couldn't be resolved. Check the name or your connection.";
    }

    private void OnAutostartChanged(object sender, RoutedEventArgs e)
    {
        Startup.AutostartManager.SetEnabled(AutostartCheckBox.IsChecked == true);
    }
}
