using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WidgX.Models;
using WidgX.Overlay;

namespace WidgX.Designer;

public partial class DesignerWindow : Window
{
    private readonly Layout _workingLayout;
    private readonly Action<Layout> _onSaved;
    private ScreenInfo _selectedScreen;

    public DesignerWindow(Layout initialLayout, Action<Layout> onSaved)
    {
        InitializeComponent();

        // Deep-copy so Discard can throw away in-progress edits.
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

    private double CanvasScale => DesignCanvas.ActualWidth > 0 && _selectedScreen.BoundsDip.Width > 0
        ? Math.Min(DesignCanvas.ActualWidth / _selectedScreen.BoundsDip.Width, DesignCanvas.ActualHeight / _selectedScreen.BoundsDip.Height)
        : 1.0;

    private void RebuildCanvas()
    {
        DesignCanvas.Children.Clear();

        foreach (var instance in _workingLayout.Widgets)
        {
            var definition = Widgets.WidgetRegistry.Get(instance.WidgetType);
            var widget = definition.CreateWidget();
            widget.Configure(instance);

            var box = new DesignerWidgetBox(instance, widget);
            box.BoundsChanged += _ => { /* properties panel sync wired in Task 6 */ };
            box.Selected += _ => { /* properties panel wired in Task 6 */ };

            DesignCanvas.Children.Add(box);
        }
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
}
