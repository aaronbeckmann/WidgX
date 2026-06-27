using System.IO;
using System.Text.Json;

namespace WidgX.Persistence;

public static class JsonFileStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public static T Load<T>(string filePath, T fallback)
    {
        if (!File.Exists(filePath))
        {
            return fallback;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var value = JsonSerializer.Deserialize<T>(json, Options);
            return value ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    public static void Save<T>(string filePath, T value)
    {
        var json = JsonSerializer.Serialize(value, Options);
        File.WriteAllText(filePath, json);
    }
}
