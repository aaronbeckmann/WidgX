# WidgX

A modern desktop‑widget overlay for Windows. WidgX paints a set of customizable
widgets directly onto your wallpaper (behind your icons and windows), with a
visual designer for arranging them per monitor. It runs from the system tray and
needs no administrator rights.

Built with WPF on .NET 8.

---

## Features

- **Desktop overlay** — widgets are drawn on the desktop "wallpaper" layer, so
  they sit behind your windows and stay out of the way. Click‑through everywhere
  except the widgets themselves.
- **Visual designer** — a WYSIWYG canvas that mirrors your monitor's real
  resolution (DPI‑aware), with drag/resize and **PowerPoint‑style alignment
  snapping** (edges and centers of other widgets, plus the screen center) with
  guide lines.
- **Per‑widget styling** — opacity (fades the card background and border, never
  the content), accent color (live preview + RGB sliders + swatches), font size,
  and a **font picker with live previews** of every installed font.
- **Multi‑monitor** — pick which monitor to render on; the picker shows the real
  model name and resolution (e.g. `Dell U2720Q — 2560×1440 (Primary)`).
- **Dark glass theme** throughout the widgets and the designer.
- **Self‑contained distribution** — the build bundles the .NET runtime, so the
  output folder runs on a machine without .NET installed.

### Widgets

| Widget | Description |
| --- | --- |
| **Clock** | Time, weekday and date. Reorder/toggle each line, per‑item font size, and a choice of date formats. |
| **Calendar** | Current month grid with today highlighted. |
| **Weather + Forecast** | Current conditions and a 5‑day forecast with weather icons (data from [Open‑Meteo](https://open-meteo.com/)). |
| **System Stats (CPU/RAM/GPU/VRAM)** | Animated radial gauges for CPU, RAM, GPU and dedicated VRAM usage. |
| **Now Playing** | Current track from any media app, with album cover (circular spinning "record" disk or full square art), song progress, an optional color‑fade background sampled from the cover, and a marquee title when it doesn't fit. |
| **Battery (BT / USB devices)** | Battery level for connected devices — Bluetooth audio, controllers, and USB/HID peripherals. Show one device or all devices that report a level. |
| **Todos** | A modern checklist with due dates (custom dark date picker), completed‑item styling, and quick add. |
| **Uptime** | How long the machine has been running. |

---

## Requirements

- Windows 10 version 2004 (build 19041) or newer.
- No administrator rights required. Autostart is registered under the current
  user (`HKCU`) only.
- To build from source: the [.NET 8 SDK](https://dotnet.microsoft.com/download).

---

## Install

Download the latest **`WidgX-<version>-win-x64.msi`** from the
[Releases](https://github.com/aaronbeckmann/WidgX/releases) page and run it. It
installs per‑user (no administrator rights) into
`%LocalAppData%\Programs\WidgX`, adds a Start Menu shortcut, and registers an
Add/Remove Programs entry. Alternatively, download the `.zip`, extract it, and run
`WidgX.exe` directly.

WidgX checks GitHub for newer releases on startup and from the tray menu
(**Check for Updates**); when one is available it shows a notification that links
to the download.

## Build & run

### Quick build (recommended)

Run the build script from the repository root:

```bat
rebuild.bat
```

This publishes a **self‑contained**, Release `win-x64` build (the .NET runtime is
bundled) into the `build\` directory. Launch it with:

```bat
build\WidgX.exe
```

### Using the .NET SDK directly

```bat
dotnet run --project src\WidgX\WidgX.csproj
```

Or publish manually:

```bat
dotnet publish src\WidgX\WidgX.csproj -c Release -r win-x64 --self-contained true -o build
```

### Build the installer

Install the free [WiX v5](https://wixtoolset.org/) toolset once
(`dotnet tool install --global wix --version 5.0.2`), then run:

```bat
installer\build-installer.bat
```

This rebuilds the app and produces a single per‑user MSI in `dist\`.

---

## Usage

1. Launch `WidgX.exe`. It starts in the system tray.
2. Right‑click the tray icon and open the **Designer**.
3. Add widgets from the palette, then drag/resize them on the canvas. Alignment
   guides appear while dragging.
4. Select a widget to edit its properties (position, size, opacity, accent color,
   font, and widget‑specific options) in the panel on the right.
5. **Apply** updates the live overlay without closing the designer; **Save**
   applies and closes; **Discard** closes without saving.
6. Enable "Run WidgX when Windows starts" in the **Settings** tab for autostart,
   and set your weather location there.

---

## Configuration & data

User data lives in `%AppData%\WidgX\`:

- `layout.json` — widget layout and per‑widget settings.
- `settings.json` — app settings (weather location, autostart, designer window
  bounds).
- `todos.json` — todo items.

---

## Project layout

```
src/WidgX/            Application (WPF, .NET 8)
  Overlay/            Desktop overlay window, monitor/DPI resolution
  Designer/           Designer window, snapping, custom controls
  Controls/           Reusable controls (radial gauge, color helpers, date picker)
  Widgets/            One folder per widget (view + service + registration)
  Themes/             Shared dark‑glass resource dictionary
  Persistence/        JSON storage for layout/settings/todos
tests/WidgX.Tests/    xUnit tests
docs/                 Design specs and plans
rebuild.bat           Self‑contained build script
```

---

## Testing

```bat
dotnet test
```

The suite covers the pure logic behind the widgets and designer (formatting,
gauge geometry, color/hex conversion, alignment snapping, palette extraction,
battery name normalization, monitor label formatting, and more).

---

## Notes

- The overlay is per‑monitor‑DPI aware; the designer canvas is sized in
  device‑independent units so it represents the screen correctly at any scaling.
- Media info uses the Windows System Media Transport Controls; Bluetooth/USB
  battery and device enumeration use the Windows device APIs; total VRAM is read
  via DXGI. All are read‑only and require no elevation.
- Weather requests are sent to Open‑Meteo's public API using the location you set.
