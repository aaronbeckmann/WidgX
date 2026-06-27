using System;
using System.IO;

namespace WidgX.Persistence;

public static class AppPaths
{
    public static string GetAppDataDirectory()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WidgX");

        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string LayoutFilePath => Path.Combine(GetAppDataDirectory(), "layout.json");
    public static string SettingsFilePath => Path.Combine(GetAppDataDirectory(), "settings.json");
    public static string TodosFilePath => Path.Combine(GetAppDataDirectory(), "todos.json");
}
