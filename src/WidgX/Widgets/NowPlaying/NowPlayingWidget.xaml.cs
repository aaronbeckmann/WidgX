using System;
using System.Windows.Controls;
using System.Windows.Threading;
using WidgX.Models;

namespace WidgX.Widgets.NowPlaying;

public partial class NowPlayingWidget : System.Windows.Controls.UserControl, IWidget
{
    private readonly DispatcherTimer _timer;
    private readonly MediaSessionService _service = new();

    public NowPlayingWidget()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _timer.Tick += async (_, _) => await Refresh();
    }

    System.Windows.Controls.UserControl IWidget.View => this;

    public void Configure(WidgetInstance config)
    {
        Width = config.Width;
        Height = config.Height;
        Opacity = config.Opacity;
        FontSize = config.FontSize;
        _ = Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates() => _timer.Stop();

    private async System.Threading.Tasks.Task Refresh()
    {
        var (title, artist) = await _service.GetCurrentTrackAsync();
        NowPlayingText.Text = MediaSessionService.FormatNowPlaying(title, artist);
    }
}
