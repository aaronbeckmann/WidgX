using System;
using System.Collections.Generic;
using Rect = System.Windows.Rect;

namespace WidgX.Designer;

/// <summary>
/// Result of a snap: the adjusted top-left position, plus the coordinate of any
/// alignment guide that should be drawn (null when no snap occurred on that axis).
/// </summary>
public readonly record struct SnapResult(double X, double Y, double? GuideX, double? GuideY);

/// <summary>
/// PowerPoint-style alignment snapping. Snaps a dragged rectangle's left/center/
/// right edges to other rectangles' edges and centers (and the canvas center and
/// edges) when within a threshold, in canvas (DIP) coordinates.
/// </summary>
public static class SnapEngine
{
    public static SnapResult Snap(
        double x, double y, double width, double height,
        IReadOnlyList<Rect> others, double canvasWidth, double canvasHeight, double threshold)
    {
        var verticalTargets = new List<double> { 0, canvasWidth / 2, canvasWidth };
        var horizontalTargets = new List<double> { 0, canvasHeight / 2, canvasHeight };

        foreach (var r in others)
        {
            verticalTargets.Add(r.Left);
            verticalTargets.Add(r.Left + r.Width / 2);
            verticalTargets.Add(r.Right);

            horizontalTargets.Add(r.Top);
            horizontalTargets.Add(r.Top + r.Height / 2);
            horizontalTargets.Add(r.Bottom);
        }

        var (snappedX, guideX) = SnapAxis(x, width, verticalTargets, threshold);
        var (snappedY, guideY) = SnapAxis(y, height, horizontalTargets, threshold);

        return new SnapResult(snappedX, snappedY, guideX, guideY);
    }

    private static (double Position, double? Guide) SnapAxis(
        double position, double size, IReadOnlyList<double> targets, double threshold)
    {
        // The dragged edge offsets that may align to a target: left, center, right.
        var offsets = new[] { 0.0, size / 2, size };

        var bestDelta = double.MaxValue;
        var snapped = position;
        double? guide = null;

        foreach (var target in targets)
        {
            foreach (var offset in offsets)
            {
                var delta = Math.Abs(position + offset - target);
                if (delta <= threshold && delta < bestDelta)
                {
                    bestDelta = delta;
                    snapped = target - offset;
                    guide = target;
                }
            }
        }

        return (snapped, guide);
    }
}
