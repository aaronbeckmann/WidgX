using System.Collections.Generic;

namespace WidgX.Models;

public class WidgetInstance
{
    public string Id { get; set; } = string.Empty;
    public string WidgetType { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Opacity { get; set; } = 1.0;
    public string AccentColorHex { get; set; } = "#FFFFFF";
    public double FontSize { get; set; } = 14;
    public string FontFamily { get; set; } = "Segoe UI";
    public Dictionary<string, string> Settings { get; set; } = new();
}
