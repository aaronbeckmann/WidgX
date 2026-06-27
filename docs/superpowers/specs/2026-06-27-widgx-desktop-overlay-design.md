# WidgX — Desktop Widget Overlay — Design

Date: 2026-06-27

## Summary

WidgX is a Windows desktop application that displays customizable widgets (calendar, todos, now-playing, system stats, uptime, weather/forecast, clock) on a single selected monitor. It runs at startup, sits at the desktop layer (above a Wallpaper Engine background, below normal app windows), and is fully click-through except where widgets actually sit. A separate designer window lets the user add, place, resize, and configure widgets, and pick the active screen.

## Architecture

- **Platform**: WPF on .NET 8 (C#), Windows-only.
- **Single executable**, two windows:
  - **Overlay window** (default at launch): borderless, transparent, pinned to the selected monitor's full bounds, reparented under the desktop's `WorkerW` window (the Rainmeter "on-desktop" technique) so it renders above Wallpaper Engine but below all normal application windows.
  - **Designer window**: opened via the system tray icon ("Edit Layout"). Lets the user manage widgets and settings; writes layout changes that the overlay hot-reloads without restarting.
- **Tray icon** (right-click menu): "Edit Layout", "Toggle Overlay Visibility", "Exit".
- **Persistence**: JSON files under `%AppData%\WidgX\`:
  - `layout.json` — widget instances and their config (type, position, size, opacity, accent color, font size, widget-specific settings) plus the selected screen identifier.
  - `todos.json` — todo items (text, due date, completed flag).
  - `settings.json` — autostart flag, weather location.
- **Autostart**: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` entry pointing at the exe with a `--background` flag, toggled from the designer's settings section. No admin/elevation required anywhere in the app.

## Overlay Window Mechanics

1. **Click-through with interactive widgets**: the overlay is a single `WS_EX_LAYERED` window. Its `WndProc` intercepts `WM_NCHITTEST`: if the cursor is over empty space, return `HTTRANSPARENT` so the click passes through to the desktop/Wallpaper Engine; if it's over a widget's bounds, return `HTCLIENT` so that widget gets the click (e.g. checking off a todo). This avoids needing separate top-level windows per widget.
2. **Desktop-layer placement**: on startup, find the desktop `WorkerW` window (via the standard `Progman`/`WorkerW` enumeration trick) and set it as the overlay's parent, so the overlay always sits above the wallpaper layer and below normal app windows without manual z-order maintenance.
3. The overlay's bounds equal the selected monitor's full `Bounds` (not just the work area), so widgets may be placed anywhere on the screen including behind/around the taskbar region.
4. If the previously selected monitor is disconnected at startup, fall back to the primary monitor and surface this in the tray icon tooltip.

## Widget System

All widgets implement a common contract:

```
IWidget
  UserControl View
  WidgetConfig Config   // Position, Size, Opacity, AccentColor, FontSize + widget-specific fields
  void Configure(WidgetConfig)
  void StartUpdates() / StopUpdates()
```

A `WidgetRegistry` maps a widget type name to: its control type, its default config, and its designer property-panel control. Adding a new widget type later requires no changes to the overlay or designer core — only a new registry entry.

Each widget refreshes on its own independent timer/event so a slow or failing data source never blocks the others.

| Widget | Data source | Refresh |
|---|---|---|
| Calendar (month + date) | `DateTime.Now` | Daily / on month change |
| Clock (time, day of week, date) | `DateTime.Now` | Every second |
| Todos (with due dates) | `todos.json`, full CRUD in-widget | On change |
| Now Playing | Windows `GlobalSystemMediaTransportControlsSessionManager` (SMTC) | On session change event |
| CPU / RAM / GPU usage | Windows Performance Counters (`% Processor Time`, memory counters, `GPU Engine` category) | Every 1–2s |
| Uptime | `Environment.TickCount64` | Every second |
| Weather + forecast | Open-Meteo (geocoding + forecast APIs, no API key) | Every ~20 min |

## Designer Window

- **Screen picker**: dropdown of connected monitors (friendly name + resolution); selecting one updates the canvas aspect ratio and which screen will host the overlay.
- **Canvas**: scaled-down representation of the selected screen, rendering placed widgets as live-preview, draggable/resizable boxes.
- **Widget palette**: sidebar of available widget types; click or drag to add a new instance to the canvas.
- **Properties panel**: for the selected widget, exposes common settings (position, size, opacity, accent color, font size as synced numeric/visual controls) plus widget-specific settings (e.g. weather location).
- **Save / Discard**: Save persists `layout.json` and signals the running overlay to hot-reload; Discard reverts unsaved canvas changes.
- **Settings section**: autostart toggle, weather location input.

## Resilience

- Each data service (weather, media session, system stats) degrades independently and silently — e.g. an unreachable weather API shows "Unavailable" in the widget and retries on its own schedule rather than crashing the overlay.
- Malformed/missing JSON config files are treated as "no config" and the app starts with an empty layout rather than failing to launch.

## Testing

- Unit tests (xUnit) for data services and pure logic: system stats counter parsing, weather response mapping, todo CRUD, layout JSON round-trip/migration.
- The Win32 click-through/`WorkerW` reparenting logic and the designer UI are inherently interactive/visual and are validated manually rather than via automated tests.

## Out of Scope (v1)

- Multiple simultaneous active screens (only one overlay/screen at a time; switching screens is supported).
- Full theming engine (only opacity/accent color/font size per widget, not arbitrary skinning).
- Hardware temperature sensors (would require elevation); only usage percentages are shown.
- Cloud sync / multi-device config sharing.
