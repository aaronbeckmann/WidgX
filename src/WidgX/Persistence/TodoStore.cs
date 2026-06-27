using System.Collections.Generic;
using WidgX.Models;

namespace WidgX.Persistence;

public static class TodoStore
{
    public static List<TodoItem> Load(string path) => JsonFileStore.Load(path, new List<TodoItem>());

    public static void Save(string path, List<TodoItem> items) => JsonFileStore.Save(path, items);
}
