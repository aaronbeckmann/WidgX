using System.Collections.Generic;
using System.Text.Json;
using WidgX.Models;
using Xunit;

namespace WidgX.Tests.Models;

public class ModelSerializationTests
{
    [Fact]
    public void Layout_RoundTrips_Through_Json()
    {
        var layout = new Layout
        {
            SelectedScreenId = "\\\\.\\DISPLAY1",
            Widgets = new List<WidgetInstance>
            {
                new WidgetInstance
                {
                    Id = "w1",
                    WidgetType = "Clock",
                    X = 10,
                    Y = 20,
                    Width = 200,
                    Height = 80,
                    Opacity = 0.9,
                    AccentColorHex = "#FFFFFF",
                    FontSize = 14,
                    Settings = new Dictionary<string, string>()
                }
            }
        };

        var json = JsonSerializer.Serialize(layout);
        var roundTripped = JsonSerializer.Deserialize<Layout>(json);

        Assert.Equal(layout.SelectedScreenId, roundTripped!.SelectedScreenId);
        Assert.Equal(1, roundTripped.Widgets.Count);
        Assert.Equal("Clock", roundTripped.Widgets[0].WidgetType);
        Assert.Equal(10, roundTripped.Widgets[0].X);
    }
}
