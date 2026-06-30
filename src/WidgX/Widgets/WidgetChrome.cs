using Border = System.Windows.Controls.Border;
using Color = System.Windows.Media.Color;
using ContentControl = System.Windows.Controls.ContentControl;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace WidgX.Widgets;

public static class WidgetChrome
{
    /// <summary>
    /// Applies the configured opacity to ONLY the widget's card background, so the
    /// card fades against the wallpaper while its text/graphics stay fully opaque.
    /// Assumes the widget's root element is the card <see cref="Border"/>.
    /// </summary>
    public static void ApplyBackgroundOpacity(ContentControl widget, double opacity)
    {
        if (widget.Content is Border card)
        {
            var color = (card.Background as SolidColorBrush)?.Color ?? Color.FromRgb(0x1B, 0x1B, 0x22);
            card.Background = new SolidColorBrush(color) { Opacity = opacity };
        }
    }
}
