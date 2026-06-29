using WidgX.Startup;
using Xunit;

namespace WidgX.Tests.Startup;

public class LaunchArgsTests
{
    [Fact]
    public void IsBackgroundLaunch_True_When_Flag_Present()
    {
        Assert.True(LaunchArgs.IsBackgroundLaunch(new[] { "--background" }));
    }

    [Fact]
    public void IsBackgroundLaunch_False_When_Flag_Absent()
    {
        Assert.False(LaunchArgs.IsBackgroundLaunch(System.Array.Empty<string>()));
    }
}
