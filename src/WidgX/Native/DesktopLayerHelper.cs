using System;

namespace WidgX.Native;

public static class DesktopLayerHelper
{
    public static void SendToDesktopLayer(IntPtr hwnd)
    {
        var progman = NativeMethods.FindWindow("Progman", null);
        if (progman == IntPtr.Zero)
        {
            return;
        }

        // Asking Progman to spawn a WorkerW behind the desktop icons is the
        // documented (if undocumented-by-Microsoft) trick every "desktop
        // wallpaper widget" tool, including Rainmeter, relies on.
        NativeMethods.SendMessageTimeout(
            progman, 0x052C, IntPtr.Zero, IntPtr.Zero,
            NativeMethods.SMTO_NORMAL, 1000, out _);

        IntPtr workerW = IntPtr.Zero;
        NativeMethods.EnumWindows((topHandle, _) =>
        {
            var shellView = NativeMethods.FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView != IntPtr.Zero)
            {
                workerW = NativeMethods.FindWindowEx(IntPtr.Zero, topHandle, "WorkerW", null);
            }

            return true;
        }, IntPtr.Zero);

        if (workerW != IntPtr.Zero)
        {
            NativeMethods.SetParent(hwnd, workerW);
        }
    }
}
