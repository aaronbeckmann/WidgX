using System.Threading.Tasks;
using Windows.Media.Control;

namespace WidgX.Widgets.NowPlaying;

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

    public async Task<(string? Title, string? Artist)> GetCurrentTrackAsync()
    {
        try
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            var session = manager.GetCurrentSession();
            if (session == null)
            {
                return (null, null);
            }

            var props = await session.TryGetMediaPropertiesAsync();
            return (props?.Title, props?.Artist);
        }
        catch (System.Exception)
        {
            return (null, null);
        }
    }
}
