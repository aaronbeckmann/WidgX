using System.Collections.Generic;
using System.Windows;
using Point = System.Windows.Point;

namespace WidgX.Native;

public static class ClickThroughHitTester
{
    public static bool HitTest(Point pointInWindowDip, IEnumerable<Rect> widgetBoundsInDip)
    {
        foreach (var rect in widgetBoundsInDip)
        {
            if (rect.Contains(pointInWindowDip))
            {
                return true;
            }
        }

        return false;
    }
}
