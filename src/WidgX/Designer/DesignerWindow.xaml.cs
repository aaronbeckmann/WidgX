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
using FontFamily = System.Windows.Media.FontFamily;

namespace WidgX.Designer;

public partial class DesignerWindow : Window
{
    private readonly Layout _workingLayout;
    private readonly Action<Layout> _onSaved;
    private ScreenInfo _selectedScreen;
    private WidgetInstance? _selectedInstance;
    private bool _suppressColorSync;
    private readonly System.Collections.Generic.List<System.Windows.Shapes.Line> _guideLines = new();

    private static readonly string[] SwatchColors =
    {
        "#FFFFFF", "#4FC3F7", "#81C784", "#BA68C8", "#FFB74D", "#E57373",
        "#64B5F6", "#FFD54F", "#4DB6AC", "#F06292", "#A1887F", "#90A4AE"
    };

    private static readonly string[] ClockItems = { "Time", "Weekday", "Date" };

    private static readonly string[] FontNames =
    {
        "Segoe UI", "Segoe UI Variable Display", "Arial", "Calibri", "Cambria", "Consolas",
        "Courier New", "Georgia", "Impact", "Lucida Console", "Segoe Print", "Segoe Script",
        "Tahoma", "Times New Roman", "Trebuchet MS", "Verdana", "Comic Sans MS"
    };

    private sealed record DateFormatOption(string Label, string Format);

    private static readonly DateFormatOption[] DateFormats =
    {
        new("2026-06-30", "yyyy-MM-dd"),
        new("06/30/2026", "MM/dd/yyyy"),
        new("30.06.2026", "dd.MM.yyyy"),
        new("June 30, 2026", "MMMM d, yyyy"),
        new("30 Jun 2026", "d MMM yyyy"),
        new("Tue, Jun 30", "ddd, MMM d")
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
                    AccentColorHex = w.AccentColorHex, FontSize = w.FontSize, FontFamily = w.FontFamily,
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

        RestoreWindowBounds(settings);
        Closing += OnWindowClosing;

        BuildSwatches();
        ClockOrder1.ItemsSource = ClockItems;
        ClockOrder2.ItemsSource = ClockItems;
        ClockOrder3.ItemsSource = ClockItems;
        ClockDateFormat.ItemsSource = DateFormats;
        FontPicker.ItemsSource = BuildFontList();
        RebuildCanvas();
    }

    private void RestoreWindowBounds(Models.AppSettings settings)
    {
        if (settings.DesignerWindowWidth is not > 0 || settings.DesignerWindowHeight is not > 0) return;
        if (settings.DesignerWindowLeft is not { } left || settings.DesignerWindowTop is not { } top) return;

        var width = settings.DesignerWindowWidth.Value;
        var height = settings.DesignerWindowHeight.Value;

        // Only restore if the window would be visible on the current virtual screen.
        var virtualScreen = new Rect(
            SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        if (!virtualScreen.IntersectsWith(new Rect(left, top, width, height))) return;

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, Width, Height)
            : RestoreBounds;

        var settings = Persistence.SettingsStore.Load(Persistence.AppPaths.SettingsFilePath);
        settings.DesignerWindowLeft = bounds.Left;
        settings.DesignerWindowTop = bounds.Top;
        settings.DesignerWindowWidth = bounds.Width;
        settings.DesignerWindowHeight = bounds.Height;
        Persistence.SettingsStore.Save(Persistence.AppPaths.SettingsFilePath, settings);
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
        // Widget coordinates are in WPF device-independent units (DIPs), so the
        // canvas must be sized in DIPs (physical pixels / DPI scale) to match —
        // otherwise at non-100% scaling the canvas wouldn't represent the screen.
        // The label still shows the familiar physical resolution.
        var scale = MonitorScale.GetScaleFactor(_selectedScreen.Bounds);
        DesignCanvas.Width = _selectedScreen.Bounds.Width / scale;
        DesignCanvas.Height = _selectedScreen.Bounds.Height / scale;
        CanvasResolutionLabel.Text = $"{(int)_selectedScreen.Bounds.Width} × {(int)_selectedScreen.Bounds.Height}";

        DesignCanvas.Children.Clear();
        _guideLines.Clear();

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
        box.SnapResolver = ResolveSnap;
        box.DragEnded += ClearGuides;

        DesignCanvas.Children.Add(box);
    }

    private (double X, double Y) ResolveSnap(WidgetInstance dragged, double x, double y)
    {
        var others = _workingLayout.Widgets
            .Where(w => w != dragged)
            .Select(w => new Rect(w.X, w.Y, w.Width, w.Height))
            .ToList();

        // Threshold proportional to canvas width keeps the on-screen "stickiness"
        // roughly constant regardless of monitor resolution / Viewbox scale.
        var threshold = DesignCanvas.Width * 0.012;
        var result = SnapEngine.Snap(x, y, dragged.Width, dragged.Height,
            others, DesignCanvas.Width, DesignCanvas.Height, threshold);

        DrawGuides(result.GuideX, result.GuideY);
        return (result.X, result.Y);
    }

    private void DrawGuides(double? guideX, double? guideY)
    {
        ClearGuides();

        var thickness = Math.Max(0.5, DesignCanvas.Width * 0.0012);
        var brush = new SolidColorBrush(Color.FromRgb(0xFF, 0x4C, 0x9A));

        if (guideX is { } gx)
        {
            var line = new System.Windows.Shapes.Line
            {
                X1 = gx, X2 = gx, Y1 = 0, Y2 = DesignCanvas.Height,
                Stroke = brush, StrokeThickness = thickness, IsHitTestVisible = false
            };
            _guideLines.Add(line);
            DesignCanvas.Children.Add(line);
        }

        if (guideY is { } gy)
        {
            var line = new System.Windows.Shapes.Line
            {
                X1 = 0, X2 = DesignCanvas.Width, Y1 = gy, Y2 = gy,
                Stroke = brush, StrokeThickness = thickness, IsHitTestVisible = false
            };
            _guideLines.Add(line);
            DesignCanvas.Children.Add(line);
        }
    }

    private void ClearGuides()
    {
        foreach (var line in _guideLines)
        {
            DesignCanvas.Children.Remove(line);
        }
        _guideLines.Clear();
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

        var fonts = (System.Collections.Generic.IEnumerable<FontFamily>)FontPicker.ItemsSource;
        FontPicker.SelectedItem = fonts.FirstOrDefault(
            f => string.Equals(f.Source, instance.FontFamily, StringComparison.OrdinalIgnoreCase)) ?? fonts.First();

        var isNowPlaying = instance.WidgetType == "NowPlaying";
        NowPlayingOptions.Visibility = isNowPlaying ? Visibility.Visible : Visibility.Collapsed;
        if (isNowPlaying)
        {
            ShowCoverCheck.IsChecked = ReadBoolSetting(instance, "showCover", true);
            SpinCoverCheck.IsChecked = ReadBoolSetting(instance, "spinCover", true);
            SquareCoverCheck.IsChecked = ReadBoolSetting(instance, "squareCover", false);
            ColorBackgroundCheck.IsChecked = ReadBoolSetting(instance, "colorBackground", false);
        }

        var isBluetooth = instance.WidgetType == "Bluetooth";
        BluetoothOptions.Visibility = isBluetooth ? Visibility.Visible : Visibility.Collapsed;
        if (isBluetooth)
        {
            BluetoothAllDevicesCheck.IsChecked = ReadBoolSetting(instance, "showAll", false);
            _ = PopulateBatteryDevicesAsync(instance.Settings.TryGetValue("deviceName", out var dn) ? dn : null);
        }

        var isClock = instance.WidgetType == "Clock";
        ClockOptions.Visibility = isClock ? Visibility.Visible : Visibility.Collapsed;
        if (isClock)
        {
            ClockShowTime.IsChecked = ReadBoolSetting(instance, "showTime", true);
            ClockShowWeekday.IsChecked = ReadBoolSetting(instance, "showWeekday", true);
            ClockShowDate.IsChecked = ReadBoolSetting(instance, "showDate", true);

            var order = Widgets.Clock.ClockWidget.ResolveItemOrder(
                instance.Settings.TryGetValue("order", out var ord) ? ord : null);
            ClockOrder1.SelectedItem = order[0];
            ClockOrder2.SelectedItem = order[1];
            ClockOrder3.SelectedItem = order[2];

            ClockTimeSize.Text = instance.Settings.TryGetValue("timeFontSize", out var ts) ? ts : "28";
            ClockWeekdaySize.Text = instance.Settings.TryGetValue("weekdayFontSize", out var ws) ? ws : "14";
            ClockDateSize.Text = instance.Settings.TryGetValue("dateFontSize", out var ds) ? ds : "14";

            var currentFormat = instance.Settings.TryGetValue("dateFormat", out var df) ? df : "yyyy-MM-dd";
            ClockDateFormat.SelectedItem = DateFormats.FirstOrDefault(f => f.Format == currentFormat) ?? DateFormats[0];
        }
    }

    private async System.Threading.Tasks.Task PopulateBatteryDevicesAsync(string? selectedName)
    {
        var devices = await new Widgets.Battery.BatteryDeviceService().GetDevicesAsync();
        BluetoothDevicePicker.ItemsSource = devices;
        BluetoothDevicePicker.SelectedItem = devices.FirstOrDefault(
            d => string.Equals(d.Name, selectedName, StringComparison.OrdinalIgnoreCase));
    }

    private void OnRefreshBluetooth(object sender, RoutedEventArgs e)
    {
        var currentName = _selectedInstance?.Settings.TryGetValue("deviceName", out var dn) == true ? dn : null;
        _ = PopulateBatteryDevicesAsync(currentName);
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
        if (FontPicker.SelectedItem is FontFamily font) _selectedInstance.FontFamily = font.Source;
        _selectedInstance.AccentColorHex = AccentColorBox.Text;

        if (_selectedInstance.WidgetType == "NowPlaying")
        {
            _selectedInstance.Settings["showCover"] = (ShowCoverCheck.IsChecked == true).ToString();
            _selectedInstance.Settings["spinCover"] = (SpinCoverCheck.IsChecked == true).ToString();
            _selectedInstance.Settings["squareCover"] = (SquareCoverCheck.IsChecked == true).ToString();
            _selectedInstance.Settings["colorBackground"] = (ColorBackgroundCheck.IsChecked == true).ToString();
        }

        if (_selectedInstance.WidgetType == "Bluetooth")
        {
            if (BluetoothDevicePicker.SelectedItem is Widgets.Battery.BatteryDevice device)
            {
                _selectedInstance.Settings["deviceName"] = device.Name;
            }
            _selectedInstance.Settings["showAll"] = (BluetoothAllDevicesCheck.IsChecked == true).ToString();
        }

        if (_selectedInstance.WidgetType == "Clock")
        {
            _selectedInstance.Settings["showTime"] = (ClockShowTime.IsChecked == true).ToString();
            _selectedInstance.Settings["showWeekday"] = (ClockShowWeekday.IsChecked == true).ToString();
            _selectedInstance.Settings["showDate"] = (ClockShowDate.IsChecked == true).ToString();

            var order = $"{ClockOrder1.SelectedItem},{ClockOrder2.SelectedItem},{ClockOrder3.SelectedItem}";
            _selectedInstance.Settings["order"] = order;
            _selectedInstance.Settings["timeFontSize"] = ClockTimeSize.Text;
            _selectedInstance.Settings["weekdayFontSize"] = ClockWeekdaySize.Text;
            _selectedInstance.Settings["dateFontSize"] = ClockDateSize.Text;
            if (ClockDateFormat.SelectedItem is DateFormatOption fmt)
            {
                _selectedInstance.Settings["dateFormat"] = fmt.Format;
            }
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
