# WidgX Phase 4: Resilience, Tray Polish, and Final QA — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Depends on:** Phases 1, 2, and 3 must be complete before starting this plan.

**Goal:** Close the remaining gaps from the spec's resilience section (unknown widget types in a saved layout must not crash the overlay; a disconnected-monitor fallback must be visible to the user), give the tray icon a real embedded icon instead of the Phase 1 placeholder, wire up the `--background` autostart flag, and run a full manual QA pass against every requirement in the original spec.

**Architecture:** No new subsystems — this phase tightens error handling at existing boundaries (`WidgetHost`, `App.xaml.cs`, `TrayIconManager`) and validates the whole app end-to-end.

**Tech Stack:** .NET 8, WPF (C#), xUnit.

## Global Constraints

(Same as Phases 1–3.)

---

### Task 1: WidgetHost skips unknown/broken widget types instead of crashing

**Files:**
- Modify: `src/WidgX/Widgets/WidgetRegistry.cs`
- Modify: `src/WidgX/Overlay/WidgetHost.cs`
- Test: `tests/WidgX.Tests/Widgets/WidgetRegistryTests.cs`
- Test: `tests/WidgX.Tests/Overlay/WidgetHostTests.cs`

**Interfaces:**
- Produces: `WidgetRegistry.TryGet(string typeName, out WidgetTypeDefinition? definition) : bool`.
- Modifies: `WidgetHost.LoadLayout` to skip any `WidgetInstance` whose `WidgetType` isn't registered (e.g. from an old layout referencing a since-removed widget type), rather than throwing.

- [ ] **Step 1: Write the failing TryGet test**

Add to `tests/WidgX.Tests/Widgets/WidgetRegistryTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter WidgetRegistryTests`
Expected: FAIL (`TryGet` doesn't exist yet)

- [ ] **Step 3: Implement TryGet**

Add to `src/WidgX/Widgets/WidgetRegistry.cs`:

```csharp
public static bool TryGet(string typeName, out WidgetTypeDefinition? definition)
{
    return Definitions.TryGetValue(typeName, out definition);
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter WidgetRegistryTests`
Expected: PASS (5 tests total)

- [ ] **Step 5: Write the failing WidgetHost skip test**

Add to `tests/WidgX.Tests/Overlay/WidgetHostTests.cs`:

```csharp
[Fact]
public void BuildBounds_Still_Includes_Unknown_Widget_Types()
{
    // BuildBounds is pure geometry and doesn't touch the registry, so it
    // should include every instance regardless of whether its type is known.
    var layout = new Layout
    {
        Widgets = new List<WidgetInstance>
        {
            new WidgetInstance { Id = "unknown1", WidgetType = "NotRegistered", X = 0, Y = 0, Width = 10, Height = 10 }
        }
    };

    var bounds = WidgetHost.BuildBounds(layout);
    Assert.Single(bounds);
}
```

(This confirms `BuildBounds` stays a pure geometry function; the actual skip-on-unknown-type behavior lives in `LoadLayout`, which needs a live `Canvas` and is verified manually in Step 8 below since it requires instantiating real `IWidget.View` controls.)

- [ ] **Step 6: Run test to verify it fails or passes trivially, then implement the skip logic**

Run: `dotnet test --filter WidgetHostTests`
Expected: this particular test passes immediately since `BuildBounds` doesn't change — it's a regression guard, not a TDD-red step. Proceed to Step 7.

- [ ] **Step 7: Make LoadLayout resilient to unknown widget types**

Modify `src/WidgX/Overlay/WidgetHost.cs` — replace the `LoadLayout` body's instantiation loop:

```csharp
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
        if (!WidgetRegistry.TryGet(instance.WidgetType, out var definition))
        {
            continue; // Unknown/removed widget type — skip rather than crash the overlay.
        }

        var widget = definition!.CreateWidget();
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
```

- [ ] **Step 8: Manual verification**

Manually edit `%AppData%\WidgX\layout.json` to add a widget entry with `"WidgetType": "NotARealType"`, run the app, and confirm it starts normally (no crash) with every other widget still rendering. Remove the bogus entry afterward.

- [ ] **Step 9: Commit**

```bash
git add src/WidgX/Widgets/WidgetRegistry.cs src/WidgX/Overlay/WidgetHost.cs tests/WidgX.Tests/Widgets/WidgetRegistryTests.cs tests/WidgX.Tests/Overlay/WidgetHostTests.cs
git commit -m "Make WidgetHost skip unknown widget types instead of crashing the overlay"
```

---

### Task 2: Tray icon polish and disconnected-monitor tooltip

**Files:**
- Modify: `src/WidgX/Tray/TrayIconManager.cs`
- Modify: `src/WidgX/App.xaml.cs`

**Interfaces:**
- Modifies: `TrayIconManager` constructor gains no new parameters; adds `void SetTooltip(string text)`.
- `App.xaml.cs` calls `SetTooltip` with a fallback notice when `ScreenResolver.ResolveSelected` didn't return the originally requested screen.

- [ ] **Step 1: Load the embedded icon resource instead of extracting from the exe**

Modify `src/WidgX/Tray/TrayIconManager.cs`:

```csharp
using System;
using System.IO;
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
            Icon = LoadEmbeddedIcon(),
            Text = "WidgX",
            ContextMenuStrip = menu,
            Visible = false
        };
    }

    private static System.Drawing.Icon LoadEmbeddedIcon()
    {
        var uri = new Uri("pack://application:,,,/Resources/tray-icon.ico");
        var resourceStream = System.Windows.Application.GetResourceStream(uri);
        if (resourceStream != null)
        {
            return new System.Drawing.Icon(resourceStream.Stream);
        }

        return System.Drawing.Icon.ExtractAssociatedIcon(
            System.Reflection.Assembly.GetExecutingAssembly().Location)!;
    }

    public void SetTooltip(string text)
    {
        // NotifyIcon.Text has a 63-character limit.
        _notifyIcon.Text = text.Length > 63 ? text[..63] : text;
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

- [ ] **Step 2: Surface the fallback in App startup**

Modify `src/WidgX/App.xaml.cs` — after resolving the screen, compare against the requested id and set the tooltip:

```csharp
var selectedScreen = ScreenResolver.ResolveSelected(layout.SelectedScreenId, screens);

_trayIconManager = new TrayIconManager(/* ...same as before... */);

if (!string.IsNullOrEmpty(layout.SelectedScreenId) && selectedScreen.Id != layout.SelectedScreenId)
{
    _trayIconManager.SetTooltip($"WidgX (fallback: {layout.SelectedScreenId} disconnected, using {selectedScreen.FriendlyName})");
}
else
{
    _trayIconManager.SetTooltip("WidgX");
}

_trayIconManager.Show();
```

(Move the `_trayIconManager = new TrayIconManager(...)` construction to before this comparison if it isn't already, keeping the constructor call exactly as it was in Phase 1/2 — only the tooltip-setting logic is new.)

- [ ] **Step 3: Build and manually verify**

Run: `dotnet build src/WidgX/WidgX.csproj`
Expected: `Build succeeded.`

Manually verify: hovering the tray icon shows "WidgX" normally. To verify the fallback path, temporarily set `SelectedScreenId` in `layout.json` to a bogus value like `"\\\\.\\DISPLAY99"`, restart the app, and confirm the tray tooltip mentions the fallback. Revert the bogus value afterward.

- [ ] **Step 4: Commit**

```bash
git add src/WidgX/Tray/TrayIconManager.cs src/WidgX/App.xaml.cs
git commit -m "Use embedded tray icon resource and surface disconnected-monitor fallback in tooltip"
```

---

### Task 3: `--background` startup flag

**Files:**
- Create: `src/WidgX/Startup/LaunchArgs.cs`
- Modify: `src/WidgX/App.xaml.cs`
- Test: `tests/WidgX.Tests/Startup/LaunchArgsTests.cs`

**Interfaces:**
- Produces: `LaunchArgs.IsBackgroundLaunch(string[] args) : bool` — pure, testable.
- Modifies: `App.OnStartup` reads `e.Args` and stores whether this was a background (autostart) launch, for any future use (currently informational only, since the overlay never shows a visible main window either way).

- [ ] **Step 1: Write the failing test**

`tests/WidgX.Tests/Startup/LaunchArgsTests.cs`:

```csharp
using WidgX.Startup;
using Xunit;

namespace WidgX.Tests.Startup;

public class LaunchArgsTests
{
    [Fact]
    public void IsBackgroundLaunch_True_When_Flag_Present()
    {
        Assert.True(LaunchArgs.IsBackgroundLaunch(new[] { "--background" }));
    }

    [Fact]
    public void IsBackgroundLaunch_False_When_Flag_Absent()
    {
        Assert.False(LaunchArgs.IsBackgroundLaunch(System.Array.Empty<string>()));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter LaunchArgsTests`
Expected: FAIL (compile error)

- [ ] **Step 3: Implement LaunchArgs**

`src/WidgX/Startup/LaunchArgs.cs`:

```csharp
using System.Linq;

namespace WidgX.Startup;

public static class LaunchArgs
{
    public static bool IsBackgroundLaunch(string[] args)
    {
        return args.Contains("--background");
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter LaunchArgsTests`
Expected: PASS (2 tests)

- [ ] **Step 5: Wire into App startup (informational field, no behavior change needed since no window ever flashes)**

Modify `src/WidgX/App.xaml.cs` — add a field and set it at the top of `OnStartup`:

```csharp
private bool _isBackgroundLaunch;
```

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    _isBackgroundLaunch = WidgX.Startup.LaunchArgs.IsBackgroundLaunch(e.Args);

    // ... rest of existing OnStartup unchanged
}
```

- [ ] **Step 6: Run the full test suite**

Run: `dotnet test`
Expected: all tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/WidgX/Startup/LaunchArgs.cs src/WidgX/App.xaml.cs tests/WidgX.Tests/Startup/LaunchArgsTests.cs
git commit -m "Parse --background launch flag used by the autostart registry entry"
```

---

### Task 4: Final end-to-end manual QA pass

**Files:** none (verification only).

- [ ] **Step 1: Run the full automated suite one last time**

Run: `dotnet test`
Expected: all tests across all four phases pass.

- [ ] **Step 2: Fresh-start QA checklist**

Delete `%AppData%\WidgX\` entirely, then run `dotnet run --project src/WidgX/WidgX.csproj` and walk through:

- [ ] App starts with no visible window flash and a tray icon appears.
- [ ] Tray → Edit Layout opens the Designer; the Layout tab's screen picker lists all connected monitors.
- [ ] Add one of each widget type (Calendar, Clock, Todos, Now Playing, System Stats, Uptime, Weather) to the canvas, position them without overlapping, and Save.
- [ ] All seven widgets render correctly on the real desktop overlay: correct month/date, correct time/day/date, an empty todo list ready for input, current track or "Nothing playing", live CPU/RAM/GPU percentages, uptime since boot, and (after setting a location in Settings) current weather + 5-day forecast.
- [ ] Add a todo with a due date from the live overlay widget itself; confirm it persists after restarting the app.
- [ ] Open another application window and drag it over the overlay's monitor — the app window renders normally on top; the widgets do not float above it (desktop-layer placement holding).
- [ ] Click empty space on the overlay (not over any widget) — the click reaches the desktop/icons underneath. Click directly on a widget (e.g. check off a todo) — the click is handled by the widget, not passed through.
- [ ] In Designer Settings, enable autostart; confirm the `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\WidgX` registry value is created pointing at the exe with `--background`. Disable it and confirm the value is removed.
- [ ] Unplug/disable a secondary monitor (or simulate via `layout.json` edit) that was previously selected; confirm the app falls back to the primary monitor and the tray tooltip reflects the fallback.
- [ ] Exit via the tray icon; confirm the process fully terminates (check Task Manager) and no error dialog appears.

- [ ] **Step 3: Record any QA failures as new tasks**

If any checklist item fails, do not mark this task complete — write down the specific failure and fix it before considering Phase 4 (and the overall project) done. Do not skip ahead.

---

## Phase 4 Exit Criteria

- `dotnet test` passes for the full suite across all four phases.
- A layout referencing an unknown widget type no longer crashes the app.
- The tray tooltip communicates a disconnected-monitor fallback.
- The `--background` autostart flag is parsed (even though no visible-window suppression was needed beyond what already existed).
- Every item in the Task 4 manual QA checklist passes against a real Windows desktop, confirming the application satisfies the original request in full: startup-launching, screen-selectable, designer-customizable widgets (calendar, todos, now playing, CPU/RAM/GPU, uptime, weather+forecast, clock/day/date) over a click-through transparent background compatible with Wallpaper Engine.
