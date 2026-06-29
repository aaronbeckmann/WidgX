using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WidgX.Models;
using WidgX.Widgets;

namespace WidgX.Overlay;

public class WidgetHost
{
    private readonly List<IWidget> _activeWidgets = new();

    public static Dictionary<string, Rect> BuildBounds(Layout layout)
    {
        var result = new Dictionary<string, Rect>();
        foreach (var widget in layout.Widgets)
        {
            result[widget.Id] = new Rect(widget.X, widget.Y, widget.Width, widget.Height);
        }
        return result;
    }

    public List<Rect> LoadLayout(Layout layout, Canvas hostCanvas)
    {
        foreach (var widget in _activeWidgets)
        {
            widget.StopUpdates();
        }
        _activeWidgets.Clear();
        hostCanvas.Children.Clear();

        var bounds = new List<Rect>();

        foreach (var instance in layout.Widgets)
        {
            if (!WidgetRegistry.TryGet(instance.WidgetType, out var definition))
            {
                continue; // Unknown/removed widget type — skip rather than crash the overlay.
            }

            var widget = definition!.CreateWidget();
            widget.Configure(instance);

            Canvas.SetLeft(widget.View, instance.X);
            Canvas.SetTop(widget.View, instance.Y);
            hostCanvas.Children.Add(widget.View);

            widget.StartUpdates();
            _activeWidgets.Add(widget);

            bounds.Add(new Rect(instance.X, instance.Y, instance.Width, instance.Height));
        }

        return bounds;
    }
}
