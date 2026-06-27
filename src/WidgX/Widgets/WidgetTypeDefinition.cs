using System;
using WidgX.Models;

namespace WidgX.Widgets;

public class WidgetTypeDefinition
{
    public string TypeName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Func<IWidget> CreateWidget { get; set; } = () => throw new InvalidOperationException();
    public Func<WidgetInstance> CreateDefaultInstance { get; set; } = () => new WidgetInstance();
}
