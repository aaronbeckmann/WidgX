using System;
using System.Collections.Generic;
using WidgX.Models;
using WidgX.Widgets;
using Xunit;

namespace WidgX.Tests.Widgets;

public class WidgetRegistryTests : IDisposable
{
    public WidgetRegistryTests()
    {
        WidgetRegistry.Clear();
    }

    [Fact]
    public void Register_Then_Get_Returns_Same_Definition()
    {
        var def = new WidgetTypeDefinition
        {
            TypeName = "Test",
            DisplayName = "Test Widget",
            CreateWidget = () => null!,
            CreateDefaultInstance = () => new WidgetInstance { WidgetType = "Test" }
        };

        WidgetRegistry.Register(def);

        var result = WidgetRegistry.Get("Test");
        Assert.Equal("Test Widget", result.DisplayName);
    }

    [Fact]
    public void Get_Unknown_Type_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() => WidgetRegistry.Get("DoesNotExist"));
    }

    [Fact]
    public void All_Lists_Every_Registered_Type()
    {
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "A", DisplayName = "A",
            CreateWidget = () => null!, CreateDefaultInstance = () => new WidgetInstance()
        });
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "B", DisplayName = "B",
            CreateWidget = () => null!, CreateDefaultInstance = () => new WidgetInstance()
        });

        Assert.Equal(2, WidgetRegistry.All.Count);
    }

    [Fact]
    public void TryGet_Returns_False_For_Unknown_Type()
    {
        var found = WidgetRegistry.TryGet("DoesNotExist", out var definition);
        Assert.False(found);
        Assert.Null(definition);
    }

    [Fact]
    public void TryGet_Returns_True_And_Definition_For_Known_Type()
    {
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "Known", DisplayName = "Known",
            CreateWidget = () => null!, CreateDefaultInstance = () => new WidgetInstance()
        });

        var found = WidgetRegistry.TryGet("Known", out var definition);
        Assert.True(found);
        Assert.Equal("Known", definition!.TypeName);
    }

    public void Dispose()
    {
        WidgetRegistry.Clear();
    }
}
