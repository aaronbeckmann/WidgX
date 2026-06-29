using System;
using System.Runtime.InteropServices;
using Rect = System.Windows.Rect;

namespace WidgX.Overlay;

/// <summary>
/// Resolves a monitor's DPI scale factor (e.g. 1.25 for 125%). Used to convert
/// a monitor's physical-pixel bounds into the device-independent units (DIPs)
/// that WPF — and therefore widget coordinates — actually use.
/// </summary>
public static class MonitorScale
{
    /// <summary>
    /// Scale factor for the monitor containing the centre of <paramref name="physicalBounds"/>.
    /// Returns 1.0 on any failure.
    /// </summary>
    public static double GetScaleFactor(Rect physicalBounds)
    {
        try
        {
            var center = new POINT
            {
                X = (int)(physicalBounds.X + physicalBounds.Width / 2),
                Y = (int)(physicalBounds.Y + physicalBounds.Height / 2)
            };

            var monitor = MonitorFromPoint(center, MONITOR_DEFAULTTONEAREST);
            if (GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out var dpiX, out _) == 0 && dpiX > 0)
            {
                return dpiX / 96.0;
            }
        }
        catch (Exception)
        {
            // Shcore unavailable / DPI-unaware host: fall through to 1.0.
        }

        return 1.0;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
    private const int MDT_EFFECTIVE_DPI = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("Shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
}
