using System;
using System.Windows.Controls;
using System.Windows.Threading;
using WidgX.Models;

namespace WidgX.Widgets.SystemStats;

public partial class SystemStatsWidget : System.Windows.Controls.UserControl, IWidget
{
    private readonly DispatcherTimer _timer;
    private readonly SystemStatsService _service = new();

    public SystemStatsWidget()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => Refresh();
    }

    System.Windows.Controls.UserControl IWidget.View => this;

    public void Configure(WidgetInstance config)
    {
        Width = config.Width;
        Height = config.Height;
        Opacity = config.Opacity;
        FontSize = config.FontSize;
        Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates() => _timer.Stop();

    private void Refresh()
    {
        try
        {
            CpuText.Text = $"CPU: {_service.GetCpuUsagePercent():0}%";
            RamText.Text = $"RAM: {_service.GetRamUsagePercent():0}%";
            GpuText.Text = $"GPU: {_service.GetGpuUsagePercent():0}%";
        }
        catch (Exception)
        {
            CpuText.Text = "CPU: Unavailable";
            RamText.Text = "RAM: Unavailable";
            GpuText.Text = "GPU: Unavailable";
        }
    }
}
