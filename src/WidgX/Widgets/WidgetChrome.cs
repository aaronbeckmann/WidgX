using Border = System.Windows.Controls.Border;
using Color = System.Windows.Media.Color;
using ContentControl = System.Windows.Controls.ContentControl;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace WidgX.Widgets;

public static class WidgetChrome
{
    /// <summary>
    /// Applies the configured opacity to the widget's card background AND border,
    /// so the card chrome fades against the wallpaper while its text/graphics stay
    /// fully opaque. Assumes the widget's root element is the card <see cref="Border"/>.
    /// </summary>
    public static void ApplyBackgroundOpacity(ContentControl widget, double opacity)
    {
        if (widget.Content is not Border card) return;

        var backgroundColor = (card.Background as SolidColorBrush)?.Color ?? Color.FromRgb(0x1B, 0x1B, 0x22);
        card.Background = new SolidColorBrush(backgroundColor) { Opacity = opacity };

        if (card.BorderBrush is SolidColorBrush border)
        {
            card.BorderBrush = new SolidColorBrush(border.Color) { Opacity = opacity };
        }
    }
}
