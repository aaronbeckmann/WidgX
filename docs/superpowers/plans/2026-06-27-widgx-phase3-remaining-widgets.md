# WidgX Phase 3: Todos, Uptime, System Stats, Now Playing, Weather — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Depends on:** Phase 1 and Phase 2 plans must be complete before starting this plan.

**Goal:** Implement the remaining five widget types (Todos with due dates, Uptime, CPU/RAM/GPU usage, Now Playing, Weather + forecast), plus the settings plumbing (weather location, autostart toggle) they need, following the same `IWidget`/`WidgetRegistry` pattern established in Phase 2.

**Architecture:** Each widget pairs a thin WPF `UserControl` with a small, independently testable service/helper that does the actual data gathering or formatting logic. OS-dependent reads (performance counters, media session, registry) are verified manually; pure formatting/mapping/aggregation logic gets xUnit tests.

**Tech Stack:** .NET 8, WPF (C#), xUnit, `System.Diagnostics.PerformanceCounter`, P/Invoke (`GlobalMemoryStatusEx`), WinRT `Windows.Media.Control` (requires bumping the app's target framework to a Windows SDK version), `HttpClient` against Open-Meteo (no API key).

## Global Constraints

(Same as Phase 1/2, plus:)
- No admin/elevation required for any new widget (rules out hardware temperature sensors).
- Each data service degrades independently and silently — a widget whose data source fails shows a clear "Unavailable" state rather than crashing the app.
- Weather location and autostart are configured once in a Settings tab, persisted via `SettingsStore`/`AppSettings` from Phase 1.

---

### Task 1: TodoWidget

**Files:**
- Create: `src/WidgX/Widgets/Todos/TodoWidget.xaml`
- Create: `src/WidgX/Widgets/Todos/TodoWidget.xaml.cs`
- Create: `src/WidgX/Widgets/Todos/TodoWidgetRegistration.cs`
- Test: `tests/WidgX.Tests/Widgets/Todos/TodoSortingTests.cs`

**Interfaces:**
- Consumes: `TodoStore.Load/Save`, `AppPaths.TodosFilePath`, `TodoItem`.
- Produces:
  - `TodoWidget : UserControl, IWidget`
  - `TodoWidget.SortForDisplay(List<TodoItem> items) : List<TodoItem>` — pure function: incomplete items first (sorted by due date, nulls last), then completed items. Unit-testable.

- [ ] **Step 1: Write the failing sort test**

`tests/WidgX.Tests/Widgets/Todos/TodoSortingTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter TodoSortingTests`
Expected: FAIL (compile error — `WidgX.Widgets.Todos` does not exist yet)

- [ ] **Step 3: Write the XAML view**

`src/WidgX/Widgets/Todos/TodoWidget.xaml`:

```xml
<UserControl x:Class="WidgX.Widgets.Todos.TodoWidget"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Border Background="#80000000" CornerRadius="6" Padding="10">
        <DockPanel>
            <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,6">
                <TextBox x:Name="NewItemText" Width="120" />
                <DatePicker x:Name="NewItemDueDate" Width="100" Margin="4,0,0,0" />
                <Button x:Name="AddButton" Content="+" Width="24" Margin="4,0,0,0" Click="OnAddClick" />
            </StackPanel>
            <ItemsControl x:Name="ItemsList">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Orientation="Horizontal" Margin="0,2,0,2">
                            <CheckBox IsChecked="{Binding IsCompleted}" Checked="OnItemToggled" Unchecked="OnItemToggled" Tag="{Binding}" />
                            <TextBlock Text="{Binding Text}" Foreground="White" Margin="4,0,0,0" />
                            <TextBlock Text="{Binding DueDate, StringFormat='({0:MM/dd})'}" Foreground="LightGray" Margin="4,0,0,0" />
                            <Button Content="x" Width="18" Margin="6,0,0,0" Click="OnDeleteClick" Tag="{Binding}" />
                        </StackPanel>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </DockPanel>
    </Border>
</UserControl>
```

- [ ] **Step 4: Implement the code-behind**

`src/WidgX/Widgets/Todos/TodoWidget.xaml.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WidgX.Models;
using WidgX.Persistence;

namespace WidgX.Widgets.Todos;

public partial class TodoWidget : UserControl, IWidget
{
    private List<TodoItem> _items = new();

    public TodoWidget()
    {
        InitializeComponent();
    }

    public UserControl View => this;

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
        if (sender is CheckBox { Tag: TodoItem item })
        {
            item.IsCompleted = !item.IsCompleted;
            Persist();
        }
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TodoItem item })
        {
            _items.RemoveAll(i => i.Id == item.Id);
            Persist();
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter TodoSortingTests`
Expected: PASS (2 tests)

- [ ] **Step 6: Implement registration**

`src/WidgX/Widgets/Todos/TodoWidgetRegistration.cs`:

```csharp
namespace WidgX.Widgets.Todos;

public static class TodoWidgetRegistration
{
    public static void Register()
    {
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "Todos",
            DisplayName = "Todos",
            CreateWidget = () => new TodoWidget(),
            CreateDefaultInstance = () => new Models.WidgetInstance
            {
                WidgetType = "Todos",
                Width = 240,
                Height = 260,
                Opacity = 0.9,
                AccentColorHex = "#FFFFFF",
                FontSize = 13
            }
        });
    }
}
```

- [ ] **Step 7: Commit**

```bash
git add src/WidgX/Widgets/Todos tests/WidgX.Tests/Widgets/Todos
git commit -m "Add TodoWidget with due dates, sorting logic, and full CRUD"
```

---

### Task 2: UptimeWidget

**Files:**
- Create: `src/WidgX/Widgets/Uptime/UptimeService.cs`
- Create: `src/WidgX/Widgets/Uptime/UptimeWidget.xaml`
- Create: `src/WidgX/Widgets/Uptime/UptimeWidget.xaml.cs`
- Create: `src/WidgX/Widgets/Uptime/UptimeWidgetRegistration.cs`
- Test: `tests/WidgX.Tests/Widgets/Uptime/UptimeServiceTests.cs`

**Interfaces:**
- Produces:
  - `UptimeService.GetUptime() : TimeSpan` — wraps `Environment.TickCount64`.
  - `UptimeService.Format(TimeSpan uptime) : string` — pure, e.g. `"3d 4h 12m"`. Unit-testable.
  - `UptimeWidget : UserControl, IWidget`

- [ ] **Step 1: Write the failing format test**

`tests/WidgX.Tests/Widgets/Uptime/UptimeServiceTests.cs`:

```csharp
using System;
using WidgX.Widgets.Uptime;
using Xunit;

namespace WidgX.Tests.Widgets.Uptime;

public class UptimeServiceTests
{
    [Theory]
    [InlineData(0, 0, 5, "0d 0h 5m")]
    [InlineData(3, 4, 12, "3d 4h 12m")]
    public void Format_Produces_Days_Hours_Minutes(int days, int hours, int minutes, string expected)
    {
        var span = new TimeSpan(days, hours, minutes, 0);
        Assert.Equal(expected, UptimeService.Format(span));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter UptimeServiceTests`
Expected: FAIL (compile error)

- [ ] **Step 3: Implement UptimeService**

`src/WidgX/Widgets/Uptime/UptimeService.cs`:

```csharp
using System;

namespace WidgX.Widgets.Uptime;

public static class UptimeService
{
    public static TimeSpan GetUptime() => TimeSpan.FromMilliseconds(Environment.TickCount64);

    public static string Format(TimeSpan uptime)
    {
        return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter UptimeServiceTests`
Expected: PASS (2 cases)

- [ ] **Step 5: Implement the widget**

`src/WidgX/Widgets/Uptime/UptimeWidget.xaml`:

```xml
<UserControl x:Class="WidgX.Widgets.Uptime.UptimeWidget"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Border Background="#80000000" CornerRadius="6" Padding="10">
        <StackPanel>
            <TextBlock Text="Uptime" FontSize="12" Foreground="LightGray" HorizontalAlignment="Center" />
            <TextBlock x:Name="UptimeText" FontSize="20" Foreground="White" HorizontalAlignment="Center" />
        </StackPanel>
    </Border>
</UserControl>
```

`src/WidgX/Widgets/Uptime/UptimeWidget.xaml.cs`:

```csharp
using System;
using System.Windows.Controls;
using System.Windows.Threading;
using WidgX.Models;

namespace WidgX.Widgets.Uptime;

public partial class UptimeWidget : UserControl, IWidget
{
    private readonly DispatcherTimer _timer;

    public UptimeWidget()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += (_, _) => Refresh();
    }

    public UserControl View => this;

    public void Configure(WidgetInstance config)
    {
        Width = config.Width;
        Height = config.Height;
        Opacity = config.Opacity;
        FontSize = config.FontSize;
        Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates() => _timer.Stop();

    private void Refresh()
    {
        UptimeText.Text = UptimeService.Format(UptimeService.GetUptime());
    }
}
```

- [ ] **Step 6: Implement registration**

`src/WidgX/Widgets/Uptime/UptimeWidgetRegistration.cs`:

```csharp
namespace WidgX.Widgets.Uptime;

public static class UptimeWidgetRegistration
{
    public static void Register()
    {
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "Uptime",
            DisplayName = "Uptime",
            CreateWidget = () => new UptimeWidget(),
            CreateDefaultInstance = () => new Models.WidgetInstance
            {
                WidgetType = "Uptime",
                Width = 160,
                Height = 70,
                Opacity = 0.9,
                AccentColorHex = "#FFFFFF",
                FontSize = 14
            }
        });
    }
}
```

- [ ] **Step 7: Commit**

```bash
git add src/WidgX/Widgets/Uptime tests/WidgX.Tests/Widgets/Uptime
git commit -m "Add UptimeWidget with testable duration formatting"
```

---

### Task 3: System Stats Widget (CPU / RAM / GPU)

**Files:**
- Create: `src/WidgX/Widgets/SystemStats/SystemStatsNativeMethods.cs`
- Create: `src/WidgX/Widgets/SystemStats/SystemStatsService.cs`
- Create: `src/WidgX/Widgets/SystemStats/SystemStatsWidget.xaml`
- Create: `src/WidgX/Widgets/SystemStats/SystemStatsWidget.xaml.cs`
- Create: `src/WidgX/Widgets/SystemStats/SystemStatsWidgetRegistration.cs`
- Test: `tests/WidgX.Tests/Widgets/SystemStats/SystemStatsCalculationTests.cs`

**Interfaces:**
- Produces:
  - `SystemStatsService.ComputeRamUsagePercent(ulong totalBytes, ulong availableBytes) : double` — pure, testable.
  - `SystemStatsService.AggregateGpuUsage(IEnumerable<double> perEngineInstanceUtilization) : double` — pure: sums per-engine-instance "Utilization Percentage" counter values and clamps to `[0, 100]`. This is the standard no-admin technique for reading GPU usage on Windows (same approach Task Manager uses), exposed as a pure function since the live counter enumeration itself isn't unit-testable.
  - `SystemStatsService.GetCpuUsagePercent() : double`, `SystemStatsService.GetRamUsagePercent() : double`, `SystemStatsService.GetGpuUsagePercent() : double` — live readers, not unit tested (verified manually).
  - `SystemStatsWidget : UserControl, IWidget`

- [ ] **Step 1: Write the failing tests for the pure calculations**

`tests/WidgX.Tests/Widgets/SystemStats/SystemStatsCalculationTests.cs`:

```csharp
using System.Collections.Generic;
using WidgX.Widgets.SystemStats;
using Xunit;

namespace WidgX.Tests.Widgets.SystemStats;

public class SystemStatsCalculationTests
{
    [Fact]
    public void ComputeRamUsagePercent_Computes_Correctly()
    {
        var result = SystemStatsService.ComputeRamUsagePercent(totalBytes: 16_000_000_000, availableBytes: 4_000_000_000);
        Assert.Equal(75.0, result, precision: 1);
    }

    [Fact]
    public void AggregateGpuUsage_Sums_Engine_Instances()
    {
        var result = SystemStatsService.AggregateGpuUsage(new List<double> { 10.0, 15.0, 5.0 });
        Assert.Equal(30.0, result, precision: 1);
    }

    [Fact]
    public void AggregateGpuUsage_Clamps_At_100()
    {
        var result = SystemStatsService.AggregateGpuUsage(new List<double> { 60.0, 60.0 });
        Assert.Equal(100.0, result, precision: 1);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter SystemStatsCalculationTests`
Expected: FAIL (compile error)

- [ ] **Step 3: Implement the GlobalMemoryStatusEx P/Invoke wrapper**

`src/WidgX/Widgets/SystemStats/SystemStatsNativeMethods.cs`:

```csharp
using System.Runtime.InteropServices;

namespace WidgX.Widgets.SystemStats;

[StructLayout(LayoutKind.Sequential)]
internal struct MEMORYSTATUSEX
{
    public uint dwLength;
    public uint dwMemoryLoad;
    public ulong ullTotalPhys;
    public ulong ullAvailPhys;
    public ulong ullTotalPageFile;
    public ulong ullAvailPageFile;
    public ulong ullTotalVirtual;
    public ulong ullAvailVirtual;
    public ulong ullAvailExtendedVirtual;
}

internal static class SystemStatsNativeMethods
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
}
```

- [ ] **Step 4: Implement SystemStatsService — pure calculations plus live readers**

`src/WidgX/Widgets/SystemStats/SystemStatsService.cs`:

```csharp
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WidgX.Widgets.SystemStats;

public class SystemStatsService
{
    private PerformanceCounter? _cpuCounter;

    public static double ComputeRamUsagePercent(ulong totalBytes, ulong availableBytes)
    {
        if (totalBytes == 0) return 0;
        var usedBytes = totalBytes - availableBytes;
        return (double)usedBytes / totalBytes * 100.0;
    }

    public static double AggregateGpuUsage(IEnumerable<double> perEngineInstanceUtilization)
    {
        var sum = perEngineInstanceUtilization.Sum();
        return sum > 100.0 ? 100.0 : sum;
    }

    public double GetCpuUsagePercent()
    {
        _cpuCounter ??= new PerformanceCounter("Processor", "% Processor Time", "_Total");
        return _cpuCounter.NextValue();
    }

    public double GetRamUsagePercent()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!SystemStatsNativeMethods.GlobalMemoryStatusEx(ref status))
        {
            return 0;
        }
        return ComputeRamUsagePercent(status.ullTotalPhys, status.ullAvailPhys);
    }

    public double GetGpuUsagePercent()
    {
        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var instanceNames = category.GetInstanceNames();
            var values = new List<double>();

            foreach (var instanceName in instanceNames)
            {
                foreach (var counter in category.GetCounters(instanceName))
                {
                    if (counter.CounterName == "Utilization Percentage")
                    {
                        values.Add(counter.NextValue());
                    }
                }
            }

            return AggregateGpuUsage(values);
        }
        catch (System.Exception)
        {
            return 0;
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter SystemStatsCalculationTests`
Expected: PASS (3 tests)

- [ ] **Step 6: Implement the widget**

`src/WidgX/Widgets/SystemStats/SystemStatsWidget.xaml`:

```xml
<UserControl x:Class="WidgX.Widgets.SystemStats.SystemStatsWidget"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Border Background="#80000000" CornerRadius="6" Padding="10">
        <StackPanel>
            <TextBlock x:Name="CpuText" Foreground="White" />
            <TextBlock x:Name="RamText" Foreground="White" />
            <TextBlock x:Name="GpuText" Foreground="White" />
        </StackPanel>
    </Border>
</UserControl>
```

`src/WidgX/Widgets/SystemStats/SystemStatsWidget.xaml.cs`:

```csharp
using System;
using System.Windows.Controls;
using System.Windows.Threading;
using WidgX.Models;

namespace WidgX.Widgets.SystemStats;

public partial class SystemStatsWidget : UserControl, IWidget
{
    private readonly DispatcherTimer _timer;
    private readonly SystemStatsService _service = new();

    public SystemStatsWidget()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => Refresh();
    }

    public UserControl View => this;

    public void Configure(WidgetInstance config)
    {
        Width = config.Width;
        Height = config.Height;
        Opacity = config.Opacity;
        FontSize = config.FontSize;
        Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates() => _timer.Stop();

    private void Refresh()
    {
        try
        {
            CpuText.Text = $"CPU: {_service.GetCpuUsagePercent():0}%";
            RamText.Text = $"RAM: {_service.GetRamUsagePercent():0}%";
            GpuText.Text = $"GPU: {_service.GetGpuUsagePercent():0}%";
        }
        catch (Exception)
        {
            CpuText.Text = "CPU: Unavailable";
            RamText.Text = "RAM: Unavailable";
            GpuText.Text = "GPU: Unavailable";
        }
    }
}
```

- [ ] **Step 7: Implement registration**

`src/WidgX/Widgets/SystemStats/SystemStatsWidgetRegistration.cs`:

```csharp
namespace WidgX.Widgets.SystemStats;

public static class SystemStatsWidgetRegistration
{
    public static void Register()
    {
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "SystemStats",
            DisplayName = "System Stats (CPU/RAM/GPU)",
            CreateWidget = () => new SystemStatsWidget(),
            CreateDefaultInstance = () => new Models.WidgetInstance
            {
                WidgetType = "SystemStats",
                Width = 180,
                Height = 100,
                Opacity = 0.9,
                AccentColorHex = "#FFFFFF",
                FontSize = 14
            }
        });
    }
}
```

- [ ] **Step 8: Manual verification**

Build and run (`dotnet run --project src/WidgX/WidgX.csproj`), add a System Stats widget via the Designer, save, and confirm on the live overlay that CPU/RAM/GPU percentages appear and update every ~2 seconds, roughly matching Task Manager's numbers (exact GPU figures may differ slightly between tools — that's expected, both use engine-level counters with slightly different aggregation).

- [ ] **Step 9: Commit**

```bash
git add src/WidgX/Widgets/SystemStats tests/WidgX.Tests/Widgets/SystemStats
git commit -m "Add SystemStatsWidget (CPU/RAM/GPU) using performance counters, no admin required"
```

---

### Task 4: Now Playing Widget

**Files:**
- Modify: `src/WidgX/WidgX.csproj`
- Create: `src/WidgX/Widgets/NowPlaying/MediaSessionService.cs`
- Create: `src/WidgX/Widgets/NowPlaying/NowPlayingWidget.xaml`
- Create: `src/WidgX/Widgets/NowPlaying/NowPlayingWidget.xaml.cs`
- Create: `src/WidgX/Widgets/NowPlaying/NowPlayingWidgetRegistration.cs`
- Test: `tests/WidgX.Tests/Widgets/NowPlaying/MediaSessionServiceTests.cs`

**Interfaces:**
- Produces:
  - `MediaSessionService.FormatNowPlaying(string? title, string? artist) : string` — pure, testable: `"Nothing playing"` if title is null/empty, `"Title — Artist"` if both present, just `"Title"` if artist is empty.
  - `MediaSessionService.GetCurrentTrackAsync() : Task<(string? Title, string? Artist)>` — wraps the WinRT SMTC API, not unit tested.
  - `NowPlayingWidget : UserControl, IWidget`

- [ ] **Step 1: Enable WinRT media APIs by targeting a Windows SDK version**

Modify `src/WidgX/WidgX.csproj` — change the `<TargetFramework>` from `net8.0-windows` to a version-qualified TFM so `Windows.Media.Control` is available:

```xml
<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
<TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
```

Run: `dotnet build src/WidgX/WidgX.csproj`
Expected: `Build succeeded.` (confirms the TFM bump alone doesn't break anything before adding new code)

- [ ] **Step 2: Write the failing formatting test**

`tests/WidgX.Tests/Widgets/NowPlaying/MediaSessionServiceTests.cs`:

```csharp
using WidgX.Widgets.NowPlaying;
using Xunit;

namespace WidgX.Tests.Widgets.NowPlaying;

public class MediaSessionServiceTests
{
    [Fact]
    public void FormatNowPlaying_With_Title_And_Artist()
    {
        Assert.Equal("Song — Artist", MediaSessionService.FormatNowPlaying("Song", "Artist"));
    }

    [Fact]
    public void FormatNowPlaying_With_Title_Only()
    {
        Assert.Equal("Song", MediaSessionService.FormatNowPlaying("Song", ""));
    }

    [Fact]
    public void FormatNowPlaying_With_Nothing_Playing()
    {
        Assert.Equal("Nothing playing", MediaSessionService.FormatNowPlaying(null, null));
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test --filter MediaSessionServiceTests`
Expected: FAIL (compile error)

- [ ] **Step 4: Implement MediaSessionService**

`src/WidgX/Widgets/NowPlaying/MediaSessionService.cs`:

```csharp
using System.Threading.Tasks;
using Windows.Media.Control;

namespace WidgX.Widgets.NowPlaying;

public class MediaSessionService
{
    public static string FormatNowPlaying(string? title, string? artist)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Nothing playing";
        }

        return string.IsNullOrWhiteSpace(artist) ? title : $"{title} — {artist}";
    }

    public async Task<(string? Title, string? Artist)> GetCurrentTrackAsync()
    {
        try
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            var session = manager.GetCurrentSession();
            if (session == null)
            {
                return (null, null);
            }

            var props = await session.TryGetMediaPropertiesAsync();
            return (props?.Title, props?.Artist);
        }
        catch (System.Exception)
        {
            return (null, null);
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter MediaSessionServiceTests`
Expected: PASS (3 tests)

- [ ] **Step 6: Implement the widget**

`src/WidgX/Widgets/NowPlaying/NowPlayingWidget.xaml`:

```xml
<UserControl x:Class="WidgX.Widgets.NowPlaying.NowPlayingWidget"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Border Background="#80000000" CornerRadius="6" Padding="10">
        <TextBlock x:Name="NowPlayingText" Foreground="White" TextWrapping="Wrap" />
    </Border>
</UserControl>
```

`src/WidgX/Widgets/NowPlaying/NowPlayingWidget.xaml.cs`:

```csharp
using System;
using System.Windows.Controls;
using System.Windows.Threading;
using WidgX.Models;

namespace WidgX.Widgets.NowPlaying;

public partial class NowPlayingWidget : UserControl, IWidget
{
    private readonly DispatcherTimer _timer;
    private readonly MediaSessionService _service = new();

    public NowPlayingWidget()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _timer.Tick += async (_, _) => await Refresh();
    }

    public UserControl View => this;

    public void Configure(WidgetInstance config)
    {
        Width = config.Width;
        Height = config.Height;
        Opacity = config.Opacity;
        FontSize = config.FontSize;
        _ = Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates() => _timer.Stop();

    private async System.Threading.Tasks.Task Refresh()
    {
        var (title, artist) = await _service.GetCurrentTrackAsync();
        NowPlayingText.Text = MediaSessionService.FormatNowPlaying(title, artist);
    }
}
```

- [ ] **Step 7: Implement registration**

`src/WidgX/Widgets/NowPlaying/NowPlayingWidgetRegistration.cs`:

```csharp
namespace WidgX.Widgets.NowPlaying;

public static class NowPlayingWidgetRegistration
{
    public static void Register()
    {
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "NowPlaying",
            DisplayName = "Now Playing",
            CreateWidget = () => new NowPlayingWidget(),
            CreateDefaultInstance = () => new Models.WidgetInstance
            {
                WidgetType = "NowPlaying",
                Width = 240,
                Height = 70,
                Opacity = 0.9,
                AccentColorHex = "#FFFFFF",
                FontSize = 13
            }
        });
    }
}
```

- [ ] **Step 8: Manual verification**

Play a track in Spotify (or any app that integrates with Windows media controls), add the Now Playing widget via the Designer, save, and confirm the overlay shows "Title — Artist" and updates within ~3 seconds of switching tracks. With nothing playing, confirm it shows "Nothing playing" rather than erroring.

- [ ] **Step 9: Commit**

```bash
git add src/WidgX/WidgX.csproj src/WidgX/Widgets/NowPlaying tests/WidgX.Tests/Widgets/NowPlaying
git commit -m "Add NowPlayingWidget using Windows SMTC media session API"
```

---

### Task 5: Weather Service and Settings (location + autostart toggle)

**Files:**
- Create: `src/WidgX/Widgets/Weather/WeatherModels.cs`
- Create: `src/WidgX/Widgets/Weather/WeatherCodeMapper.cs`
- Create: `src/WidgX/Widgets/Weather/WeatherService.cs`
- Create: `src/WidgX/Widgets/Weather/WeatherWidget.xaml`
- Create: `src/WidgX/Widgets/Weather/WeatherWidget.xaml.cs`
- Create: `src/WidgX/Widgets/Weather/WeatherWidgetRegistration.cs`
- Create: `src/WidgX/Startup/AutostartManager.cs`
- Modify: `src/WidgX/Designer/DesignerWindow.xaml`
- Modify: `src/WidgX/Designer/DesignerWindow.xaml.cs`
- Test: `tests/WidgX.Tests/Widgets/Weather/WeatherCodeMapperTests.cs`
- Test: `tests/WidgX.Tests/Startup/AutostartManagerTests.cs`

**Interfaces:**
- Produces:
  - `WeatherCodeMapper.Describe(int wmoCode) : string` — pure mapping from Open-Meteo's WMO weather codes to a short description (e.g. `0` → `"Clear sky"`, `61` → `"Light rain"`).
  - `WeatherService.GeocodeAsync(string locationName) : Task<(double Latitude, double Longitude)?>`
  - `WeatherService.GetForecastAsync(double lat, double lon) : Task<WeatherForecast>` where `WeatherForecast { double CurrentTempC; int CurrentWeatherCode; List<DailyForecast> Daily; }` and `DailyForecast { DateTime Date; double MaxTempC; double MinTempC; int WeatherCode; }`.
  - `AutostartManager.IsEnabled() : bool`, `AutostartManager.SetEnabled(bool enabled) : void` — manages the `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` value named `WidgX`.
  - Designer gains a "Settings" tab with weather location text box and autostart checkbox.

- [ ] **Step 1: Write the failing WeatherCodeMapper test**

`tests/WidgX.Tests/Widgets/Weather/WeatherCodeMapperTests.cs`:

```csharp
using WidgX.Widgets.Weather;
using Xunit;

namespace WidgX.Tests.Widgets.Weather;

public class WeatherCodeMapperTests
{
    [Theory]
    [InlineData(0, "Clear sky")]
    [InlineData(61, "Light rain")]
    [InlineData(95, "Thunderstorm")]
    [InlineData(9999, "Unknown")]
    public void Describe_Maps_Known_Codes(int code, string expected)
    {
        Assert.Equal(expected, WeatherCodeMapper.Describe(code));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter WeatherCodeMapperTests`
Expected: FAIL (compile error)

- [ ] **Step 3: Implement WeatherCodeMapper (WMO weather codes, the standard Open-Meteo coding scheme)**

`src/WidgX/Widgets/Weather/WeatherCodeMapper.cs`:

```csharp
using System.Collections.Generic;

namespace WidgX.Widgets.Weather;

public static class WeatherCodeMapper
{
    private static readonly Dictionary<int, string> Descriptions = new()
    {
        [0] = "Clear sky",
        [1] = "Mainly clear",
        [2] = "Partly cloudy",
        [3] = "Overcast",
        [45] = "Fog",
        [48] = "Freezing fog",
        [51] = "Light drizzle",
        [53] = "Drizzle",
        [55] = "Dense drizzle",
        [61] = "Light rain",
        [63] = "Rain",
        [65] = "Heavy rain",
        [71] = "Light snow",
        [73] = "Snow",
        [75] = "Heavy snow",
        [80] = "Light showers",
        [81] = "Showers",
        [82] = "Violent showers",
        [95] = "Thunderstorm",
        [96] = "Thunderstorm with hail",
        [99] = "Severe thunderstorm with hail"
    };

    public static string Describe(int wmoCode)
    {
        return Descriptions.TryGetValue(wmoCode, out var description) ? description : "Unknown";
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter WeatherCodeMapperTests`
Expected: PASS (4 cases)

- [ ] **Step 5: Implement the forecast models and WeatherService**

`src/WidgX/Widgets/Weather/WeatherModels.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace WidgX.Widgets.Weather;

public class WeatherForecast
{
    public double CurrentTempC { get; set; }
    public int CurrentWeatherCode { get; set; }
    public List<DailyForecast> Daily { get; set; } = new();
}

public class DailyForecast
{
    public DateTime Date { get; set; }
    public double MaxTempC { get; set; }
    public double MinTempC { get; set; }
    public int WeatherCode { get; set; }
}
```

`src/WidgX/Widgets/Weather/WeatherService.cs`:

```csharp
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace WidgX.Widgets.Weather;

public class WeatherService
{
    private readonly HttpClient _httpClient;

    public WeatherService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<(double Latitude, double Longitude)?> GeocodeAsync(string locationName)
    {
        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(locationName)}&count=1";
        var json = await _httpClient.GetStringAsync(url);

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
        {
            return null;
        }

        var first = results[0];
        return (first.GetProperty("latitude").GetDouble(), first.GetProperty("longitude").GetDouble());
    }

    public async Task<WeatherForecast> GetForecastAsync(double lat, double lon)
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}" +
                  "&current_weather=true&daily=temperature_2m_max,temperature_2m_min,weathercode&timezone=auto";
        var json = await _httpClient.GetStringAsync(url);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var forecast = new WeatherForecast
        {
            CurrentTempC = root.GetProperty("current_weather").GetProperty("temperature").GetDouble(),
            CurrentWeatherCode = root.GetProperty("current_weather").GetProperty("weathercode").GetInt32()
        };

        var daily = root.GetProperty("daily");
        var dates = daily.GetProperty("time");
        var maxTemps = daily.GetProperty("temperature_2m_max");
        var minTemps = daily.GetProperty("temperature_2m_min");
        var codes = daily.GetProperty("weathercode");

        for (var i = 0; i < dates.GetArrayLength(); i++)
        {
            forecast.Daily.Add(new DailyForecast
            {
                Date = DateTime.Parse(dates[i].GetString()!),
                MaxTempC = maxTemps[i].GetDouble(),
                MinTempC = minTemps[i].GetDouble(),
                WeatherCode = codes[i].GetInt32()
            });
        }

        return forecast;
    }
}
```

- [ ] **Step 6: Implement the widget**

`src/WidgX/Widgets/Weather/WeatherWidget.xaml`:

```xml
<UserControl x:Class="WidgX.Widgets.Weather.WeatherWidget"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Border Background="#80000000" CornerRadius="6" Padding="10">
        <StackPanel>
            <TextBlock x:Name="CurrentText" FontSize="18" Foreground="White" />
            <ItemsControl x:Name="ForecastList" Margin="0,6,0,0">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <TextBlock Foreground="LightGray" FontSize="12" Text="{Binding}" />
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </StackPanel>
    </Border>
</UserControl>
```

`src/WidgX/Widgets/Weather/WeatherWidget.xaml.cs`:

```csharp
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;
using WidgX.Models;
using WidgX.Persistence;

namespace WidgX.Widgets.Weather;

public partial class WeatherWidget : UserControl, IWidget
{
    private readonly DispatcherTimer _timer;
    private readonly WeatherService _service = new();

    public WeatherWidget()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(20) };
        _timer.Tick += async (_, _) => await Refresh();
    }

    public UserControl View => this;

    public void Configure(WidgetInstance config)
    {
        Width = config.Width;
        Height = config.Height;
        Opacity = config.Opacity;
        FontSize = config.FontSize;
        _ = Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates() => _timer.Stop();

    private async System.Threading.Tasks.Task Refresh()
    {
        var settings = SettingsStore.Load(AppPaths.SettingsFilePath);

        if (settings.WeatherLatitude is null || settings.WeatherLongitude is null)
        {
            CurrentText.Text = "Set a location in Settings";
            return;
        }

        try
        {
            var forecast = await _service.GetForecastAsync(settings.WeatherLatitude.Value, settings.WeatherLongitude.Value);

            CurrentText.Text = $"{forecast.CurrentTempC:0}°C, {WeatherCodeMapper.Describe(forecast.CurrentWeatherCode)}";

            ForecastList.ItemsSource = forecast.Daily.Take(5).Select(d =>
                $"{d.Date:ddd}: {d.MinTempC:0}–{d.MaxTempC:0}°C, {WeatherCodeMapper.Describe(d.WeatherCode)}");
        }
        catch (Exception)
        {
            CurrentText.Text = "Weather unavailable";
        }
    }
}
```

- [ ] **Step 7: Implement registration**

`src/WidgX/Widgets/Weather/WeatherWidgetRegistration.cs`:

```csharp
namespace WidgX.Widgets.Weather;

public static class WeatherWidgetRegistration
{
    public static void Register()
    {
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "Weather",
            DisplayName = "Weather + Forecast",
            CreateWidget = () => new WeatherWidget(),
            CreateDefaultInstance = () => new Models.WidgetInstance
            {
                WidgetType = "Weather",
                Width = 240,
                Height = 220,
                Opacity = 0.9,
                AccentColorHex = "#FFFFFF",
                FontSize = 13
            }
        });
    }
}
```

- [ ] **Step 8: Write the failing AutostartManager test**

`tests/WidgX.Tests/Startup/AutostartManagerTests.cs`:

```csharp
using Microsoft.Win32;
using WidgX.Startup;
using Xunit;

namespace WidgX.Tests.Startup;

public class AutostartManagerTests
{
    private const string TestValueName = "WidgXTestEntry";

    [Fact]
    public void SetEnabled_True_Then_False_RoundTrips()
    {
        AutostartManager.SetEnabled(true, TestValueName, @"C:\fake\WidgX.exe");
        Assert.True(AutostartManager.IsEnabled(TestValueName));

        AutostartManager.SetEnabled(false, TestValueName, @"C:\fake\WidgX.exe");
        Assert.False(AutostartManager.IsEnabled(TestValueName));
    }
}
```

- [ ] **Step 9: Run test to verify it fails**

Run: `dotnet test --filter AutostartManagerTests`
Expected: FAIL (compile error — `WidgX.Startup` does not exist yet)

- [ ] **Step 10: Implement AutostartManager**

`src/WidgX/Startup/AutostartManager.cs`:

```csharp
using Microsoft.Win32;

namespace WidgX.Startup;

public static class AutostartManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string DefaultValueName = "WidgX";

    public static bool IsEnabled(string valueName = DefaultValueName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(valueName) != null;
    }

    public static void SetEnabled(bool enabled, string valueName = DefaultValueName, string? exePathOverride = null)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                         ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (enabled)
        {
            var exePath = exePathOverride ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
            key.SetValue(valueName, $"\"{exePath}\" --background");
        }
        else
        {
            key.DeleteValue(valueName, throwOnMissingValue: false);
        }
    }
}
```

- [ ] **Step 11: Run test to verify it passes**

Run: `dotnet test --filter AutostartManagerTests`
Expected: PASS

- [ ] **Step 12: Add a Settings tab to the Designer (location + autostart)**

Modify `src/WidgX/Designer/DesignerWindow.xaml` — wrap the existing three-column `Grid` (from Phase 2 Task 6) in a `TabControl` with two tabs, "Layout" and "Settings":

```xml
<TabControl>
    <TabItem Header="Layout">
        <Grid Margin="8">
            <!-- existing palette / canvas / properties columns from Phase 2 go here unchanged -->
        </Grid>
    </TabItem>
    <TabItem Header="Settings">
        <StackPanel Margin="16">
            <TextBlock Text="Weather location (city name)" FontWeight="Bold" />
            <TextBox x:Name="WeatherLocationBox" Width="220" Margin="0,4,0,12" />
            <Button x:Name="SaveLocationButton" Content="Save location" Width="120" Click="OnSaveLocation" HorizontalAlignment="Left" Margin="0,0,0,16" />

            <CheckBox x:Name="AutostartCheckBox" Content="Run WidgX when Windows starts" Checked="OnAutostartChanged" Unchecked="OnAutostartChanged" />
        </StackPanel>
    </TabItem>
</TabControl>
```

- [ ] **Step 13: Wire the Settings tab in the code-behind**

Modify `src/WidgX/Designer/DesignerWindow.xaml.cs` — add to the constructor (after existing setup) and add the two new handlers:

```csharp
// In the constructor, after WidgetPalette.ItemsSource = WidgetRegistry.All;
var settings = Persistence.SettingsStore.Load(Persistence.AppPaths.SettingsFilePath);
WeatherLocationBox.Text = settings.WeatherLocationName;
AutostartCheckBox.IsChecked = Startup.AutostartManager.IsEnabled();
```

```csharp
private async void OnSaveLocation(object sender, RoutedEventArgs e)
{
    var locationName = WeatherLocationBox.Text;
    if (string.IsNullOrWhiteSpace(locationName)) return;

    var weatherService = new Widgets.Weather.WeatherService();
    var coords = await weatherService.GeocodeAsync(locationName);

    var settings = Persistence.SettingsStore.Load(Persistence.AppPaths.SettingsFilePath);
    settings.WeatherLocationName = locationName;
    if (coords != null)
    {
        settings.WeatherLatitude = coords.Value.Latitude;
        settings.WeatherLongitude = coords.Value.Longitude;
    }
    Persistence.SettingsStore.Save(Persistence.AppPaths.SettingsFilePath, settings);
}

private void OnAutostartChanged(object sender, RoutedEventArgs e)
{
    Startup.AutostartManager.SetEnabled(AutostartCheckBox.IsChecked == true);
}
```

- [ ] **Step 14: Register the Weather widget at startup alongside the others**

Modify `src/WidgX/App.xaml.cs` — add, alongside the existing `*.Register()` calls:

```csharp
WidgX.Widgets.Todos.TodoWidgetRegistration.Register();
WidgX.Widgets.Uptime.UptimeWidgetRegistration.Register();
WidgX.Widgets.SystemStats.SystemStatsWidgetRegistration.Register();
WidgX.Widgets.NowPlaying.NowPlayingWidgetRegistration.Register();
WidgX.Widgets.Weather.WeatherWidgetRegistration.Register();
```

- [ ] **Step 15: Manual verification**

Run the app, open the Designer's "Settings" tab, enter a real city name, click "Save location" — confirm no error. Add a Weather widget to the layout, save, and confirm the overlay shows a current temperature/condition and a 5-day forecast within a few seconds. Toggle the autostart checkbox on, then check `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (via `regedit` or `Get-ItemProperty`) for a `WidgX` value pointing at the exe with `--background`; toggle it off and confirm the value disappears.

- [ ] **Step 16: Run the full test suite**

Run: `dotnet test`
Expected: all tests pass.

- [ ] **Step 17: Commit**

```bash
git add src/WidgX/Widgets/Weather src/WidgX/Startup src/WidgX/Designer src/WidgX/App.xaml.cs tests/WidgX.Tests/Widgets/Weather tests/WidgX.Tests/Startup
git commit -m "Add WeatherWidget (Open-Meteo), AutostartManager, and Designer Settings tab"
```

---

## Phase 3 Exit Criteria

- All ten widget types from the original spec (Calendar, Clock, Todos, Now Playing, CPU/RAM/GPU, Uptime, Weather+forecast) are implemented, registered, and placeable via the Designer.
- Weather location and autostart are configurable from the Designer's Settings tab and persist correctly.
- `dotnet test` passes for all new pure-logic tests (sorting, formatting, RAM/GPU calculations, weather code mapping, autostart round-trip).
- Manual verification confirms each OS-dependent widget (system stats, now playing, weather, autostart) behaves correctly against the real OS/network.
