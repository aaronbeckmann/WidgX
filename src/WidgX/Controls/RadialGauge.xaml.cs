using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Point = System.Windows.Point;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Size = System.Windows.Size;

namespace WidgX.Controls;

/// <summary>
/// A circular progress gauge: a track ring plus a foreground arc whose sweep is
/// proportional to <see cref="Value"/> (0..100). Value changes animate smoothly.
/// Geometry math lives in the pure, tested <see cref="GaugeGeometry"/>.
/// </summary>
public partial class RadialGauge : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(RadialGauge),
        new PropertyMetadata(0.0, OnValueChanged));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(RadialGauge),
        new PropertyMetadata(string.Empty, OnLabelChanged));

    public static readonly DependencyProperty AccentProperty = DependencyProperty.Register(
        nameof(Accent), typeof(Brush), typeof(RadialGauge),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7)), OnAccentChanged));

    // Internal animated angle (0..360) that the arc is currently drawn at.
    private static readonly DependencyProperty AngleProperty = DependencyProperty.Register(
        nameof(Angle), typeof(double), typeof(RadialGauge),
        new PropertyMetadata(0.0, OnAngleChanged));

    public RadialGauge()
    {
        InitializeComponent();
        ValueArc.Stroke = Accent;
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public Brush Accent
    {
        get => (Brush)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    private double Angle
    {
        get => (double)GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var gauge = (RadialGauge)d;
        var target = GaugeGeometry.SweepAngle((double)e.NewValue);
        gauge.ValueText.Text = $"{Math.Clamp((double)e.NewValue, 0, 100):0}%";

        var animation = new DoubleAnimation
        {
            To = target,
            Duration = TimeSpan.FromMilliseconds(350),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        gauge.BeginAnimation(AngleProperty, animation);
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((RadialGauge)d).LabelText.Text = (string)e.NewValue;

    private static void OnAccentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((RadialGauge)d).ValueArc.Stroke = (Brush)e.NewValue;

    private static void OnAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((RadialGauge)d).UpdateArc();

    private void OnRootSizeChanged(object sender, SizeChangedEventArgs e) => UpdateArc();

    private void UpdateArc()
    {
        double w = Root.ActualWidth, h = Root.ActualHeight;
        if (w <= 0 || h <= 0) return;

        double thickness = Math.Max(4, Math.Min(w, h) * 0.12);
        double radius = Math.Min(w, h) / 2 - thickness / 2;
        if (radius <= 0) return;

        var center = new Point(w / 2, h / 2);

        TrackEllipse.StrokeThickness = thickness;
        TrackEllipse.Width = radius * 2;
        TrackEllipse.Height = radius * 2;

        ValueArc.StrokeThickness = thickness;

        double sweep = Angle;
        if (sweep <= 0.01)
        {
            ValueArc.Data = null;
            return;
        }

        if (sweep >= 359.99)
        {
            ValueArc.Data = new EllipseGeometry(center, radius, radius);
            return;
        }

        var start = GaugeGeometry.PointOnArc(center, radius, 0);
        var end = GaugeGeometry.PointOnArc(center, radius, sweep);

        var figure = new PathFigure { StartPoint = start, IsClosed = false };
        figure.Segments.Add(new ArcSegment
        {
            Point = end,
            Size = new Size(radius, radius),
            IsLargeArc = GaugeGeometry.IsLargeArc(sweep),
            SweepDirection = SweepDirection.Clockwise
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        ValueArc.Data = geometry;
    }
}
