using System.Collections.Generic;
using WidgX.Models;
using WidgX.Overlay;
using Xunit;

namespace WidgX.Tests.Overlay;

public class WidgetHostTests
{
    [Fact]
    public void BuildBounds_Maps_Each_Widget_Id_To_Its_Rect()
    {
        var layout = new Layout
        {
            Widgets = new List<WidgetInstance>
            {
                new WidgetInstance { Id = "w1", X = 10, Y = 20, Width = 100, Height = 50 },
                new WidgetInstance { Id = "w2", X = 200, Y = 200, Width = 80, Height = 80 }
            }
        };

        var bounds = WidgetHost.BuildBounds(layout);

        Assert.Equal(2, bounds.Count);
        Assert.Equal(10, bounds["w1"].X);
        Assert.Equal(80, bounds["w2"].Width);
    }
}
