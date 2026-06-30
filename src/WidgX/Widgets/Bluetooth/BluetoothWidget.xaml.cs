using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WidgX.Models;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;

namespace WidgX.Widgets.Bluetooth;

public partial class BluetoothWidget : System.Windows.Controls.UserControl, IWidget
{
    private readonly DispatcherTimer _timer;
    private readonly BluetoothDeviceService _service = new();

    private string _deviceId = string.Empty;
    private string _deviceName = "No device";
    private bool _showAll;

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
        WidgetChrome.ApplyBackgroundOpacity(this, config.Opacity);
        FontSize = config.FontSize;

        _deviceId = config.Settings.TryGetValue("deviceId", out var id) ? id : string.Empty;
        _deviceName = config.Settings.TryGetValue("deviceName", out var name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : "No device";
        _showAll = config.Settings.TryGetValue("showAll", out var raw) && bool.TryParse(raw, out var b) && b;

        _ = Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates() => _timer.Stop();

    private async Task Refresh()
    {
        if (_showAll)
        {
            var connected = await _service.GetConnectedStatusesAsync();
            RenderRows(connected.ConvertAll(d => (d.Name, d.Status)));
            if (connected.Count == 0)
            {
                RenderRows(new List<(string, BluetoothStatus)> { ("No connected devices", new BluetoothStatus()) });
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(_deviceId))
        {
            RenderRows(new List<(string, BluetoothStatus)> { (_deviceName, new BluetoothStatus()) });
            return;
        }

        var status = await _service.GetStatusAsync(_deviceId);
        RenderRows(new List<(string, BluetoothStatus)> { (_deviceName, status) });
    }

    private void RenderRows(List<(string Name, BluetoothStatus Status)> rows)
    {
        DeviceList.Children.Clear();

        foreach (var (name, status) in rows)
        {
            var row = new DockPanel { LastChildFill = false, Margin = new Thickness(0, 1, 0, 1) };

            var icon = new TextBlock
            {
                Text = "",  // Bluetooth glyph
                FontFamily = new FontFamily("Segoe MDL2 Assets"), // glyph set below
                Foreground = (Brush)FindResource("AccentBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            DockPanel.SetDock(icon, Dock.Left);
            row.Children.Add(icon);

            var nameText = new TextBlock
            {
                Text = name,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(nameText, Dock.Left);
            row.Children.Add(nameText);

            var statusText = new TextBlock
            {
                Text = BluetoothStatusFormatter.Format(status.Connected, status.BatteryPercent, status.Charging),
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0)
            };
            DockPanel.SetDock(statusText, Dock.Right);
            row.Children.Add(statusText);

            DeviceList.Children.Add(row);
        }
    }
}
