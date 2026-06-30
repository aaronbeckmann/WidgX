using WidgX.Widgets.Bluetooth;
using Xunit;

namespace WidgX.Tests.Widgets.Bluetooth;

public class BluetoothStatusFormatterTests
{
    [Fact]
    public void Disconnected_ShowsCross()
    {
        Assert.Equal("✕", BluetoothStatusFormatter.Format(false, 80, false));
    }

    [Fact]
    public void Connected_WithBattery_ShowsPercent()
    {
        Assert.Equal("80%", BluetoothStatusFormatter.Format(true, 80, false));
    }

    [Fact]
    public void Connected_Charging_AppendsBolt()
    {
        Assert.Equal("80% ⚡", BluetoothStatusFormatter.Format(true, 80, true));
    }

    [Fact]
    public void Connected_NoBatteryReported_ShowsCheck()
    {
        Assert.Equal("✓", BluetoothStatusFormatter.Format(true, null, false));
    }
}
