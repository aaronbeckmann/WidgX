using System.Globalization;

namespace WidgX.Controls;

/// <summary>
/// Pure conversions between "#RRGGBB" hex strings and RGB byte components,
/// used by the Designer's color picker. Tolerant of a missing '#' and of an
/// 8-digit "#AARRGGBB" form (the alpha is ignored).
/// </summary>
public static class ColorHex
{
    public static bool TryParseRgb(string? hex, out byte r, out byte g, out byte b)
    {
        r = g = b = 0;
        if (string.IsNullOrWhiteSpace(hex)) return false;

        var s = hex.Trim();
        if (s.StartsWith('#')) s = s[1..];

        // Accept #AARRGGBB by dropping the leading alpha pair.
        if (s.Length == 8) s = s[2..];
        if (s.Length != 6) return false;

        if (!byte.TryParse(s.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r)) return false;
        if (!byte.TryParse(s.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)) return false;
        if (!byte.TryParse(s.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b)) return false;
        return true;
    }

    public static string Format(byte r, byte g, byte b)
        => $"#{r:X2}{g:X2}{b:X2}";
}
