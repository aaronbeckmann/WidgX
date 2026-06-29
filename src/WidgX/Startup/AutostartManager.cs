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
            var exePath = exePathOverride ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
            key.SetValue(valueName, $"\"{exePath}\" --background");
        }
        else
        {
            key.DeleteValue(valueName, throwOnMissingValue: false);
        }
    }
}
