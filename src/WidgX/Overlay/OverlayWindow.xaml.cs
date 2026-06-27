using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using Point = System.Windows.Point;
using WidgX.Models;
using WidgX.Native;

namespace WidgX.Overlay;

public partial class OverlayWindow : Window
{
    private List<Rect> _widgetBounds = new();
    private HwndSource? _hwndSource;
    private readonly WidgetHost _widgetHost = new();

    public OverlayWindow(Rect screenBoundsDip)
    {
        InitializeComponent();

        Left = screenBoundsDip.X;
        Top = screenBoundsDip.Y;
        Width = screenBoundsDip.Width;
        Height = screenBoundsDip.Height;

        SourceInitialized += OnSourceInitialized;
    }

    public void SetWidgetBounds(IEnumerable<Rect> bounds)
    {
        _widgetBounds = new List<Rect>(bounds);
    }

    public void ReloadLayout(Layout layout)
    {
        var bounds = _widgetHost.LoadLayout(layout, RootCanvas);
        SetWidgetBounds(bounds);
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        _hwndSource = HwndSource.FromHwnd(hwnd);
        _hwndSource?.AddHook(WndProc);

        DesktopLayerHelper.SendToDesktopLayer(hwnd);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_NCHITTEST)
        {
            var screenX = unchecked((short)((long)lParam & 0xFFFF));
            var screenY = unchecked((short)(((long)lParam >> 16) & 0xFFFF));
            var pointInWindow = PointFromScreen(new Point(screenX, screenY));

            var isOverWidget = ClickThroughHitTester.HitTest(pointInWindow, _widgetBounds);

            handled = true;
            return isOverWidget ? (IntPtr)NativeMethods.HTCLIENT : (IntPtr)NativeMethods.HTTRANSPARENT;
        }

        return IntPtr.Zero;
    }
}
