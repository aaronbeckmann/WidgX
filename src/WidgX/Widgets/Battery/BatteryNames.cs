using System;

namespace WidgX.Widgets.Battery;

/// <summary>
/// Normalizes device names by stripping purely technical Bluetooth transport
/// suffixes (e.g. "Sonos Ace Avrcp Transport" -> "Sonos Ace"), while preserving
/// meaningful profile distinctions like "Hands-Free". This way a device that
/// reports a different battery level per profile (e.g. "Sonos Ace" 86% and
/// "Sonos Ace Hands-Free" 50%) keeps both readings visible as separate entries.
/// </summary>
public static class BatteryNames
{
    private static readonly string[] ProfileSuffixes =
    {
        " Avrcp Transport", " Stereo", " AG", " LE"
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
