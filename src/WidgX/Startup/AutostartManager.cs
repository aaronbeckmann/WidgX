using System;
using Microsoft.Win32;

namespace WidgX.Startup;

public static class AutostartManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string DefaultValueName = "WidgX";

    public static bool IsEnabled(string valueName = DefaultValueName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(valueName) != null;
    }

    public static void SetEnabled(bool enabled, string valueName = DefaultValueName, string? exePathOverride = null)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                         ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (enabled)
        {
            var exePath = exePathOverride ?? GetExecutablePath();
            key.SetValue(valueName, $"\"{exePath}\" --background");
        }
        else
        {
            key.DeleteValue(valueName, throwOnMissingValue: false);
        }
    }

    /// <summary>
    /// If autostart is enabled, rewrites its command to point at the current
    /// executable. Self-heals stale entries — e.g. after an update, or the earlier
    /// bug that stored the .dll path (which Windows can't launch).
    /// </summary>
    public static void RefreshIfEnabled(string valueName = DefaultValueName)
    {
        if (IsEnabled(valueName))
        {
            SetEnabled(true, valueName);
        }
    }

    // Environment.ProcessPath is the actual launching executable (WidgX.exe).
    // Assembly.Location points at the managed .dll for self-contained apps (and is
    // empty for single-file), which Windows cannot run as an autostart command.
    private static string GetExecutablePath()
        => Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
}
