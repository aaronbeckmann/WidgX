using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Storage.Streams;
using WidgX.Models;

namespace WidgX.Widgets.NowPlaying;

public partial class NowPlayingWidget : System.Windows.Controls.UserControl, IWidget
{
    private readonly DispatcherTimer _timer;
    private readonly MediaSessionService _service = new();

    private bool _showCover = true;
    private bool _spinCover = true;
    private bool _spinning;
    private string? _lastTrackKey;
    private int _emptyReads;

    public NowPlayingWidget()
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
        Opacity = config.Opacity;
        FontSize = config.FontSize;

        _showCover = ReadBool(config.Settings, "showCover", true);
        _spinCover = ReadBool(config.Settings, "spinCover", true);
        _lastTrackKey = null; // force the cover to reload on the next refresh

        _ = Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates()
    {
        _timer.Stop();
        UpdateSpin(false);
    }

    private static bool ReadBool(IDictionary<string, string> settings, string key, bool fallback)
    {
        if (settings.TryGetValue(key, out var raw) && bool.TryParse(raw, out var value))
        {
            return value;
        }
        return fallback;
    }

    private async Task Refresh()
    {
        var info = await _service.GetNowPlayingAsync();
        var hasTrack = !string.IsNullOrWhiteSpace(info.Title);

        if (!hasTrack)
        {
            // The SMTC session can briefly drop out between polls; tolerate a few
            // empty reads before tearing down so the cover doesn't stutter.
            if (++_emptyReads < 3) return;

            NowPlayingText.Text = MediaSessionService.FormatNowPlaying(null, null);
            TrackProgress.Visibility = Visibility.Collapsed;
            CoverHost.Visibility = Visibility.Collapsed;
            _lastTrackKey = null;
            UpdateSpin(false);
            return;
        }

        _emptyReads = 0;
        NowPlayingText.Text = MediaSessionService.FormatNowPlaying(info.Title, info.Artist);

        if (info.Duration > TimeSpan.Zero)
        {
            TrackProgress.Value = MediaProgress.Fraction(info.Position, info.Duration);
            TrackProgress.Visibility = Visibility.Visible;
        }
        else
        {
            TrackProgress.Visibility = Visibility.Collapsed;
        }

        if (_showCover)
        {
            var key = $"{info.Title}|{info.Artist}";
            if (key != _lastTrackKey)
            {
                _lastTrackKey = key;
                var bmp = await LoadThumbnailAsync(info.Thumbnail);
                CoverImage.Source = bmp;
                CoverHost.Visibility = bmp != null ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        else
        {
            CoverHost.Visibility = Visibility.Collapsed;
            _lastTrackKey = null;
        }

        // Spin only while actually playing, and never tear the animation down for
        // a transient read (handled by the hysteresis above).
        UpdateSpin(_showCover && _spinCover && info.IsPlaying
                   && CoverHost.Visibility == Visibility.Visible);
    }

    private void UpdateSpin(bool shouldSpin)
    {
        if (shouldSpin && !_spinning)
        {
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(6),
                RepeatBehavior = RepeatBehavior.Forever
            };
            CoverRotation.BeginAnimation(RotateTransform.AngleProperty, animation);
            _spinning = true;
        }
        else if (!shouldSpin && _spinning)
        {
            CoverRotation.BeginAnimation(RotateTransform.AngleProperty, null);
            CoverRotation.Angle = 0;
            _spinning = false;
        }
    }

    private static async Task<BitmapImage?> LoadThumbnailAsync(IRandomAccessStreamReference? reference)
    {
        if (reference == null) return null;

        try
        {
            using var randomStream = await reference.OpenReadAsync();
            using var source = randomStream.AsStreamForRead();
            var buffer = new MemoryStream();
            await source.CopyToAsync(buffer);
            buffer.Position = 0;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = buffer;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
