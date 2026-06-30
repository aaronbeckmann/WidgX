using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using WidgX.Models;

namespace WidgX.Widgets.SystemStats;

public partial class SystemStatsWidget : System.Windows.Controls.UserControl, IWidget
{
    private readonly DispatcherTimer _timer;
    private readonly SystemStatsService _service = new();
    private bool _isSampling;

    public SystemStatsWidget()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += async (_, _) => await Refresh();
    }

    System.Windows.Controls.UserControl IWidget.View => this;

    public void Configure(WidgetInstance config)
    {
        Width = config.Width;
        Height = config.Height;
        WidgetChrome.ApplyBackgroundOpacity(this, config.Opacity);
        WidgetChrome.ApplyFont(this, config.FontFamily);
        WidgetChrome.ApplyTextShadow(this, config.TextShadow);
        FontSize = config.FontSize;
        _ = Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates()
    {
        _timer.Stop();
        _service.Dispose();
    }

    private async Task Refresh()
    {
        // Sampling reads performance counters and DXGI on each tick; doing that on
        // the UI thread janks the gauge/cover animations, so sample off-thread and
        // only touch the gauges back on the UI thread.
        if (_isSampling) return;
        _isSampling = true;
        try
        {
            var (cpu, ram, gpu, vram) = await Task.Run(() => (
                _service.GetCpuUsagePercent(),
                _service.GetRamUsagePercent(),
                _service.GetGpuUsagePercent(),
                _service.GetVramUsagePercent() ?? 0));

            CpuGauge.Value = cpu;
            RamGauge.Value = ram;
            GpuGauge.Value = gpu;
            VramGauge.Value = vram;
        }
        catch (Exception)
        {
            CpuGauge.Value = 0;
            RamGauge.Value = 0;
            GpuGauge.Value = 0;
            VramGauge.Value = 0;
        }
        finally
        {
            _isSampling = false;
        }
    }
}
