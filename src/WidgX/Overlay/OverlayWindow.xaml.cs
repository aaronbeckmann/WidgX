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
    private Rect _screenBounds;

    public OverlayWindow(Rect screenBounds)
    {
        InitializeComponent();
        _screenBounds = screenBounds;

        Left = screenBounds.X;
        Top = screenBounds.Y;
        Width = screenBounds.Width;
        Height = screenBounds.Height;

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

    /// <summary>Repositions the overlay onto the given monitor bounds (physical pixels).</summary>
    public void MoveToScreen(Rect screenBounds)
    {
        if (screenBounds == _screenBounds) return;
        _screenBounds = screenBounds;

        Left = screenBounds.X;
        Top = screenBounds.Y;
        Width = screenBounds.Width;
        Height = screenBounds.Height;

        if (_hwndSource != null)
        {
            NativeMethods.SetWindowPos(
                _hwndSource.Handle, IntPtr.Zero,
                (int)screenBounds.X, (int)screenBounds.Y,
                (int)screenBounds.Width, (int)screenBounds.Height,
                NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        _hwndSource = HwndSource.FromHwnd(hwnd);
        _hwndSource?.AddHook(WndProc);

        DesktopLayerHelper.SendToDesktopLayer(hwnd);

        // Place the overlay using physical pixel bounds (DPI-independent). After
        // PerMonitorV2 + WorkerW reparenting, child coords are virtual-screen
        // physical pixels, matching Screen.Bounds.
        NativeMethods.SetWindowPos(
            hwnd, IntPtr.Zero,
            (int)_screenBounds.X, (int)_screenBounds.Y,
            (int)_screenBounds.Width, (int)_screenBounds.Height,
            NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
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
