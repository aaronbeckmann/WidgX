using Microsoft.Win32;
using WidgX.Startup;
using Xunit;

namespace WidgX.Tests.Startup;

public class AutostartManagerTests
{
    private const string TestValueName = "WidgXTestEntry";

    [Fact]
    public void SetEnabled_True_Then_False_RoundTrips()
    {
        AutostartManager.SetEnabled(true, TestValueName, @"C:\fake\WidgX.exe");
        Assert.True(AutostartManager.IsEnabled(TestValueName));

        AutostartManager.SetEnabled(false, TestValueName, @"C:\fake\WidgX.exe");
        Assert.False(AutostartManager.IsEnabled(TestValueName));
    }
}
