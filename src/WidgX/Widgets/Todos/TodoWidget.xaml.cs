using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WidgX.Models;
using WidgX.Persistence;

namespace WidgX.Widgets.Todos;

public partial class TodoWidget : System.Windows.Controls.UserControl, IWidget
{
    private List<TodoItem> _items = new();

    public TodoWidget()
    {
        InitializeComponent();
    }

    System.Windows.Controls.UserControl IWidget.View => this;

    public static List<TodoItem> SortForDisplay(List<TodoItem> items)
    {
        var incomplete = items.Where(i => !i.IsCompleted)
            .OrderBy(i => i.DueDate ?? DateTime.MaxValue)
            .ToList();
        var completed = items.Where(i => i.IsCompleted).ToList();

        incomplete.AddRange(completed);
        return incomplete;
    }

    public void Configure(WidgetInstance config)
    {
        Width = config.Width;
        Height = config.Height;
        Opacity = config.Opacity;
        FontSize = config.FontSize;

        Reload();
    }

    public void StartUpdates() { }

    public void StopUpdates() { }

    private void Reload()
    {
        _items = TodoStore.Load(AppPaths.TodosFilePath);
        ItemsList.ItemsSource = SortForDisplay(_items);
    }

    private void Persist()
    {
        TodoStore.Save(AppPaths.TodosFilePath, _items);
        ItemsList.ItemsSource = SortForDisplay(_items);
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewItemText.Text)) return;

        _items.Add(new TodoItem
        {
            Id = Guid.NewGuid().ToString(),
            Text = NewItemText.Text,
            DueDate = NewItemDueDate.SelectedDate,
            IsCompleted = false
        });

        NewItemText.Text = string.Empty;
        NewItemDueDate.SelectedDate = null;
        Persist();
    }

    private void OnItemToggled(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.CheckBox { Tag: TodoItem item })
        {
            Persist();
        }
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: TodoItem item })
        {
            _items.RemoveAll(i => i.Id == item.Id);
            Persist();
        }
    }
}
