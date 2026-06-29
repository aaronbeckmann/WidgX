using System;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace WidgX.Widgets.NowPlaying;

/// <summary>
/// Snapshot of the current media session, including timeline and cover art.
/// </summary>
public class NowPlayingInfo
{
    public string? Title { get; init; }
    public string? Artist { get; init; }
    public TimeSpan Position { get; init; }
    public TimeSpan Duration { get; init; }
    public IRandomAccessStreamReference? Thumbnail { get; init; }
}

public class MediaSessionService
{
    public static string FormatNowPlaying(string? title, string? artist)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Nothing playing";
        }

        return string.IsNullOrWhiteSpace(artist) ? title : $"{title} — {artist}";
    }

    public async Task<NowPlayingInfo> GetNowPlayingAsync()
    {
        try
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            var session = manager.GetCurrentSession();
            if (session == null)
            {
                return new NowPlayingInfo();
            }

            var props = await session.TryGetMediaPropertiesAsync();
            var timeline = session.GetTimelineProperties();

            var duration = timeline != null ? timeline.EndTime - timeline.StartTime : TimeSpan.Zero;

            return new NowPlayingInfo
            {
                Title = props?.Title,
                Artist = props?.Artist,
                Position = timeline?.Position ?? TimeSpan.Zero,
                Duration = duration,
                Thumbnail = props?.Thumbnail
            };
        }
        catch (Exception)
        {
            return new NowPlayingInfo();
        }
    }
}
