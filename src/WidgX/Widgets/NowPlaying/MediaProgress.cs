using System;

namespace WidgX.Widgets.NowPlaying;

/// <summary>
/// Pure helper for the now-playing progress bar.
/// </summary>
public static class MediaProgress
{
    /// <summary>
    /// Fraction of the track elapsed, clamped to 0..1. Returns 0 when the
    /// duration is unknown or non-positive.
    /// </summary>
    public static double Fraction(TimeSpan position, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) return 0;
        return Math.Clamp(position.TotalSeconds / duration.TotalSeconds, 0, 1);
    }
}
