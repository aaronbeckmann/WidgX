using WidgX.Models;

namespace WidgX.Persistence;

public static class LayoutStore
{
    public static Layout Load(string path) => JsonFileStore.Load(path, new Layout());

    public static void Save(string path, Layout layout) => JsonFileStore.Save(path, layout);
}
