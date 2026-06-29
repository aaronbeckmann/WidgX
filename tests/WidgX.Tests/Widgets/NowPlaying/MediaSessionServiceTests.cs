using WidgX.Widgets.NowPlaying;
using Xunit;

namespace WidgX.Tests.Widgets.NowPlaying;

public class MediaSessionServiceTests
{
    [Fact]
    public void FormatNowPlaying_With_Title_And_Artist()
    {
        Assert.Equal("Song — Artist", MediaSessionService.FormatNowPlaying("Song", "Artist"));
    }

    [Fact]
    public void FormatNowPlaying_With_Title_Only()
    {
        Assert.Equal("Song", MediaSessionService.FormatNowPlaying("Song", ""));
    }

    [Fact]
    public void FormatNowPlaying_With_Nothing_Playing()
    {
        Assert.Equal("Nothing playing", MediaSessionService.FormatNowPlaying(null, null));
    }
}
