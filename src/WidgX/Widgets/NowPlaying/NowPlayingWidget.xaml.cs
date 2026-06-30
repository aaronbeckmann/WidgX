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
using Color = System.Windows.Media.Color;

namespace WidgX.Widgets.NowPlaying;

public partial class NowPlayingWidget : System.Windows.Controls.UserControl, IWidget
{
    private readonly DispatcherTimer _timer;
    private readonly MediaSessionService _service = new();

    private bool _showCover = true;
    private bool _spinCover = true;
    private bool _squareCover;
    private bool _colorBackground;
    private bool _spinning;
    private string? _lastTrackKey;
    private int _emptyReads;

    public NowPlayingWidget()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += async (_, _) => await Refresh();
        SizeChanged += (_, _) => UpdateCoverSize();
    }

    private void UpdateCoverSize()
    {
        // Scale the album art with the widget's height (leaving room for padding
        // and the progress bar), and keep the clip shape in sync.
        var size = Math.Clamp(ActualHeight - 36, 36, 240);
        CoverHost.Width = size;
        CoverHost.Height = size;

        // A square (rounded) cover is only offered when the disk isn't spinning.
        var square = !_spinCover && _squareCover;
        if (square)
        {
            var radius = size * 0.10;
            CoverImage.Clip = new RectangleGeometry(new System.Windows.Rect(0, 0, size, size), radius, radius);
            Spindle.Visibility = Visibility.Collapsed;
        }
        else
        {
            var r = size / 2;
            CoverImage.Clip = new EllipseGeometry(new System.Windows.Point(r, r), r, r);
            Spindle.Visibility = _spinCover ? Visibility.Visible : Visibility.Collapsed;
            Spindle.Width = Spindle.Height = Math.Max(6, size * 0.18);
        }
    }

    System.Windows.Controls.UserControl IWidget.View => this;

    public void Configure(WidgetInstance config)
    {
        Width = config.Width;
        Height = config.Height;
        WidgetChrome.ApplyBackgroundOpacity(this, config.Opacity);
        FontSize = config.FontSize;

        _showCover = ReadBool(config.Settings, "showCover", true);
        _spinCover = ReadBool(config.Settings, "spinCover", true);
        _squareCover = ReadBool(config.Settings, "squareCover", false);
        _colorBackground = ReadBool(config.Settings, "colorBackground", false);
        _lastTrackKey = null; // force the cover to reload on the next refresh

        if (!_colorBackground) StopColorBackground();
        UpdateCoverSize();
        _ = Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates()
    {
        _timer.Stop();
        UpdateSpin(false);
        StopColorBackground();
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
            StopColorBackground();
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

        var trackKey = $"{info.Title}|{info.Artist}";
        var trackChanged = trackKey != _lastTrackKey;
        if (trackChanged) _lastTrackKey = trackKey;

        BitmapImage? bmp = null;
        if ((_showCover || _colorBackground) && trackChanged)
        {
            bmp = await LoadThumbnailAsync(info.Thumbnail);
        }

        if (_showCover)
        {
            if (trackChanged)
            {
                CoverImage.Source = bmp;
                CoverHost.Visibility = bmp != null ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        else
        {
            CoverHost.Visibility = Visibility.Collapsed;
        }

        if (_colorBackground && trackChanged)
        {
            if (bmp != null)
            {
                var (pixels, pw, ph) = GetCoverPixels(bmp, 24);
                StartColorBackground(PaletteExtractor.Extract(pixels, pw, ph, 4));
            }
            else
            {
                StopColorBackground();
            }
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

    private static (byte[] Pixels, int Width, int Height) GetCoverPixels(BitmapSource source, int target)
    {
        var scale = (double)target / Math.Max(source.PixelWidth, source.PixelHeight);
        var scaled = new TransformedBitmap(source, new ScaleTransform(scale, scale));
        var converted = new FormatConvertedBitmap(scaled, PixelFormats.Bgra32, null, 0);

        int w = converted.PixelWidth, h = converted.PixelHeight;
        var stride = w * 4;
        var pixels = new byte[h * stride];
        converted.CopyPixels(pixels, stride, 0);
        return (pixels, w, h);
    }

    private void StartColorBackground(List<Color> palette)
    {
        if (palette.Count == 0)
        {
            StopColorBackground();
            return;
        }

        var colors = palette.Count >= 2
            ? palette
            : new List<Color> { palette[0], Darken(palette[0]) };

        ColorBackground.Visibility = Visibility.Visible;
        AnimateStop(ColorStop0, colors, 0);
        AnimateStop(ColorStop1, colors, colors.Count / 2);
    }

    private void StopColorBackground()
    {
        ColorStop0.BeginAnimation(GradientStop.ColorProperty, null);
        ColorStop1.BeginAnimation(GradientStop.ColorProperty, null);
        ColorBackground.Visibility = Visibility.Collapsed;
    }

    private static void AnimateStop(GradientStop stop, List<Color> colors, int phase)
    {
        var animation = new ColorAnimationUsingKeyFrames { RepeatBehavior = RepeatBehavior.Forever };
        const double secondsPerColor = 4.0;

        for (var i = 0; i <= colors.Count; i++)
        {
            var c = colors[(phase + i) % colors.Count];
            var soft = Color.FromArgb(0xB0, c.R, c.G, c.B); // keep the wash subtle
            animation.KeyFrames.Add(new LinearColorKeyFrame(soft, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(secondsPerColor * i))));
        }

        stop.BeginAnimation(GradientStop.ColorProperty, animation);
    }

    private static Color Darken(Color c)
        => Color.FromRgb((byte)(c.R * 0.5), (byte)(c.G * 0.5), (byte)(c.B * 0.5));

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
