using System;
using Point = System.Windows.Point;

namespace WidgX.Controls;

/// <summary>
/// Pure geometry helpers for drawing a circular progress gauge. The arc is
/// measured clockwise starting from the top (12 o'clock).
/// </summary>
public static class GaugeGeometry
{
    /// <summary>Sweep angle in degrees (0..360) for a 0..100 percentage.</summary>
    public static double SweepAngle(double percent)
    {
        var clamped = Math.Clamp(percent, 0, 100);
        return clamped / 100.0 * 360.0;
    }

    /// <summary>
    /// Point on a circle for an angle measured clockwise from the top.
    /// angle 0 → top, 90 → right, 180 → bottom, 270 → left.
    /// </summary>
    public static Point PointOnArc(Point center, double radius, double angleDegreesClockwiseFromTop)
    {
        var radians = angleDegreesClockwiseFromTop * Math.PI / 180.0;
        var x = center.X + radius * Math.Sin(radians);
        var y = center.Y - radius * Math.Cos(radians);
        return new Point(x, y);
    }

    /// <summary>An ArcSegment is "large" when its sweep exceeds 180 degrees.</summary>
    public static bool IsLargeArc(double sweepAngle) => sweepAngle > 180.0;
}
