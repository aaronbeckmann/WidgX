using System.Collections.Generic;
using System.Linq;

namespace WidgX.Widgets;

public static class WidgetRegistry
{
    private static readonly Dictionary<string, WidgetTypeDefinition> Definitions = new();

    public static void Register(WidgetTypeDefinition definition)
    {
        Definitions[definition.TypeName] = definition;
    }

    public static WidgetTypeDefinition Get(string typeName)
    {
        return Definitions[typeName];
    }

    public static IReadOnlyList<WidgetTypeDefinition> All => Definitions.Values.ToList();

    public static void Clear()
    {
        Definitions.Clear();
    }
}
