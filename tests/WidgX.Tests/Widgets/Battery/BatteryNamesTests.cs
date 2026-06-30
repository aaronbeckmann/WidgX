using WidgX.Widgets.Battery;
using Xunit;

namespace WidgX.Tests.Widgets.Battery;

public class BatteryNamesTests
{
    [Theory]
    [InlineData("Sonos Ace Hands-Free AG", "Sonos Ace")]
    [InlineData("Sonos Ace", "Sonos Ace")]
    [InlineData("Headset Stereo", "Headset")]
    [InlineData("Mouse LE", "Mouse")]
    [InlineData("Xbox Wireless Controller", "Xbox Wireless Controller")]
    public void Clean_StripsProfileSuffixes(string input, string expected)
    {
        Assert.Equal(expected, BatteryNames.Clean(input));
    }
}
