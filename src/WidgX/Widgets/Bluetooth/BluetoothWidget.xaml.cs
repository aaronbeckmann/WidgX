using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using WidgX.Models;

namespace WidgX.Widgets.Bluetooth;

public partial class BluetoothWidget : System.Windows.Controls.UserControl, IWidget
{
    private readonly DispatcherTimer _timer;
    private readonly BluetoothDeviceService _service = new();

    private string _deviceId = string.Empty;

    public BluetoothWidget()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _timer.Tick += async (_, _) => await Refresh();
    }

    System.Windows.Controls.UserControl IWidget.View => this;

    public void Configure(WidgetInstance config)
    {
        Width = config.Width;
        Height = config.Height;
        Opacity = config.Opacity;
        FontSize = config.FontSize;

        _deviceId = config.Settings.TryGetValue("deviceId", out var id) ? id : string.Empty;
        DeviceNameText.Text = config.Settings.TryGetValue("deviceName", out var name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : "No device";

        _ = Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates() => _timer.Stop();

    private async Task Refresh()
    {
        if (string.IsNullOrWhiteSpace(_deviceId))
        {
            StatusText.Text = "✕";
            return;
        }

        var status = await _service.GetStatusAsync(_deviceId);
        StatusText.Text = BluetoothStatusFormatter.Format(status.Connected, status.BatteryPercent, status.Charging);
    }
}
