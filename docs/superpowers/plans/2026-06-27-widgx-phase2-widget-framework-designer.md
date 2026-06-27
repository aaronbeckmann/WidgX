# WidgX Phase 2: Widget Framework, Designer Window, Clock & Calendar — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Depends on:** `docs/superpowers/plans/2026-06-27-widgx-phase1-core-overlay.md` must be complete and merged before starting this plan.

**Goal:** Introduce the `IWidget` plugin contract and `WidgetRegistry`, implement the Clock and Calendar widgets end-to-end, build the `WidgetHost` that renders configured widgets onto the live overlay (feeding their bounds into the Phase 1 click-through hit-tester), and build the Designer window (screen picker, canvas, palette, properties panel, save/discard) so widgets can actually be placed and persisted.

**Architecture:** `IWidget` is a small contract every widget type implements. `WidgetRegistry` is a static lookup from type name to a factory + default config. `WidgetHost` is owned by `OverlayWindow`; it instantiates `IWidget`s from the current `Layout`, places their `View` on the overlay's `Canvas`, and reports their screen-space bounds back to the click-through hit-tester. The Designer window is a separate, normal (non-transparent) `Window` that edits a `Layout` in memory and writes it via `LayoutStore` on Save, then asks the running `OverlayWindow` to reload.

**Tech Stack:** .NET 8, WPF (C#), xUnit. Builds directly on Phase 1's `Models`, `Persistence`, `Overlay`, and `Native` namespaces.

## Global Constraints

(Same as Phase 1, plus:)
- Adding a new widget type must require no changes to `OverlayWindow`, `WidgetHost`, or `DesignerWindow` — only a new `WidgetTypeDefinition` registration.
- Each widget refreshes itself independently (its own timer) — no shared global tick that couples widgets together.
- Designer Save must hot-reload the running overlay without restarting the process.
- Designer Discard must not mutate the on-disk layout.

---

### Task 1: IWidget contract and WidgetRegistry

**Files:**
- Create: `src/WidgX/Widgets/IWidget.cs`
- Create: `src/WidgX/Widgets/WidgetTypeDefinition.cs`
- Create: `src/WidgX/Widgets/WidgetRegistry.cs`
- Test: `tests/WidgX.Tests/Widgets/WidgetRegistryTests.cs`

**Interfaces:**
- Produces:
  - `IWidget { UserControl View { get; } void Configure(WidgetInstance config); void StartUpdates(); void StopUpdates(); }`
  - `WidgetTypeDefinition { string TypeName; string DisplayName; Func<IWidget> CreateWidget; Func<WidgetInstance> CreateDefaultInstance; }`
  - `WidgetRegistry.Register(WidgetTypeDefinition definition) : void`
  - `WidgetRegistry.Get(string typeName) : WidgetTypeDefinition` (throws `KeyNotFoundException` if unknown)
  - `WidgetRegistry.All : IReadOnlyList<WidgetTypeDefinition>`
  - `WidgetRegistry.Clear() : void` (test-only reset hook, since the registry is a static singleton)

- [ ] **Step 1: Write the failing tests**

`tests/WidgX.Tests/Widgets/WidgetRegistryTests.cs`:

```csharp
using System;
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

    public void Dispose()
    {
        WidgetRegistry.Clear();
    }
}
```

(Add `using System.Collections.Generic;` to the top alongside the existing usings for `KeyNotFoundException` and `IReadOnlyList`.)

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter WidgetRegistryTests`
Expected: FAIL (compile error — `WidgX.Widgets` does not exist yet)

- [ ] **Step 3: Implement IWidget**

`src/WidgX/Widgets/IWidget.cs`:

```csharp
using System.Windows.Controls;
using WidgX.Models;

namespace WidgX.Widgets;

public interface IWidget
{
    UserControl View { get; }
    void Configure(WidgetInstance config);
    void StartUpdates();
    void StopUpdates();
}
```

- [ ] **Step 4: Implement WidgetTypeDefinition**

`src/WidgX/Widgets/WidgetTypeDefinition.cs`:

```csharp
using System;
using WidgX.Models;

namespace WidgX.Widgets;

public class WidgetTypeDefinition
{
    public string TypeName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Func<IWidget> CreateWidget { get; set; } = () => throw new InvalidOperationException();
    public Func<WidgetInstance> CreateDefaultInstance { get; set; } = () => new WidgetInstance();
}
```

- [ ] **Step 5: Implement WidgetRegistry**

`src/WidgX/Widgets/WidgetRegistry.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;

namespace WidgX.Widgets;

public static class WidgetRegistry
{
    private static readonly Dictionary<string, WidgetTypeDefinition> Definitions = new();

    public static void Register(WidgetTypeDefinition definition)
    {
        Definitions[definition.TypeName] = definition;
    }

    public static WidgetTypeDefinition Get(string typeName)
    {
        return Definitions[typeName];
    }

    public static IReadOnlyList<WidgetTypeDefinition> All => Definitions.Values.ToList();

    public static void Clear()
    {
        Definitions.Clear();
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test --filter WidgetRegistryTests`
Expected: PASS (3 tests)

- [ ] **Step 7: Commit**

```bash
git add src/WidgX/Widgets tests/WidgX.Tests/Widgets
git commit -m "Add IWidget contract and WidgetRegistry"
```

---

### Task 2: ClockWidget

**Files:**
- Create: `src/WidgX/Widgets/Clock/ClockWidget.xaml`
- Create: `src/WidgX/Widgets/Clock/ClockWidget.xaml.cs`
- Create: `src/WidgX/Widgets/Clock/ClockWidgetRegistration.cs`
- Test: `tests/WidgX.Tests/Widgets/Clock/ClockWidgetFormattingTests.cs`

**Interfaces:**
- Consumes: `IWidget`, `WidgetInstance`, `WidgetRegistry.Register`.
- Produces:
  - `ClockWidget : UserControl, IWidget`
  - `ClockWidget.FormatDisplay(DateTime now) : (string Time, string DayOfWeek, string Date)` — static pure formatting method, unit-testable without a live clock.
  - `ClockWidgetRegistration.Register() : void` — registers `"Clock"` in `WidgetRegistry`.

- [ ] **Step 1: Write the failing formatting test**

`tests/WidgX.Tests/Widgets/Clock/ClockWidgetFormattingTests.cs`:

```csharp
using System;
using WidgX.Widgets.Clock;
using Xunit;

namespace WidgX.Tests.Widgets.Clock;

public class ClockWidgetFormattingTests
{
    [Fact]
    public void FormatDisplay_Produces_Time_Day_And_Date()
    {
        var now = new DateTime(2026, 6, 27, 14, 5, 0); // Saturday

        var (time, dayOfWeek, date) = ClockWidget.FormatDisplay(now);

        Assert.Equal("14:05", time);
        Assert.Equal("Saturday", dayOfWeek);
        Assert.Equal("2026-06-27", date);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter ClockWidgetFormattingTests`
Expected: FAIL (compile error — `WidgX.Widgets.Clock` does not exist yet)

- [ ] **Step 3: Write the XAML view**

`src/WidgX/Widgets/Clock/ClockWidget.xaml`:

```xml
<UserControl x:Class="WidgX.Widgets.Clock.ClockWidget"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Border Background="#80000000" CornerRadius="6" Padding="10">
        <StackPanel>
            <TextBlock x:Name="TimeText" FontSize="28" Foreground="White" HorizontalAlignment="Center" />
            <TextBlock x:Name="DayOfWeekText" FontSize="14" Foreground="White" HorizontalAlignment="Center" />
            <TextBlock x:Name="DateText" FontSize="14" Foreground="White" HorizontalAlignment="Center" />
        </StackPanel>
    </Border>
</UserControl>
```

- [ ] **Step 4: Implement the code-behind**

`src/WidgX/Widgets/Clock/ClockWidget.xaml.cs`:

```csharp
using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WidgX.Models;

namespace WidgX.Widgets.Clock;

public partial class ClockWidget : UserControl, IWidget
{
    private readonly DispatcherTimer _timer;

    public ClockWidget()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Refresh();
    }

    public UserControl View => this;

    public static (string Time, string DayOfWeek, string Date) FormatDisplay(DateTime now)
    {
        return (
            now.ToString("HH:mm"),
            now.ToString("dddd"),
            now.ToString("yyyy-MM-dd")
        );
    }

    public void Configure(WidgetInstance config)
    {
        Width = config.Width;
        Height = config.Height;
        Opacity = config.Opacity;
        FontSize = config.FontSize;

        if (ColorConverter.ConvertFromString(config.AccentColorHex) is Color color)
        {
            TimeText.Foreground = new SolidColorBrush(color);
            DayOfWeekText.Foreground = new SolidColorBrush(color);
            DateText.Foreground = new SolidColorBrush(color);
        }

        Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates() => _timer.Stop();

    private void Refresh()
    {
        var (time, dayOfWeek, date) = FormatDisplay(DateTime.Now);
        TimeText.Text = time;
        DayOfWeekText.Text = dayOfWeek;
        DateText.Text = date;
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test --filter ClockWidgetFormattingTests`
Expected: PASS

- [ ] **Step 6: Implement and call the registration helper**

`src/WidgX/Widgets/Clock/ClockWidgetRegistration.cs`:

```csharp
namespace WidgX.Widgets.Clock;

public static class ClockWidgetRegistration
{
    public static void Register()
    {
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "Clock",
            DisplayName = "Clock",
            CreateWidget = () => new ClockWidget(),
            CreateDefaultInstance = () => new Models.WidgetInstance
            {
                WidgetType = "Clock",
                Width = 220,
                Height = 110,
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
git add src/WidgX/Widgets/Clock tests/WidgX.Tests/Widgets/Clock
git commit -m "Add ClockWidget with testable formatting logic"
```

---

### Task 3: CalendarWidget

**Files:**
- Create: `src/WidgX/Widgets/Calendar/CalendarWidget.xaml`
- Create: `src/WidgX/Widgets/Calendar/CalendarWidget.xaml.cs`
- Create: `src/WidgX/Widgets/Calendar/CalendarWidgetRegistration.cs`
- Test: `tests/WidgX.Tests/Widgets/Calendar/CalendarWidgetLogicTests.cs`

**Interfaces:**
- Consumes: same as Task 2.
- Produces:
  - `CalendarWidget : UserControl, IWidget`
  - `CalendarWidget.BuildMonthGrid(DateTime today) : List<List<int?>>` — pure logic returning a 6-row x 7-col grid of day numbers (`null` for blank leading/trailing cells), unit-testable.
  - `CalendarWidgetRegistration.Register() : void`

- [ ] **Step 1: Write the failing grid-logic test**

`tests/WidgX.Tests/Widgets/Calendar/CalendarWidgetLogicTests.cs`:

```csharp
using System;
using WidgX.Widgets.Calendar;
using Xunit;

namespace WidgX.Tests.Widgets.Calendar;

public class CalendarWidgetLogicTests
{
    [Fact]
    public void BuildMonthGrid_Places_First_Day_On_Correct_Weekday()
    {
        // June 2026: June 1 is a Monday.
        var grid = CalendarWidget.BuildMonthGrid(new DateTime(2026, 6, 15));

        // Row 0 should contain June 1 at the Monday column (index 1, since
        // the grid's columns are Sun..Sat, index 0..6).
        Assert.Equal(1, grid[0][1]);
        Assert.Null(grid[0][0]); // Sunday before June 1 is blank
    }

    [Fact]
    public void BuildMonthGrid_Contains_Last_Day_Of_Month()
    {
        var grid = CalendarWidget.BuildMonthGrid(new DateTime(2026, 6, 15));

        var flatDays = grid.SelectMany(row => row).Where(d => d.HasValue).Select(d => d!.Value);
        Assert.Contains(30, flatDays); // June has 30 days
        Assert.DoesNotContain(31, flatDays);
    }
}
```

(Add `using System.Linq;` to the usings.)

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter CalendarWidgetLogicTests`
Expected: FAIL (compile error)

- [ ] **Step 3: Write the XAML view**

`src/WidgX/Widgets/Calendar/CalendarWidget.xaml`:

```xml
<UserControl x:Class="WidgX.Widgets.Calendar.CalendarWidget"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Border Background="#80000000" CornerRadius="6" Padding="10">
        <StackPanel>
            <TextBlock x:Name="MonthText" FontSize="16" FontWeight="Bold" Foreground="White" HorizontalAlignment="Center" Margin="0,0,0,6" />
            <Grid x:Name="DaysGrid" />
        </StackPanel>
    </Border>
</UserControl>
```

- [ ] **Step 4: Implement the code-behind**

`src/WidgX/Widgets/Calendar/CalendarWidget.xaml.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WidgX.Models;

namespace WidgX.Widgets.Calendar;

public partial class CalendarWidget : UserControl, IWidget
{
    private readonly DispatcherTimer _timer;
    private Color _accentColor = Colors.White;

    public CalendarWidget()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromHours(1) };
        _timer.Tick += (_, _) => Refresh();
    }

    public UserControl View => this;

    public static List<List<int?>> BuildMonthGrid(DateTime today)
    {
        var firstOfMonth = new DateTime(today.Year, today.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var startColumn = (int)firstOfMonth.DayOfWeek; // Sunday = 0

        var grid = new List<List<int?>>();
        var day = 1;

        for (var row = 0; row < 6 && day <= daysInMonth; row++)
        {
            var rowCells = new List<int?>();
            for (var col = 0; col < 7; col++)
            {
                if (row == 0 && col < startColumn)
                {
                    rowCells.Add(null);
                }
                else if (day <= daysInMonth)
                {
                    rowCells.Add(day);
                    day++;
                }
                else
                {
                    rowCells.Add(null);
                }
            }
            grid.Add(rowCells);
        }

        return grid;
    }

    public void Configure(WidgetInstance config)
    {
        Width = config.Width;
        Height = config.Height;
        Opacity = config.Opacity;
        FontSize = config.FontSize;

        if (ColorConverter.ConvertFromString(config.AccentColorHex) is Color color)
        {
            _accentColor = color;
        }

        Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates() => _timer.Stop();

    private void Refresh()
    {
        var today = DateTime.Now;
        MonthText.Text = today.ToString("MMMM yyyy");
        MonthText.Foreground = new SolidColorBrush(_accentColor);

        var grid = BuildMonthGrid(today);

        DaysGrid.Children.Clear();
        DaysGrid.RowDefinitions.Clear();
        DaysGrid.ColumnDefinitions.Clear();

        for (var c = 0; c < 7; c++) DaysGrid.ColumnDefinitions.Add(new ColumnDefinition());
        for (var r = 0; r < grid.Count; r++) DaysGrid.RowDefinitions.Add(new RowDefinition());

        for (var r = 0; r < grid.Count; r++)
        {
            for (var c = 0; c < 7; c++)
            {
                var dayValue = grid[r][c];
                var text = new TextBlock
                {
                    Text = dayValue?.ToString() ?? string.Empty,
                    Foreground = new SolidColorBrush(_accentColor),
                    FontWeight = dayValue == today.Day ? FontWeights.Bold : FontWeights.Normal,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                };
                Grid.SetRow(text, r);
                Grid.SetColumn(text, c);
                DaysGrid.Children.Add(text);
            }
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter CalendarWidgetLogicTests`
Expected: PASS (2 tests)

- [ ] **Step 6: Implement the registration helper**

`src/WidgX/Widgets/Calendar/CalendarWidgetRegistration.cs`:

```csharp
namespace WidgX.Widgets.Calendar;

public static class CalendarWidgetRegistration
{
    public static void Register()
    {
        WidgetRegistry.Register(new WidgetTypeDefinition
        {
            TypeName = "Calendar",
            DisplayName = "Calendar",
            CreateWidget = () => new CalendarWidget(),
            CreateDefaultInstance = () => new Models.WidgetInstance
            {
                WidgetType = "Calendar",
                Width = 260,
                Height = 220,
                Opacity = 0.9,
                AccentColorHex = "#FFFFFF",
                FontSize = 14
            }
        });
    }
}
```

- [ ] **Step 7: Register both widgets at app startup**

Modify `src/WidgX/App.xaml.cs` — add at the top of `OnStartup`, before loading the layout:

```csharp
WidgX.Widgets.Clock.ClockWidgetRegistration.Register();
WidgX.Widgets.Calendar.CalendarWidgetRegistration.Register();
```

- [ ] **Step 8: Commit**

```bash
git add src/WidgX/Widgets/Calendar tests/WidgX.Tests/Widgets/Calendar src/WidgX/App.xaml.cs
git commit -m "Add CalendarWidget with testable month-grid logic; register widgets at startup"
```

---

### Task 4: WidgetHost — rendering configured widgets onto the overlay

**Files:**
- Create: `src/WidgX/Overlay/WidgetHost.cs`
- Modify: `src/WidgX/Overlay/OverlayWindow.xaml`
- Modify: `src/WidgX/Overlay/OverlayWindow.xaml.cs`
- Test: `tests/WidgX.Tests/Overlay/WidgetHostTests.cs`

**Interfaces:**
- Consumes: `WidgetRegistry.Get`, `IWidget`, `WidgetInstance`, `Layout`.
- Produces:
  - `WidgetHost.BuildBounds(Layout layout) : Dictionary<string, System.Windows.Rect>` — pure function mapping each widget instance's `Id` to its `Rect` (X, Y, Width, Height), used both for rendering and hit-testing. Testable without WPF visuals.
  - `WidgetHost.LoadLayout(Layout layout, Canvas hostCanvas) : List<Rect>` — instantiates each widget via `WidgetRegistry`, configures it, adds its `View` to `hostCanvas` at the right `Canvas.Left/Top`, starts its updates, and returns the list of bounds (for the overlay to feed into `SetWidgetBounds`). Disposes/stops any previously hosted widgets first.

- [ ] **Step 1: Write the failing test for BuildBounds**

`tests/WidgX.Tests/Overlay/WidgetHostTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter WidgetHostTests`
Expected: FAIL (compile error — `WidgetHost` does not exist yet)

- [ ] **Step 3: Implement WidgetHost**

`src/WidgX/Overlay/WidgetHost.cs`:

```csharp
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WidgX.Models;
using WidgX.Widgets;

namespace WidgX.Overlay;

public class WidgetHost
{
    private readonly List<IWidget> _activeWidgets = new();

    public static Dictionary<string, Rect> BuildBounds(Layout layout)
    {
        var result = new Dictionary<string, Rect>();
        foreach (var widget in layout.Widgets)
        {
            result[widget.Id] = new Rect(widget.X, widget.Y, widget.Width, widget.Height);
        }
        return result;
    }

    public List<Rect> LoadLayout(Layout layout, Canvas hostCanvas)
    {
        foreach (var widget in _activeWidgets)
        {
            widget.StopUpdates();
        }
        _activeWidgets.Clear();
        hostCanvas.Children.Clear();

        var bounds = new List<Rect>();

        foreach (var instance in layout.Widgets)
        {
            var definition = WidgetRegistry.Get(instance.WidgetType);
            var widget = definition.CreateWidget();
            widget.Configure(instance);

            Canvas.SetLeft(widget.View, instance.X);
            Canvas.SetTop(widget.View, instance.Y);
            hostCanvas.Children.Add(widget.View);

            widget.StartUpdates();
            _activeWidgets.Add(widget);

            bounds.Add(new Rect(instance.X, instance.Y, instance.Width, instance.Height));
        }

        return bounds;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter WidgetHostTests`
Expected: PASS

- [ ] **Step 5: Give OverlayWindow a Canvas and a reload method**

Modify `src/WidgX/Overlay/OverlayWindow.xaml` — replace the `Grid` root with a `Canvas`:

```xml
<Window x:Class="WidgX.Overlay.OverlayWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        ShowInTaskbar="False"
        ResizeMode="NoResize"
        Topmost="False">
    <Canvas x:Name="RootCanvas" Background="Transparent" />
</Window>
```

- [ ] **Step 6: Wire WidgetHost into OverlayWindow**

Modify `src/WidgX/Overlay/OverlayWindow.xaml.cs` — add a `WidgetHost` field and a public `ReloadLayout` method:

```csharp
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using WidgX.Models;
using WidgX.Native;

namespace WidgX.Overlay;

public partial class OverlayWindow : Window
{
    private List<Rect> _widgetBounds = new();
    private HwndSource? _hwndSource;
    private readonly WidgetHost _widgetHost = new();

    public OverlayWindow(Rect screenBoundsDip)
    {
        InitializeComponent();

        Left = screenBoundsDip.X;
        Top = screenBoundsDip.Y;
        Width = screenBoundsDip.Width;
        Height = screenBoundsDip.Height;

        SourceInitialized += OnSourceInitialized;
    }

    public void SetWidgetBounds(IEnumerable<Rect> bounds)
    {
        _widgetBounds = new List<Rect>(bounds);
    }

    public void ReloadLayout(Layout layout)
    {
        var bounds = _widgetHost.LoadLayout(layout, RootCanvas);
        SetWidgetBounds(bounds);
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        _hwndSource = HwndSource.FromHwnd(hwnd);
        _hwndSource?.AddHook(WndProc);

        DesktopLayerHelper.SendToDesktopLayer(hwnd);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_NCHITTEST)
        {
            var screenX = unchecked((short)((long)lParam & 0xFFFF));
            var screenY = unchecked((short)(((long)lParam >> 16) & 0xFFFF));
            var pointInWindow = PointFromScreen(new Point(screenX, screenY));

            var isOverWidget = ClickThroughHitTester.HitTest(pointInWindow, _widgetBounds);

            handled = true;
            return isOverWidget ? (IntPtr)NativeMethods.HTCLIENT : (IntPtr)NativeMethods.HTTRANSPARENT;
        }

        return IntPtr.Zero;
    }
}
```

- [ ] **Step 7: Call ReloadLayout from App startup**

Modify `src/WidgX/App.xaml.cs` — after `_overlayWindow.Show();`, add:

```csharp
_overlayWindow.ReloadLayout(layout);
```

- [ ] **Step 8: Run the full test suite**

Run: `dotnet test`
Expected: all tests pass, including the new `WidgetHostTests`.

- [ ] **Step 9: Commit**

```bash
git add src/WidgX/Overlay tests/WidgX.Tests/Overlay src/WidgX/App.xaml.cs
git commit -m "Add WidgetHost; overlay now renders configured widgets and reports their bounds for click-through"
```

---

### Task 5: Designer window shell — screen picker and canvas

**Files:**
- Create: `src/WidgX/Designer/DesignerWindow.xaml`
- Create: `src/WidgX/Designer/DesignerWindow.xaml.cs`
- Create: `src/WidgX/Designer/DesignerWidgetBox.xaml`
- Create: `src/WidgX/Designer/DesignerWidgetBox.xaml.cs`

**Interfaces:**
- Consumes: `ScreenResolver.GetAllScreens`, `WidgetRegistry.All`, `WidgetHost` (for live-preview rendering — the canvas hosts real `IWidget.View` instances, just scaled down), `Layout`/`WidgetInstance`.
- Produces:
  - `DesignerWindow(Layout initialLayout, Action<Layout> onSaved)` — constructor; `onSaved` is invoked with the new `Layout` when the user clicks Save.
  - `DesignerWidgetBox(WidgetInstance instance, IWidget widget)` — a draggable/resizable wrapper around a live widget preview; raises `event Action<WidgetInstance> BoundsChanged` whenever the user drags/resizes it, and `event Action<WidgetInstance> Selected` when clicked.

- [ ] **Step 1: Implement DesignerWidgetBox — drag to move, corner thumb to resize**

`src/WidgX/Designer/DesignerWidgetBox.xaml`:

```xml
<UserControl x:Class="WidgX.Designer.DesignerWidgetBox"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Border x:Name="OuterBorder" BorderBrush="Cyan" BorderThickness="1" Background="Transparent">
        <Grid>
            <ContentPresenter x:Name="WidgetContent" />
            <Thumb x:Name="ResizeThumb" Width="12" Height="12"
                   HorizontalAlignment="Right" VerticalAlignment="Bottom"
                   Background="Cyan" Cursor="SizeNWSE" />
        </Grid>
    </Border>
</UserControl>
```

- [ ] **Step 2: Implement the code-behind**

`src/WidgX/Designer/DesignerWidgetBox.xaml.cs`:

```csharp
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WidgX.Models;
using WidgX.Widgets;

namespace WidgX.Designer;

public partial class DesignerWidgetBox : UserControl
{
    private readonly WidgetInstance _instance;
    private Point _dragStartMouse;
    private Point _dragStartPosition;
    private bool _isDragging;

    public event Action<WidgetInstance>? BoundsChanged;
    public event Action<WidgetInstance>? Selected;

    public DesignerWidgetBox(WidgetInstance instance, IWidget widget)
    {
        InitializeComponent();
        _instance = instance;

        WidgetContent.Content = widget.View;
        Width = instance.Width;
        Height = instance.Height;

        Canvas.SetLeft(this, instance.X);
        Canvas.SetTop(this, instance.Y);

        OuterBorder.MouseLeftButtonDown += OnMouseDown;
        OuterBorder.MouseMove += OnMouseMove;
        OuterBorder.MouseLeftButtonUp += OnMouseUp;
        ResizeThumb.DragDelta += OnResizeDrag;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _dragStartMouse = e.GetPosition(Parent as IInputElement);
        _dragStartPosition = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
        OuterBorder.CaptureMouse();
        Selected?.Invoke(_instance);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        var current = e.GetPosition(Parent as IInputElement);
        var delta = current - _dragStartMouse;

        var newX = _dragStartPosition.X + delta.X;
        var newY = _dragStartPosition.Y + delta.Y;

        Canvas.SetLeft(this, newX);
        Canvas.SetTop(this, newY);

        _instance.X = newX;
        _instance.Y = newY;
        BoundsChanged?.Invoke(_instance);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        OuterBorder.ReleaseMouseCapture();
    }

    private void OnResizeDrag(object sender, DragDeltaEventArgs e)
    {
        var newWidth = Math.Max(40, Width + e.HorizontalChange);
        var newHeight = Math.Max(40, Height + e.VerticalChange);

        Width = newWidth;
        Height = newHeight;

        _instance.Width = newWidth;
        _instance.Height = newHeight;
        BoundsChanged?.Invoke(_instance);
    }
}
```

- [ ] **Step 3: Implement the DesignerWindow shell (screen picker + canvas, no palette/properties yet)**

`src/WidgX/Designer/DesignerWindow.xaml`:

```xml
<Window x:Class="WidgX.Designer.DesignerWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="WidgX Designer" Width="900" Height="650"
        WindowStartupLocation="CenterScreen">
    <DockPanel>
        <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="8">
            <TextBlock Text="Screen:" VerticalAlignment="Center" Margin="0,0,6,0" />
            <ComboBox x:Name="ScreenPicker" Width="220" DisplayMemberPath="FriendlyName"
                      SelectionChanged="OnScreenChanged" />
        </StackPanel>
        <Border DockPanel.Dock="Bottom" Padding="8" Background="#EEEEEE">
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                <Button x:Name="DiscardButton" Content="Discard" Width="80" Margin="0,0,8,0" Click="OnDiscard" />
                <Button x:Name="SaveButton" Content="Save" Width="80" Click="OnSave" />
            </StackPanel>
        </Border>
        <Border Background="#222222" Margin="8">
            <Canvas x:Name="DesignCanvas" Background="#222222" ClipToBounds="True" />
        </Border>
    </DockPanel>
</Window>
```

- [ ] **Step 4: Implement the code-behind (screen picker + canvas scaling, Save/Discard stubs)**

`src/WidgX/Designer/DesignerWindow.xaml.cs`:

```csharp
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WidgX.Models;
using WidgX.Overlay;

namespace WidgX.Designer;

public partial class DesignerWindow : Window
{
    private readonly Layout _workingLayout;
    private readonly Action<Layout> _onSaved;
    private ScreenInfo _selectedScreen;

    public DesignerWindow(Layout initialLayout, Action<Layout> onSaved)
    {
        InitializeComponent();

        // Deep-copy so Discard can throw away in-progress edits.
        _workingLayout = new Layout
        {
            SelectedScreenId = initialLayout.SelectedScreenId,
            Widgets = initialLayout.Widgets
                .Select(w => new WidgetInstance
                {
                    Id = w.Id, WidgetType = w.WidgetType, X = w.X, Y = w.Y,
                    Width = w.Width, Height = w.Height, Opacity = w.Opacity,
                    AccentColorHex = w.AccentColorHex, FontSize = w.FontSize,
                    Settings = new System.Collections.Generic.Dictionary<string, string>(w.Settings)
                })
                .ToList()
        };
        _onSaved = onSaved;

        var screens = ScreenResolver.GetAllScreens();
        ScreenPicker.ItemsSource = screens;
        _selectedScreen = ScreenResolver.ResolveSelected(_workingLayout.SelectedScreenId, screens);
        ScreenPicker.SelectedItem = screens.FirstOrDefault(s => s.Id == _selectedScreen.Id);

        RebuildCanvas();
    }

    private void OnScreenChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ScreenPicker.SelectedItem is ScreenInfo screen)
        {
            _selectedScreen = screen;
            _workingLayout.SelectedScreenId = screen.Id;
            RebuildCanvas();
        }
    }

    private double CanvasScale => DesignCanvas.ActualWidth > 0 && _selectedScreen.BoundsDip.Width > 0
        ? Math.Min(DesignCanvas.ActualWidth / _selectedScreen.BoundsDip.Width, DesignCanvas.ActualHeight / _selectedScreen.BoundsDip.Height)
        : 1.0;

    private void RebuildCanvas()
    {
        DesignCanvas.Children.Clear();

        foreach (var instance in _workingLayout.Widgets)
        {
            var definition = Widgets.WidgetRegistry.Get(instance.WidgetType);
            var widget = definition.CreateWidget();
            widget.Configure(instance);

            var box = new DesignerWidgetBox(instance, widget);
            box.BoundsChanged += _ => { /* properties panel sync wired in Task 6 */ };
            box.Selected += _ => { /* properties panel wired in Task 6 */ };

            DesignCanvas.Children.Add(box);
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _onSaved(_workingLayout);
        Close();
    }

    private void OnDiscard(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
```

- [ ] **Step 5: Build and manually verify the shell**

Run: `dotnet build src/WidgX/WidgX.csproj`
Expected: `Build succeeded.`

Manual check (this window has no caller yet, so verify via a temporary `Show()` call added to `App.OnStartup` for this check only, then remove it): the screen picker lists your monitors, switching it doesn't crash, and any widgets already in `layout.json` render in the canvas (likely off-scale/unscaled at this point — that's expected and fixed in Task 6).

- [ ] **Step 6: Commit**

```bash
git add src/WidgX/Designer
git commit -m "Add DesignerWindow shell with screen picker, canvas, and draggable/resizable DesignerWidgetBox"
```

---

### Task 6: Widget palette and properties panel

**Files:**
- Modify: `src/WidgX/Designer/DesignerWindow.xaml`
- Modify: `src/WidgX/Designer/DesignerWindow.xaml.cs`

**Interfaces:**
- Consumes: `WidgetRegistry.All`, `DesignerWidgetBox.BoundsChanged/Selected`.
- Produces: a fully interactive designer — add widgets from a palette, select a placed widget to edit its common properties (X, Y, Width, Height, Opacity, AccentColorHex, FontSize) live.

- [ ] **Step 1: Extend the XAML with a left palette panel and a right properties panel**

Replace the `<Border Background="#222222" Margin="8">` block in `src/WidgX/Designer/DesignerWindow.xaml` with a three-column layout:

```xml
<Grid Margin="8">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="160" />
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="220" />
    </Grid.ColumnDefinitions>

    <StackPanel Grid.Column="0" Margin="0,0,8,0">
        <TextBlock Text="Widgets" FontWeight="Bold" Margin="0,0,0,6" />
        <ListBox x:Name="WidgetPalette" DisplayMemberPath="DisplayName" />
        <Button x:Name="AddWidgetButton" Content="Add to canvas" Margin="0,6,0,0" Click="OnAddWidget" />
    </StackPanel>

    <Border Grid.Column="1" Background="#222222">
        <Canvas x:Name="DesignCanvas" Background="#222222" ClipToBounds="True" />
    </Border>

    <StackPanel Grid.Column="2" Margin="8,0,0,0" x:Name="PropertiesPanel" IsEnabled="False">
        <TextBlock Text="Properties" FontWeight="Bold" Margin="0,0,0,6" />
        <TextBlock Text="X" /><TextBox x:Name="XBox" />
        <TextBlock Text="Y" Margin="0,6,0,0" /><TextBox x:Name="YBox" />
        <TextBlock Text="Width" Margin="0,6,0,0" /><TextBox x:Name="WidthBox" />
        <TextBlock Text="Height" Margin="0,6,0,0" /><TextBox x:Name="HeightBox" />
        <TextBlock Text="Opacity (0-1)" Margin="0,6,0,0" /><TextBox x:Name="OpacityBox" />
        <TextBlock Text="Accent Color (hex)" Margin="0,6,0,0" /><TextBox x:Name="AccentColorBox" />
        <TextBlock Text="Font Size" Margin="0,6,0,0" /><TextBox x:Name="FontSizeBox" />
        <Button x:Name="ApplyPropertiesButton" Content="Apply" Margin="0,10,0,0" Click="OnApplyProperties" />
    </StackPanel>
</Grid>
```

- [ ] **Step 2: Wire palette, selection, and properties in the code-behind**

Modify `src/WidgX/Designer/DesignerWindow.xaml.cs` — add a field for the currently selected instance, populate the palette in the constructor, and implement the new handlers:

```csharp
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WidgX.Models;
using WidgX.Overlay;
using WidgX.Widgets;

namespace WidgX.Designer;

public partial class DesignerWindow : Window
{
    private readonly Layout _workingLayout;
    private readonly Action<Layout> _onSaved;
    private ScreenInfo _selectedScreen;
    private WidgetInstance? _selectedInstance;

    public DesignerWindow(Layout initialLayout, Action<Layout> onSaved)
    {
        InitializeComponent();

        _workingLayout = new Layout
        {
            SelectedScreenId = initialLayout.SelectedScreenId,
            Widgets = initialLayout.Widgets
                .Select(w => new WidgetInstance
                {
                    Id = w.Id, WidgetType = w.WidgetType, X = w.X, Y = w.Y,
                    Width = w.Width, Height = w.Height, Opacity = w.Opacity,
                    AccentColorHex = w.AccentColorHex, FontSize = w.FontSize,
                    Settings = new System.Collections.Generic.Dictionary<string, string>(w.Settings)
                })
                .ToList()
        };
        _onSaved = onSaved;

        var screens = ScreenResolver.GetAllScreens();
        ScreenPicker.ItemsSource = screens;
        _selectedScreen = ScreenResolver.ResolveSelected(_workingLayout.SelectedScreenId, screens);
        ScreenPicker.SelectedItem = screens.FirstOrDefault(s => s.Id == _selectedScreen.Id);

        WidgetPalette.ItemsSource = WidgetRegistry.All;

        RebuildCanvas();
    }

    private void OnScreenChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ScreenPicker.SelectedItem is ScreenInfo screen)
        {
            _selectedScreen = screen;
            _workingLayout.SelectedScreenId = screen.Id;
            RebuildCanvas();
        }
    }

    private void RebuildCanvas()
    {
        DesignCanvas.Children.Clear();

        foreach (var instance in _workingLayout.Widgets)
        {
            AddBoxForInstance(instance);
        }
    }

    private void AddBoxForInstance(WidgetInstance instance)
    {
        var definition = WidgetRegistry.Get(instance.WidgetType);
        var widget = definition.CreateWidget();
        widget.Configure(instance);

        var box = new DesignerWidgetBox(instance, widget);
        box.Selected += OnWidgetSelected;
        box.BoundsChanged += _ => { if (_selectedInstance == instance) PopulatePropertiesPanel(instance); };

        DesignCanvas.Children.Add(box);
    }

    private void OnWidgetSelected(WidgetInstance instance)
    {
        _selectedInstance = instance;
        PropertiesPanel.IsEnabled = true;
        PopulatePropertiesPanel(instance);
    }

    private void PopulatePropertiesPanel(WidgetInstance instance)
    {
        XBox.Text = instance.X.ToString("0");
        YBox.Text = instance.Y.ToString("0");
        WidthBox.Text = instance.Width.ToString("0");
        HeightBox.Text = instance.Height.ToString("0");
        OpacityBox.Text = instance.Opacity.ToString("0.00");
        AccentColorBox.Text = instance.AccentColorHex;
        FontSizeBox.Text = instance.FontSize.ToString("0");
    }

    private void OnApplyProperties(object sender, RoutedEventArgs e)
    {
        if (_selectedInstance == null) return;

        if (double.TryParse(XBox.Text, out var x)) _selectedInstance.X = x;
        if (double.TryParse(YBox.Text, out var y)) _selectedInstance.Y = y;
        if (double.TryParse(WidthBox.Text, out var w)) _selectedInstance.Width = w;
        if (double.TryParse(HeightBox.Text, out var h)) _selectedInstance.Height = h;
        if (double.TryParse(OpacityBox.Text, out var o)) _selectedInstance.Opacity = o;
        if (double.TryParse(FontSizeBox.Text, out var fs)) _selectedInstance.FontSize = fs;
        _selectedInstance.AccentColorHex = AccentColorBox.Text;

        RebuildCanvas();
    }

    private void OnAddWidget(object sender, RoutedEventArgs e)
    {
        if (WidgetPalette.SelectedItem is not WidgetTypeDefinition definition) return;

        var instance = definition.CreateDefaultInstance();
        instance.Id = Guid.NewGuid().ToString();
        instance.X = 20;
        instance.Y = 20;

        _workingLayout.Widgets.Add(instance);
        AddBoxForInstance(instance);
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _onSaved(_workingLayout);
        Close();
    }

    private void OnDiscard(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build src/WidgX/WidgX.csproj`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add src/WidgX/Designer
git commit -m "Add widget palette and properties panel to DesignerWindow"
```

---

### Task 7: Wire Designer into the running app (tray "Edit Layout", Save hot-reloads overlay)

**Files:**
- Modify: `src/WidgX/App.xaml.cs`

**Interfaces:**
- Consumes: `DesignerWindow`, `OverlayWindow.ReloadLayout`, `LayoutStore.Save`, `AppPaths.LayoutFilePath`.
- Produces: clicking "Edit Layout" in the tray menu opens the designer pre-loaded with the current layout; Save persists it and immediately updates the live overlay.

- [ ] **Step 1: Replace the Edit Layout stub**

Modify `src/WidgX/App.xaml.cs` — change the `onEditLayout` callback passed to `TrayIconManager`:

```csharp
onEditLayout: () =>
{
    var currentLayout = LayoutStore.Load(AppPaths.LayoutFilePath);
    var designer = new WidgX.Designer.DesignerWindow(currentLayout, savedLayout =>
    {
        LayoutStore.Save(AppPaths.LayoutFilePath, savedLayout);
        _overlayWindow!.ReloadLayout(savedLayout);
    });
    designer.Show();
},
```

- [ ] **Step 2: Manual verification**

Run: `dotnet run --project src/WidgX/WidgX.csproj`

Expected, observed manually:
- Right-click the tray icon → "Edit Layout" opens the designer window.
- The widget palette lists "Clock" and "Calendar"; clicking each then "Add to canvas" places a live-updating preview on the design canvas.
- Dragging a placed widget moves it; dragging its bottom-right corner thumb resizes it; the properties panel populates on selection and "Apply" updates the box after editing a field (e.g. change Opacity and click Apply).
- Clicking "Save" closes the designer; switching to the desktop shows the same widgets now rendered on the real transparent overlay, in the same relative positions, live-updating (clock ticking).
- Clicking on a widget on the live overlay (e.g. nothing currently registers clicks for Clock/Calendar, so just confirm the click does NOT fall through to the desktop underneath — i.e. it's swallowed by the widget's bounds, proving hit-testing works), while clicking empty overlay space still reaches the desktop underneath.
- Reopening "Edit Layout" shows the previously saved widgets still placed correctly (round-trip through `layout.json` confirmed).

- [ ] **Step 3: Commit**

```bash
git add src/WidgX/App.xaml.cs
git commit -m "Wire DesignerWindow into tray Edit Layout action with live overlay hot-reload"
```

---

## Phase 2 Exit Criteria

- `dotnet test` passes for all widget, registry, and widget-host tests.
- From the tray icon, the user can open the Designer, add Clock and Calendar widgets, position/resize them, edit their common properties, and Save.
- Saved widgets render live on the real transparent overlay in the correct position, and the overlay's click-through hit-testing correctly treats their bounds as "hit" (not pass-through).
- Reopening the Designer reflects the previously saved layout.
