using Border = System.Windows.Controls.Border;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Control = System.Windows.Controls.Control;
using ContentControl = System.Windows.Controls.ContentControl;
using FontFamily = System.Windows.Media.FontFamily;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace WidgX.Widgets;

public static class WidgetChrome
{
    /// <summary>
    /// Sets the widget's font family on its root, so all text that doesn't pin its
    /// own font (icon glyphs do) inherits it. Invalid names fall back to Segoe UI.
    /// </summary>
    public static void ApplyFont(Control widget, string? fontFamily)
    {
        var name = string.IsNullOrWhiteSpace(fontFamily) ? "Segoe UI" : fontFamily;
        try { widget.FontFamily = new FontFamily(name); }
        catch { widget.FontFamily = new FontFamily("Segoe UI"); }
    }

    /// <summary>Parses an accent hex (e.g. "#4FC3F7") into a brush, or null if invalid.</summary>
    public static Brush? ParseAccent(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        try
        {
            if (ColorConverter.ConvertFromString(hex) is Color color)
            {
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                return brush;
            }
        }
        catch
        {
            // fall through
        }
        return null;
    }

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
