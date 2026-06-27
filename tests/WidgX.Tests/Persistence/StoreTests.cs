using System;
using System.Collections.Generic;
using System.IO;
using WidgX.Models;
using WidgX.Persistence;
using Xunit;

namespace WidgX.Tests.Persistence;

public class StoreTests : IDisposable
{
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");

    [Fact]
    public void LayoutStore_RoundTrips()
    {
        var layout = new Layout { SelectedScreenId = "DISPLAY1", Widgets = new List<WidgetInstance>() };
        LayoutStore.Save(_tempFile, layout);
        var loaded = LayoutStore.Load(_tempFile);
        Assert.Equal("DISPLAY1", loaded.SelectedScreenId);
    }

    [Fact]
    public void SettingsStore_RoundTrips()
    {
        var settings = new AppSettings { AutostartEnabled = true, WeatherLocationName = "London" };
        SettingsStore.Save(_tempFile, settings);
        var loaded = SettingsStore.Load(_tempFile);
        Assert.True(loaded.AutostartEnabled);
        Assert.Equal("London", loaded.WeatherLocationName);
    }

    [Fact]
    public void TodoStore_RoundTrips()
    {
        var items = new List<TodoItem> { new TodoItem { Id = "t1", Text = "Buy milk" } };
        TodoStore.Save(_tempFile, items);
        var loaded = TodoStore.Load(_tempFile);
        Assert.Equal(1, loaded.Count);
        Assert.Equal("Buy milk", loaded[0].Text);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }
}
