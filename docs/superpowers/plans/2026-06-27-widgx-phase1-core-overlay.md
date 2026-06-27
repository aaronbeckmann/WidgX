# WidgX Phase 1: Core Scaffolding, Persistence & Overlay Mechanics — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up the WidgX solution skeleton, the JSON persistence layer, and a borderless transparent overlay window that sits at the desktop layer (above Wallpaper Engine, below normal apps) on the selected monitor, fully click-through, controlled from a system tray icon.

**Architecture:** WPF .NET 8 app. Win32 interop (P/Invoke) handles desktop-layer reparenting (`WorkerW` trick) and per-pixel click-through (`WM_NCHITTEST` hook). Persistence is plain JSON files under `%AppData%\WidgX\` read/written by small store classes. No widgets are rendered yet — this phase proves the overlay mechanics with an empty layout.

**Tech Stack:** .NET 8, WPF (C#), xUnit for unit tests, `System.Text.Json` for persistence, P/Invoke (`user32.dll`) for window mechanics.

## Global Constraints

- Windows-only; .NET 8 / WPF / C#.
- No admin/elevation required anywhere in the app.
- Persistence is JSON under `%AppData%\WidgX\` (`layout.json`, `settings.json`, `todos.json`).
- Overlay must be click-through everywhere except widget bounds, via `WM_NCHITTEST`, not the `WS_EX_TRANSPARENT` extended style.
- Overlay must render above Wallpaper Engine and below normal app windows (desktop layer / `WorkerW` reparenting), not always-on-top.
- Only one overlay/screen active at a time; if the configured screen is disconnected, fall back to the primary monitor.
- Malformed or missing JSON config files must not crash startup — treat as empty/default.

---

### Task 1: Solution and project scaffolding

**Files:**
- Create: `WidgX.sln`
- Create: `src/WidgX/WidgX.csproj`
- Create: `src/WidgX/App.xaml`
- Create: `src/WidgX/App.xaml.cs`
- Create: `src/WidgX/AssemblyInfo.cs`
- Create: `tests/WidgX.Tests/WidgX.Tests.csproj`
- Create: `tests/WidgX.Tests/PlaceholderTests.cs`

**Interfaces:**
- Produces: a buildable `WidgX.sln` containing `WidgX` (WPF exe, `net8.0-windows`) and `WidgX.Tests` (xUnit, referencing `WidgX`).

- [ ] **Step 1: Create the WPF project**

```bash
mkdir -p src/WidgX tests/WidgX.Tests
cd src/WidgX
dotnet new wpf -n WidgX -o . --framework net8.0-windows
```

- [ ] **Step 2: Create the test project and wire references**

```bash
cd ../../tests/WidgX.Tests
dotnet new xunit -n WidgX.Tests -o .
dotnet add reference ../../src/WidgX/WidgX.csproj
```

- [ ] **Step 3: Create the solution and add both projects**

```bash
cd ../..
dotnet new sln -n WidgX
dotnet sln add src/WidgX/WidgX.csproj tests/WidgX.Tests/WidgX.Tests.csproj
```

- [ ] **Step 4: Write a placeholder test to confirm the test project runs**

`tests/WidgX.Tests/PlaceholderTests.cs`:

```csharp
using Xunit;

namespace WidgX.Tests;

public class PlaceholderTests
{
    [Fact]
    public void Solution_Builds_And_Tests_Run()
    {
        Assert.True(true);
    }
}
```

- [ ] **Step 5: Build and run tests**

Run: `dotnet test`
Expected: `Passed!` with 1 test passed.

- [ ] **Step 6: Commit**

```bash
git add WidgX.sln src/WidgX tests/WidgX.Tests
git commit -m "Scaffold WidgX solution with WPF app and xUnit test project"
```

---

### Task 2: Core models

**Files:**
- Create: `src/WidgX/Models/WidgetInstance.cs`
- Create: `src/WidgX/Models/Layout.cs`
- Create: `src/WidgX/Models/AppSettings.cs`
- Create: `src/WidgX/Models/TodoItem.cs`
- Test: `tests/WidgX.Tests/Models/ModelSerializationTests.cs`

**Interfaces:**
- Produces:
  - `WidgX.Models.WidgetInstance { string Id; string WidgetType; double X; double Y; double Width; double Height; double Opacity; string AccentColorHex; double FontSize; Dictionary<string,string> Settings; }`
  - `WidgX.Models.Layout { string SelectedScreenId; List<WidgetInstance> Widgets; }`
  - `WidgX.Models.AppSettings { bool AutostartEnabled; string WeatherLocationName; double? WeatherLatitude; double? WeatherLongitude; }`
  - `WidgX.Models.TodoItem { string Id; string Text; DateTime? DueDate; bool IsCompleted; }`

- [ ] **Step 1: Write the failing serialization test**

`tests/WidgX.Tests/Models/ModelSerializationTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter Layout_RoundTrips_Through_Json`
Expected: FAIL (compile error — `WidgX.Models` namespace does not exist yet)

- [ ] **Step 3: Implement the models**

`src/WidgX/Models/WidgetInstance.cs`:

```csharp
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
    public Dictionary<string, string> Settings { get; set; } = new();
}
```

`src/WidgX/Models/Layout.cs`:

```csharp
using System.Collections.Generic;

namespace WidgX.Models;

public class Layout
{
    public string SelectedScreenId { get; set; } = string.Empty;
    public List<WidgetInstance> Widgets { get; set; } = new();
}
```

`src/WidgX/Models/AppSettings.cs`:

```csharp
namespace WidgX.Models;

public class AppSettings
{
    public bool AutostartEnabled { get; set; }
    public string WeatherLocationName { get; set; } = string.Empty;
    public double? WeatherLatitude { get; set; }
    public double? WeatherLongitude { get; set; }
}
```

`src/WidgX/Models/TodoItem.cs`:

```csharp
using System;

namespace WidgX.Models;

public class TodoItem
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter Layout_RoundTrips_Through_Json`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WidgX/Models tests/WidgX.Tests/Models
git commit -m "Add core persistence models (WidgetInstance, Layout, AppSettings, TodoItem)"
```

---

### Task 3: AppPaths and JSON store base helper

**Files:**
- Create: `src/WidgX/Persistence/AppPaths.cs`
- Create: `src/WidgX/Persistence/JsonFileStore.cs`
- Test: `tests/WidgX.Tests/Persistence/JsonFileStoreTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks except `Models` (any serializable type).
- Produces:
  - `WidgX.Persistence.AppPaths.GetAppDataDirectory() : string` — returns `%AppData%\WidgX`, creating it if missing.
  - `WidgX.Persistence.JsonFileStore.Load<T>(string filePath, T fallback) : T` — returns `fallback` if file is missing or malformed.
  - `WidgX.Persistence.JsonFileStore.Save<T>(string filePath, T value) : void`

- [ ] **Step 1: Write the failing tests**

`tests/WidgX.Tests/Persistence/JsonFileStoreTests.cs`:

```csharp
using System;
using System.IO;
using WidgX.Persistence;
using Xunit;

namespace WidgX.Tests.Persistence;

public class JsonFileStoreTests : IDisposable
{
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");

    [Fact]
    public void Load_Returns_Fallback_When_File_Missing()
    {
        var result = JsonFileStore.Load(_tempFile, new TestPayload { Value = 42 });
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Save_Then_Load_RoundTrips()
    {
        var payload = new TestPayload { Value = 7 };
        JsonFileStore.Save(_tempFile, payload);

        var result = JsonFileStore.Load(_tempFile, new TestPayload { Value = -1 });
        Assert.Equal(7, result.Value);
    }

    [Fact]
    public void Load_Returns_Fallback_When_File_Is_Malformed()
    {
        File.WriteAllText(_tempFile, "{ not valid json");
        var result = JsonFileStore.Load(_tempFile, new TestPayload { Value = 99 });
        Assert.Equal(99, result.Value);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }

    public class TestPayload
    {
        public int Value { get; set; }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter JsonFileStoreTests`
Expected: FAIL (compile error — `WidgX.Persistence` does not exist yet)

- [ ] **Step 3: Implement AppPaths**

`src/WidgX/Persistence/AppPaths.cs`:

```csharp
using System;
using System.IO;

namespace WidgX.Persistence;

public static class AppPaths
{
    public static string GetAppDataDirectory()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WidgX");

        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string LayoutFilePath => Path.Combine(GetAppDataDirectory(), "layout.json");
    public static string SettingsFilePath => Path.Combine(GetAppDataDirectory(), "settings.json");
    public static string TodosFilePath => Path.Combine(GetAppDataDirectory(), "todos.json");
}
```

- [ ] **Step 4: Implement JsonFileStore**

`src/WidgX/Persistence/JsonFileStore.cs`:

```csharp
using System.IO;
using System.Text.Json;

namespace WidgX.Persistence;

public static class JsonFileStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public static T Load<T>(string filePath, T fallback)
    {
        if (!File.Exists(filePath))
        {
            return fallback;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var value = JsonSerializer.Deserialize<T>(json, Options);
            return value ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    public static void Save<T>(string filePath, T value)
    {
        var json = JsonSerializer.Serialize(value, Options);
        File.WriteAllText(filePath, json);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter JsonFileStoreTests`
Expected: PASS (3 tests)

- [ ] **Step 6: Commit**

```bash
git add src/WidgX/Persistence tests/WidgX.Tests/Persistence
git commit -m "Add AppPaths and generic JsonFileStore with fallback-on-malformed behavior"
```

---

### Task 4: LayoutStore, SettingsStore, TodoStore

**Files:**
- Create: `src/WidgX/Persistence/LayoutStore.cs`
- Create: `src/WidgX/Persistence/SettingsStore.cs`
- Create: `src/WidgX/Persistence/TodoStore.cs`
- Test: `tests/WidgX.Tests/Persistence/StoreTests.cs`

**Interfaces:**
- Consumes: `JsonFileStore.Load/Save`, `AppPaths.*FilePath`, `Models.Layout/AppSettings/TodoItem`.
- Produces:
  - `LayoutStore.Load(string path) : Layout`, `LayoutStore.Save(string path, Layout layout) : void`
  - `SettingsStore.Load(string path) : AppSettings`, `SettingsStore.Save(string path, AppSettings settings) : void`
  - `TodoStore.Load(string path) : List<TodoItem>`, `TodoStore.Save(string path, List<TodoItem> items) : void`

  Each store takes an explicit path parameter (rather than hardcoding `AppPaths`) so tests can point at temp files; production callers pass `AppPaths.LayoutFilePath` etc.

- [ ] **Step 1: Write the failing tests**

`tests/WidgX.Tests/Persistence/StoreTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter StoreTests`
Expected: FAIL (compile error — types don't exist yet)

- [ ] **Step 3: Implement the three stores**

`src/WidgX/Persistence/LayoutStore.cs`:

```csharp
using WidgX.Models;

namespace WidgX.Persistence;

public static class LayoutStore
{
    public static Layout Load(string path) => JsonFileStore.Load(path, new Layout());

    public static void Save(string path, Layout layout) => JsonFileStore.Save(path, layout);
}
```

`src/WidgX/Persistence/SettingsStore.cs`:

```csharp
using WidgX.Models;

namespace WidgX.Persistence;

public static class SettingsStore
{
    public static AppSettings Load(string path) => JsonFileStore.Load(path, new AppSettings());

    public static void Save(string path, AppSettings settings) => JsonFileStore.Save(path, settings);
}
```

`src/WidgX/Persistence/TodoStore.cs`:

```csharp
using System.Collections.Generic;
using WidgX.Models;

namespace WidgX.Persistence;

public static class TodoStore
{
    public static List<TodoItem> Load(string path) => JsonFileStore.Load(path, new List<TodoItem>());

    public static void Save(string path, List<TodoItem> items) => JsonFileStore.Save(path, items);
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter StoreTests`
Expected: PASS (3 tests)

- [ ] **Step 5: Commit**

```bash
git add src/WidgX/Persistence tests/WidgX.Tests/Persistence
git commit -m "Add LayoutStore, SettingsStore, TodoStore"
```

---

### Task 5: Win32 interop — desktop-layer reparenting and click-through hit-testing

**Files:**
- Create: `src/WidgX/Native/NativeMethods.cs`
- Create: `src/WidgX/Native/DesktopLayerHelper.cs`
- Create: `src/WidgX/Native/ClickThroughHitTester.cs`
- Test: `tests/WidgX.Tests/Native/ClickThroughHitTesterTests.cs`

**Interfaces:**
- Produces:
  - `DesktopLayerHelper.SendToDesktopLayer(IntPtr hwnd) : void` — finds/creates the wallpaper-layer `WorkerW` and reparents `hwnd` under it.
  - `ClickThroughHitTester.HitTest(System.Windows.Point pointInWindowDip, IEnumerable<System.Windows.Rect> widgetBoundsInDip) : bool` — returns `true` if the point is inside any widget rect (should receive the click), `false` otherwise (should be click-through). Pure logic, testable without a real window.

  `ClickThroughHitTester` is deliberately separated from the raw `WM_NCHITTEST` P/Invoke plumbing so the targeting logic is unit-testable; the WndProc hook (wired in Task 6) calls it.

- [ ] **Step 1: Write the failing test for the pure hit-test logic**

`tests/WidgX.Tests/Native/ClickThroughHitTesterTests.cs`:

```csharp
using System.Collections.Generic;
using System.Windows;
using WidgX.Native;
using Xunit;

namespace WidgX.Tests.Native;

public class ClickThroughHitTesterTests
{
    [Fact]
    public void Point_Outside_All_Widgets_Is_Not_A_Hit()
    {
        var widgets = new List<Rect> { new Rect(0, 0, 100, 50) };
        var result = ClickThroughHitTester.HitTest(new Point(200, 200), widgets);
        Assert.False(result);
    }

    [Fact]
    public void Point_Inside_A_Widget_Is_A_Hit()
    {
        var widgets = new List<Rect> { new Rect(0, 0, 100, 50) };
        var result = ClickThroughHitTester.HitTest(new Point(10, 10), widgets);
        Assert.True(result);
    }

    [Fact]
    public void No_Widgets_Means_Never_A_Hit()
    {
        var result = ClickThroughHitTester.HitTest(new Point(10, 10), new List<Rect>());
        Assert.False(result);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter ClickThroughHitTesterTests`
Expected: FAIL (compile error — `WidgX.Native` does not exist yet)

- [ ] **Step 3: Implement ClickThroughHitTester**

`src/WidgX/Native/ClickThroughHitTester.cs`:

```csharp
using System.Collections.Generic;
using System.Windows;

namespace WidgX.Native;

public static class ClickThroughHitTester
{
    public static bool HitTest(Point pointInWindowDip, IEnumerable<Rect> widgetBoundsInDip)
    {
        foreach (var rect in widgetBoundsInDip)
        {
            if (rect.Contains(pointInWindowDip))
            {
                return true;
            }
        }

        return false;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter ClickThroughHitTesterTests`
Expected: PASS (3 tests)

- [ ] **Step 5: Implement the raw Win32 P/Invoke declarations**

`src/WidgX/Native/NativeMethods.cs`:

```csharp
using System;
using System.Runtime.InteropServices;

namespace WidgX.Native;

internal static class NativeMethods
{
    public const int WM_NCHITTEST = 0x0084;
    public const int HTTRANSPARENT = -1;
    public const int HTCLIENT = 1;
    public const uint SMTO_NORMAL = 0x0000;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam,
        uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
}
```

- [ ] **Step 6: Implement DesktopLayerHelper using the standard WorkerW trick**

`src/WidgX/Native/DesktopLayerHelper.cs`:

```csharp
using System;

namespace WidgX.Native;

public static class DesktopLayerHelper
{
    public static void SendToDesktopLayer(IntPtr hwnd)
    {
        var progman = NativeMethods.FindWindow("Progman", null);
        if (progman == IntPtr.Zero)
        {
            return;
        }

        // Asking Progman to spawn a WorkerW behind the desktop icons is the
        // documented (if undocumented-by-Microsoft) trick every "desktop
        // wallpaper widget" tool, including Rainmeter, relies on.
        NativeMethods.SendMessageTimeout(
            progman, 0x052C, IntPtr.Zero, IntPtr.Zero,
            NativeMethods.SMTO_NORMAL, 1000, out _);

        IntPtr workerW = IntPtr.Zero;
        NativeMethods.EnumWindows((topHandle, _) =>
        {
            var shellView = NativeMethods.FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView != IntPtr.Zero)
            {
                workerW = NativeMethods.FindWindowEx(IntPtr.Zero, topHandle, "WorkerW", null);
            }

            return true;
        }, IntPtr.Zero);

        if (workerW != IntPtr.Zero)
        {
            NativeMethods.SetParent(hwnd, workerW);
        }
    }
}
```

- [ ] **Step 7: Commit**

```bash
git add src/WidgX/Native tests/WidgX.Tests/Native
git commit -m "Add Win32 interop: desktop-layer reparenting and click-through hit-test logic"
```

---

### Task 6: OverlayWindow

**Files:**
- Create: `src/WidgX/Overlay/OverlayWindow.xaml`
- Create: `src/WidgX/Overlay/OverlayWindow.xaml.cs`

**Interfaces:**
- Consumes: `DesktopLayerHelper.SendToDesktopLayer`, `ClickThroughHitTester.HitTest`, `NativeMethods.WM_NCHITTEST/HTTRANSPARENT/HTCLIENT`.
- Produces: `OverlayWindow` — a WPF `Window` subclass with constructor `OverlayWindow(System.Windows.Rect screenBoundsDip)`. Exposes `void SetWidgetBounds(IEnumerable<System.Windows.Rect> bounds)` so later phases can register where interactive widgets are (Phase 1 calls this with an empty list since no widgets exist yet).

- [ ] **Step 1: Write the XAML — borderless, transparent, no taskbar entry**

`src/WidgX/Overlay/OverlayWindow.xaml`:

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
    <Grid x:Name="RootGrid" Background="Transparent" />
</Window>
```

- [ ] **Step 2: Implement the code-behind**

`src/WidgX/Overlay/OverlayWindow.xaml.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using WidgX.Native;

namespace WidgX.Overlay;

public partial class OverlayWindow : Window
{
    private List<Rect> _widgetBounds = new();
    private HwndSource? _hwndSource;

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

- [ ] **Step 3: Commit**

```bash
git add src/WidgX/Overlay
git commit -m "Add OverlayWindow with click-through WM_NCHITTEST hook and desktop-layer reparenting"
```

---

### Task 7: Screen selection and fallback logic

**Files:**
- Create: `src/WidgX/Overlay/ScreenResolver.cs`
- Test: `tests/WidgX.Tests/Overlay/ScreenResolverTests.cs`

**Interfaces:**
- Produces:
  - `ScreenInfo { string Id; string FriendlyName; System.Windows.Rect BoundsDip; bool IsPrimary; }`
  - `ScreenResolver.GetAllScreens() : List<ScreenInfo>` — wraps `System.Windows.Forms.Screen.AllScreens` (id = `screen.DeviceName`).
  - `ScreenResolver.ResolveSelected(string? requestedScreenId, IReadOnlyList<ScreenInfo> availableScreens) : ScreenInfo` — returns the screen matching `requestedScreenId`; if not found (disconnected) or `null`, returns the primary screen. Pure function, fully testable without real displays.

- [ ] **Step 1: Write the failing tests for the pure resolution logic**

`tests/WidgX.Tests/Overlay/ScreenResolverTests.cs`:

```csharp
using System.Collections.Generic;
using System.Windows;
using WidgX.Overlay;
using Xunit;

namespace WidgX.Tests.Overlay;

public class ScreenResolverTests
{
    private static List<ScreenInfo> TwoScreens() => new()
    {
        new ScreenInfo { Id = "DISPLAY1", FriendlyName = "Primary", BoundsDip = new Rect(0, 0, 1920, 1080), IsPrimary = true },
        new ScreenInfo { Id = "DISPLAY2", FriendlyName = "Secondary", BoundsDip = new Rect(1920, 0, 1920, 1080), IsPrimary = false }
    };

    [Fact]
    public void Resolves_Requested_Screen_When_Present()
    {
        var result = ScreenResolver.ResolveSelected("DISPLAY2", TwoScreens());
        Assert.Equal("DISPLAY2", result.Id);
    }

    [Fact]
    public void Falls_Back_To_Primary_When_Requested_Screen_Missing()
    {
        var result = ScreenResolver.ResolveSelected("DISPLAY9", TwoScreens());
        Assert.Equal("DISPLAY1", result.Id);
    }

    [Fact]
    public void Falls_Back_To_Primary_When_No_Screen_Requested()
    {
        var result = ScreenResolver.ResolveSelected(null, TwoScreens());
        Assert.Equal("DISPLAY1", result.Id);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter ScreenResolverTests`
Expected: FAIL (compile error — `WidgX.Overlay.ScreenResolver`/`ScreenInfo` don't exist yet)

- [ ] **Step 3: Implement ScreenInfo and ScreenResolver**

`src/WidgX/Overlay/ScreenResolver.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace WidgX.Overlay;

public class ScreenInfo
{
    public string Id { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public Rect BoundsDip { get; set; }
    public bool IsPrimary { get; set; }
}

public static class ScreenResolver
{
    public static List<ScreenInfo> GetAllScreens()
    {
        return Screen.AllScreens.Select(screen => new ScreenInfo
        {
            Id = screen.DeviceName,
            FriendlyName = screen.DeviceName,
            BoundsDip = new Rect(screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height),
            IsPrimary = screen.Primary
        }).ToList();
    }

    public static ScreenInfo ResolveSelected(string? requestedScreenId, IReadOnlyList<ScreenInfo> availableScreens)
    {
        if (requestedScreenId != null)
        {
            var match = availableScreens.FirstOrDefault(s => s.Id == requestedScreenId);
            if (match != null)
            {
                return match;
            }
        }

        return availableScreens.FirstOrDefault(s => s.IsPrimary) ?? availableScreens[0];
    }
}
```

- [ ] **Step 4: Add the WinForms reference needed for `Screen`**

In `src/WidgX/WidgX.csproj`, inside the existing `<PropertyGroup>`, add:

```xml
<UseWindowsForms>true</UseWindowsForms>
```

(Keep the existing `<UseWPF>true</UseWPF>` alongside it.)

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter ScreenResolverTests`
Expected: PASS (3 tests)

- [ ] **Step 6: Commit**

```bash
git add src/WidgX/Overlay tests/WidgX.Tests/Overlay src/WidgX/WidgX.csproj
git commit -m "Add ScreenResolver with disconnected-monitor fallback to primary"
```

---

### Task 8: Tray icon

**Files:**
- Create: `src/WidgX/Tray/TrayIconManager.cs`
- Create: `src/WidgX/Resources/tray-icon.ico` (placeholder binary icon — see step 1)

**Interfaces:**
- Produces: `TrayIconManager` with constructor `TrayIconManager(Action onEditLayout, Action onToggleVisibility, Action onExit)`, method `void Show()`, implements `IDisposable`.

- [ ] **Step 1: Obtain a basic tray icon file**

Use any 32x32 `.ico` file for now (e.g. export a simple placeholder from any icon editor, or copy a stock `.ico` from `%SystemRoot%\System32` such as `shell32.dll`'s embedded icons via a quick script). Place it at `src/WidgX/Resources/tray-icon.ico`. Mark it as embedded resource:

In `src/WidgX/WidgX.csproj`, inside an `<ItemGroup>`, add:

```xml
<Resource Include="Resources\tray-icon.ico" />
```

- [ ] **Step 2: Implement TrayIconManager using System.Windows.Forms.NotifyIcon**

`src/WidgX/Tray/TrayIconManager.cs`:

```csharp
using System;
using System.Windows.Forms;

namespace WidgX.Tray;

public class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public TrayIconManager(Action onEditLayout, Action onToggleVisibility, Action onExit)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Edit Layout", null, (_, _) => onEditLayout());
        menu.Items.Add("Toggle Overlay Visibility", null, (_, _) => onToggleVisibility());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => onExit());

        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location),
            Text = "WidgX",
            ContextMenuStrip = menu,
            Visible = false
        };
    }

    public void Show()
    {
        _notifyIcon.Visible = true;
    }

    public void Dispose()
    {
        _notifyIcon.Dispose();
    }
}
```

(Using `Icon.ExtractAssociatedIcon` off the exe avoids needing to wire up the embedded `.ico` resource loading path right now; Task 1 of Phase 2 can swap in the embedded resource once the designer needs a polished icon.)

- [ ] **Step 3: Commit**

```bash
git add src/WidgX/Tray
git commit -m "Add TrayIconManager with Edit Layout / Toggle Visibility / Exit menu"
```

---

### Task 9: Wire up App startup

**Files:**
- Modify: `src/WidgX/App.xaml`
- Modify: `src/WidgX/App.xaml.cs`

**Interfaces:**
- Consumes: `ScreenResolver.GetAllScreens/ResolveSelected`, `LayoutStore.Load`, `AppPaths.LayoutFilePath`, `OverlayWindow`, `TrayIconManager`.
- Produces: a running app with no visible main window (the overlay is shown explicitly), tray icon visible, overlay positioned on the resolved screen.

- [ ] **Step 1: Remove the default MainWindow from App.xaml**

`src/WidgX/App.xaml` (delete the `StartupUri` attribute the template generated):

```xml
<Application x:Class="WidgX.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources />
</Application>
```

- [ ] **Step 2: Implement startup logic in App.xaml.cs**

`src/WidgX/App.xaml.cs`:

```csharp
using System.Windows;
using WidgX.Overlay;
using WidgX.Persistence;
using WidgX.Tray;

namespace WidgX;

public partial class App : Application
{
    private OverlayWindow? _overlayWindow;
    private TrayIconManager? _trayIconManager;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var layout = LayoutStore.Load(AppPaths.LayoutFilePath);
        var screens = ScreenResolver.GetAllScreens();
        var selectedScreen = ScreenResolver.ResolveSelected(layout.SelectedScreenId, screens);

        _overlayWindow = new OverlayWindow(selectedScreen.BoundsDip);
        _overlayWindow.Show();

        _trayIconManager = new TrayIconManager(
            onEditLayout: () => { /* wired in Phase 2 */ },
            onToggleVisibility: () =>
            {
                if (_overlayWindow.IsVisible) _overlayWindow.Hide();
                else _overlayWindow.Show();
            },
            onExit: () => Shutdown());

        _trayIconManager.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIconManager?.Dispose();
        base.OnExit(e);
    }
}
```

- [ ] **Step 3: Build the app**

Run: `dotnet build src/WidgX/WidgX.csproj`
Expected: `Build succeeded.`

- [ ] **Step 4: Manual verification**

Run: `dotnet run --project src/WidgX/WidgX.csproj`

Expected, observed manually:
- No visible window border anywhere; the overlay is fully transparent (you should not be able to tell it's there visually).
- A WidgX icon appears in the system tray.
- Open another application (e.g. Notepad) and move it over the monitor the overlay is on — the app window should render normally; the overlay should not block or appear above it (desktop-layer placement working).
- Click anywhere on the desktop where the overlay covers it (e.g. a desktop icon) — the click should reach the desktop icon, not be swallowed (click-through working, since `_widgetBounds` is empty).
- Right-click the tray icon: "Toggle Overlay Visibility" hides/shows the (invisible) overlay without error; "Exit" closes the app cleanly.

Stop the app with the tray icon's "Exit" once verified.

- [ ] **Step 5: Commit**

```bash
git add src/WidgX/App.xaml src/WidgX/App.xaml.cs
git commit -m "Wire up App startup: load layout, resolve screen, show overlay and tray icon"
```

---

## Phase 1 Exit Criteria

- `dotnet test` passes for all persistence, hit-test, and screen-resolution unit tests.
- `dotnet run` launches a fully transparent, click-through overlay on the correct (or fallback) monitor, sitting below normal app windows, controllable via the tray icon.
- No widgets render yet — that begins in Phase 2.
