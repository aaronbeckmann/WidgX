using System;
using System.Collections.Generic;
using WidgX.Models;
using WidgX.Widgets.Todos;
using Xunit;

namespace WidgX.Tests.Widgets.Todos;

public class TodoSortingTests
{
    [Fact]
    public void Incomplete_Items_Come_Before_Completed_Items()
    {
        var items = new List<TodoItem>
        {
            new TodoItem { Id = "1", Text = "Done", IsCompleted = true },
            new TodoItem { Id = "2", Text = "Not done", IsCompleted = false }
        };

        var sorted = TodoWidget.SortForDisplay(items);

        Assert.Equal("2", sorted[0].Id);
        Assert.Equal("1", sorted[1].Id);
    }

    [Fact]
    public void Incomplete_Items_Sort_By_Due_Date_Nulls_Last()
    {
        var items = new List<TodoItem>
        {
            new TodoItem { Id = "no-date", Text = "No date", DueDate = null },
            new TodoItem { Id = "later", Text = "Later", DueDate = new DateTime(2026, 7, 1) },
            new TodoItem { Id = "soon", Text = "Soon", DueDate = new DateTime(2026, 6, 28) }
        };

        var sorted = TodoWidget.SortForDisplay(items);

        Assert.Equal("soon", sorted[0].Id);
        Assert.Equal("later", sorted[1].Id);
        Assert.Equal("no-date", sorted[2].Id);
    }
}
