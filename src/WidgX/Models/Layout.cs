using System.Collections.Generic;

namespace WidgX.Models;

public class Layout
{
    public string SelectedScreenId { get; set; } = string.Empty;
    public List<WidgetInstance> Widgets { get; set; } = new();
}
