using System;

namespace WidgX.Widgets.Battery;

/// <summary>
/// Normalizes device names so the same physical device reported under several
/// Bluetooth profiles (e.g. "Sonos Ace" and "Sonos Ace Hands-Free AG") collapses
/// to a single friendly name.
/// </summary>
public static class BatteryNames
{
    private static readonly string[] ProfileSuffixes =
    {
        " Hands-Free AG", " Hands-Free", " Avrcp Transport", " Stereo", " AG", " LE"
    };

    public static string Clean(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        var cleaned = name.Trim();
        bool changed;
        do
        {
            changed = false;
            foreach (var suffix in ProfileSuffixes)
            {
                if (cleaned.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned[..^suffix.Length].TrimEnd();
                    changed = true;
                }
            }
        } while (changed);

        return cleaned;
    }
}
