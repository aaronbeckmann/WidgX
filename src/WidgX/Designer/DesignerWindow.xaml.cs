using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WidgX.Models;
using WidgX.Overlay;
using WidgX.Widgets;

namespace WidgX.Designer;

public partial class DesignerWindow : Window
{
    private readonly Layout _workingLayout;
    private readonly Action<Layout> _onSaved;
    private ScreenInfo _selectedScreen;
    private WidgetInstance? _selectedInstance;

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

        RebuildCanvas();
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
    }

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

        RebuildCanvas();
    }

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
