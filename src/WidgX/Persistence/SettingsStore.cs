using WidgX.Models;

namespace WidgX.Persistence;

public static class SettingsStore
{
    public static AppSettings Load(string path) => JsonFileStore.Load(path, new AppSettings());

    public static void Save(string path, AppSettings settings) => JsonFileStore.Save(path, settings);
}
