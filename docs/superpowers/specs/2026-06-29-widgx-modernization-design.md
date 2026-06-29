# WidgX Modernization & New Widgets — Design Spec

**Date:** 2026-06-29
**Status:** Approved (design), pending spec review → implementation plan
**Branch:** `master` (subagent-driven development, TDD where logic is testable)
**Builds on:** `2026-06-27-widgx-desktop-overlay-design.md` (the completed base app)

## Goal

Modernize the visual design of the completed WidgX overlay + Designer, make the
monitor handling resolution-aware (picker labels *and* a true-to-scale design
canvas), and add several widget/UX features. All changes are additive to the
existing, working app; no behavior already shipped should regress (41/41 tests
stay green).

## Scope (seven workstreams)

1. **Dark-glass theme** across overlay widgets and the Designer window.
2. **Monitor resolution** — friendly model names + resolution in the picker.
3. **WYSIWYG design canvas** — the Designer canvas represents the selected
   monitor to scale.
4. **Bluetooth battery widget** — new widget, paired-device selection.
5. **NowPlaying enhancements** — optional cover art, song progress, optional
   spinning-record animation.
6. **System-stats radial gauges** — animated circular gauges replace text.
7. **Designer UX** — color picker + a non-closing **Apply** button.

Out of scope: true per-widget wallpaper blur (not feasible cheaply on a layered
desktop-child window); multi-device Bluetooth (v1 tracks one device); trimming
(unsupported for WPF).

---

## 1. Dark-glass theme

**What:** A shared, cohesive dark visual language for both the transparent
overlay widgets and the (normal) Designer window.

**Architecture:**
- New `src/WidgX/Themes/Theme.xaml` `ResourceDictionary`, merged in
  `App.xaml` `<Application.Resources>`.
- Defines: palette `SolidColorBrush`es (`SurfaceBrush` ~`#B0_1E1E24`,
  `SurfaceBorderBrush` hairline, `TextPrimaryBrush`, `TextSecondaryBrush`,
  `AccentBrush`), a `WidgetCardStyle` (Border: `CornerRadius=12`, translucent
  fill, 1px hairline border, soft `DropShadowEffect`), and `TextBlock` styles
  (`CardTitleStyle`, `CardBodyStyle`, `CardCaptionStyle`) using Segoe UI
  Variable.
- Designer control styles: themed `Button`, `ComboBox`, `TextBox`,
  `TabControl`/`TabItem`, `ListBox`/`ListBoxItem`, `CheckBox` so the window is
  dark and consistent rather than default light chrome.

**Per-widget refit:** every widget's outer `<Border>` adopts `WidgetCardStyle`
in place of its hard-coded `Background="#80000000" CornerRadius="6"`. Text
elements adopt the shared text styles. **Widget code-behind logic is not
touched** — only XAML chrome/styling. Per-widget `Configure(...)` overrides
(opacity, accent color, font size) continue to win over theme defaults: theme
supplies the baseline, `Configure` mutates the instance's properties as today.

**Designer acrylic:** the Designer `Window` gets real acrylic/blur via a small
Win32 call (`DwmSetWindowAttribute` with `DWMWA_SYSTEMBACKDROP_TYPE`, or
`SetWindowCompositionAttribute` ACCENT_ENABLE_ACRYLICBLURBEHIND as fallback),
isolated in a `WindowAcrylicHelper` and wrapped in try/catch — failure simply
yields a solid dark window. Overlay widgets do **not** get real blur (they're on
a transparent layered child of the desktop); their "glass" is translucency +
rounded + hairline + shadow.

**Testing:** styling has no unit-testable logic; verified by smoke test +
manual visual QA. The acrylic P/Invoke degrades gracefully.

---

## 2. Monitor resolution in the picker

**What:** Replace raw `\\.\DISPLAY1` labels with
`Dell U2720Q — 2560×1440 (Primary)`.

**Architecture:**
- New `src/WidgX/Overlay/MonitorNameResolver.cs`:
  - Native part: Win32 **DisplayConfig** APIs
    (`GetDisplayConfigBufferSizes` → `QueryDisplayConfig` →
    `DisplayConfigGetDeviceInfo` with `DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME`
    for the monitor friendly name and `..._GET_SOURCE_NAME` for the
    `\\.\DISPLAYn` GDI name). Builds a `Dictionary<string GdiName, string
    FriendlyName>`. Entirely wrapped in try/catch; on any failure returns an
    empty map.
  - Pure part: `FormatLabel(string friendlyName, Rect bounds, bool isPrimary)
    : string` → `"{name} — {W}×{H}{ (Primary)}"`. **Unit-tested.**
- `ScreenResolver.GetAllScreens()` calls the resolver once, sets
  `ScreenInfo.FriendlyName` to `FormatLabel(resolvedName ?? deviceName,
  bounds, isPrimary)`. `ScreenInfo` may gain a `Resolution` convenience
  (`Size`) derived from `Bounds`, but the label is the primary surface.
- Picker already binds `DisplayMemberPath="FriendlyName"` — no XAML change.

**Persistence unchanged & already correct:** `Id` (`\\.\DISPLAYn`) is the stable
key saved in `SelectedScreenId`; resolution is read live from `Bounds`. The
DisplayConfig friendly-name map keys on the same GDI device name, so it
correlates reliably.

**Testing:** `FormatLabel` unit tests (primary/non-primary, name fallback).
Native lookup is isolated; smoke-tested.

---

## 3. WYSIWYG design canvas

**What:** Today the canvas is an arbitrary `#222` box and widget X/Y are placed
1:1, so the canvas doesn't show where a widget actually lands on screen. Make
the canvas represent the **selected monitor to scale**, preserving aspect ratio,
so placement is true WYSIWYG.

**Architecture:**
- Wrap the inner design `Canvas` in a `Viewbox Stretch="Uniform"` inside the
  existing center column. Set the inner `Canvas.Width/Height` to the monitor's
  **DIP extent** (not raw physical px — see subtlety). The Viewbox auto-scales
  and letterboxes to fit the available area while preserving aspect ratio;
  switching monitors in the dropdown resets these dimensions and reshapes the
  canvas.
- An overlaid resolution label (e.g. `2560 × 1440`) sits in a corner of the
  canvas area.
- `DesignerWidgetBox` placement (`Canvas.SetLeft/Top` with `instance.X/Y`) and
  drag math are **unchanged in form**: because boxes live *inside* the inner
  canvas, `e.GetPosition(innerCanvas)` returns coordinates already in the
  canvas's (monitor-DIP) space, so drag deltas map 1:1 to stored coordinates —
  the Viewbox handles visual scaling transparently. This keeps the box logic
  simple and correct.

**Subtlety (must get right):** stored `WidgetInstance.X/Y/Width/Height` are WPF
device-independent units as interpreted by the overlay window at the *target
monitor's* DPI scale. So the inner canvas must be sized to the monitor's DIP
extent = `physicalBounds / targetMonitorScaleFactor`, not the physical pixel
resolution. Aspect ratio is identical either way (uniform scale), but using the
DIP extent keeps placement pixel-accurate on scaled (125 %/150 %) displays. The
target monitor's scale factor is obtained from the monitor DPI (e.g.
`GetDpiForMonitor`), isolated with a safe fallback to 1.0. This ties directly to
the PerMonitorV2 fix already in the app.

**Testing:** a pure helper `CanvasGeometry.DipExtent(Rect physicalBounds, double
scale) : Size` (and any coordinate mapping helper) is unit-tested. Visual
correctness (drag lands where dropped) is manual QA.

---

## 4. Bluetooth battery widget

**What:** New widget showing a chosen paired Bluetooth device's battery
percentage (and charging state *if* the device reports it), or a clear `✕` when
disconnected/unavailable.

**Architecture:**
- New folder `src/WidgX/Widgets/Bluetooth/`:
  - `BluetoothBatteryService` (WinRT `Windows.Devices.Enumeration`):
    - `Task<IReadOnlyList<BluetoothDeviceInfo>> GetPairedDevicesAsync()` —
      enumerates paired BT + BLE devices (`BluetoothDevice` /
      `BluetoothLEDevice` AQS selectors) → `{ Id, Name }` for the Designer
      dropdown.
    - `Task<BluetoothDeviceStatus> GetStatusAsync(string deviceId)` — reads the
      device's battery level via the standard battery device-property
      (`DeviceInformation.CreateFromIdAsync` requesting
      `"{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2"` = `System.Devices.BatteryLevel`,
      the same value Windows Settings shows) and connection status. Returns
      `BluetoothDeviceStatus { Name, IsConnected, BatteryPercent (int?),
      IsCharging (bool?) }`. All in try/catch; failure → `IsConnected=false`.
  - `BluetoothStatusFormatter.Format(BluetoothDeviceStatus) : string` — **pure,
    unit-tested.** Connected → `"{name}  {pct}%"` (+ a charging glyph only when
    `IsCharging == true`); disconnected/unknown battery → `"✕ {name}"` (or just
    `"✕"` when no name).
  - `BluetoothWidget` (UserControl + IWidget) — `WidgetCardStyle` chrome; a
    `DispatcherTimer` (~30 s) calls `GetStatusAsync` and renders via the
    formatter; `StartUpdates/StopUpdates` manage the timer.
  - `BluetoothWidgetRegistration` — registered at startup with the other widgets.
- **Selected device id** persists per-widget in the existing
  `WidgetInstance.Settings` dictionary (key `"deviceId"`). The Designer's
  properties panel, when a Bluetooth widget is selected, shows a paired-device
  dropdown (populated by `GetPairedDevicesAsync`) that writes
  `instance.Settings["deviceId"]`. (Using the per-instance bag — which already
  exists — means multiple BT widgets can track different devices, unlike the
  single-value weather-location pattern.)

**Honesty/limits:** battery % is available for many but not all devices;
charging state is rarely exposed by BT peripherals and will usually be absent —
the formatter shows it only when present. Worst case degrades to `✕`.

**Testing:** `BluetoothStatusFormatter` unit tests (connected w/ %, connected w/
charging, disconnected, no-name). WinRT enumeration isolated + smoke-tested.

---

## 5. NowPlaying enhancements

**What:** Optional album cover art, a song-progress bar, and an optional
spinning-record animation of the cover.

**Architecture:**
- Extend `MediaSessionService` (keep `FormatNowPlaying` pure + tested):
  - `GetCurrentTrackAsync` returns, in addition to title/artist, a thumbnail
    `ImageSource?` (from `props.Thumbnail.OpenReadAsync()` → `BitmapImage`) and
    timeline info from `session.GetTimelineProperties()` (`Position`,
    `StartTime`, `EndTime`) plus `GetPlaybackInfo().PlaybackStatus`. Wrapped in
    try/catch; missing pieces are null.
  - Pure helper `ComputeProgressFraction(TimeSpan position, TimeSpan start,
    TimeSpan end) : double` (clamped 0..1) — **unit-tested.**
- `NowPlayingWidget.xaml`: `WidgetCardStyle`; an optional `Image` (cover), the
  title/artist text, and a slim `ProgressBar`/custom progress track bound to the
  computed fraction. Timer-polled (~1 s for smooth progress).
- **Spinning record:** when enabled and `PlaybackStatus == Playing`, a
  continuous `RotateTransform` `DoubleAnimation` (e.g. 360° over ~4 s,
  `RepeatBehavior=Forever`) spins the cover; paused/stopped pauses the
  animation. Disabled → static cover.
- **Toggles** stored in `WidgetInstance.Settings`: `"showCover"` and
  `"spinCover"` ("true"/"false"). Designer properties panel shows two
  checkboxes when a NowPlaying widget is selected.

**Testing:** `ComputeProgressFraction` unit tests (mid-track, before start, past
end, zero-length). Thumbnail/timeline retrieval + animation are smoke/visual QA.

---

## 6. System-stats radial gauges

**What:** Replace the CPU/RAM/GPU text lines with three animated circular
gauges.

**Architecture:**
- New reusable `src/WidgX/Controls/RadialGauge.xaml(.cs)` UserControl:
  - Dependency properties `Value` (0–100), `Label` (e.g. "CPU"), `Accent`.
  - Renders a background ring + a foreground arc whose sweep angle ∝ Value,
    drawn with an `ArcSegment`; a center `TextBlock` shows `{Value}%` and the
    label. The pure arc math — `GaugeGeometry.PointOnArc(...)` / sweep-angle and
    `IsLargeArc` computation — lives in a static helper and is **unit-tested.**
  - **Animated:** `Value` changes animate the arc smoothly (animate an internal
    angle DP via `DoubleAnimation`, ~300 ms ease), so gauges sweep rather than
    jump.
- `SystemStatsWidget.xaml`: three `RadialGauge`s (CPU/RAM/GPU) in a horizontal
  panel, `WidgetCardStyle` chrome. Code-behind keeps the existing
  `SystemStatsService` polling/`Refresh()`/`IDisposable` wiring — it just sets
  each gauge's `Value` instead of `…Text.Text`. The `_service.Dispose()` in
  `StopUpdates()` stays.

**Testing:** `GaugeGeometry` unit tests (0 %, 50 %, 100 %, large-arc boundary).
Animation/visual is manual QA.

---

## 7. Designer UX: color picker + Apply button

**Color picker (replaces the accent hex `TextBox`):**
- New lightweight `src/WidgX/Controls/ColorPickerControl.xaml(.cs)` — **no new
  NuGet dependency.** A small palette of preset swatches + R/G/B sliders + a hex
  box that stay in sync; exposes a `SelectedColorHex` DP and a changed event.
  Pure conversion helpers `HexToColor`/`ColorToHex` (with graceful handling of
  bad input) are **unit-tested.**
- The Designer properties panel uses it in place of `AccentColorBox`; applying
  writes `instance.AccentColorHex`.

**Apply button (apply without closing):**
- Today: `OnSave` saves + `Close()`; `OnDiscard` just `Close()`; `OnApplyProperties`
  mutates the selected instance + rebuilds the canvas but does **not** persist or
  update the live overlay.
- Add a bottom-bar **Apply** button → calls `_onSaved(_workingLayout)` (persists
  layout + live-reloads the overlay, exactly what Save's callback does) **but
  does not close** the window. Final semantics:
  - **Apply** — persist + live-update overlay, keep Designer open.
  - **Save** — persist + live-update + close.
  - **Discard** — close without persisting (unsaved working changes dropped).
- The per-instance **Apply** (properties panel) keeps its current role (commit
  edited fields to the working instance + refresh canvas); the new bottom-bar
  **Apply** is what pushes the whole working layout to disk + overlay.

**Testing:** color conversion helpers unit-tested; Apply/Save/Discard flow is
smoke + manual QA.

---

## Cross-cutting

- **Schema:** no new model fields needed — `WidgetInstance.Settings` (already
  present) carries `deviceId`, `showCover`, `spinCover`. Existing
  layout/settings JSON remains backward-compatible (missing keys → defaults).
- **Isolation:** each new piece is a focused unit with a pure, tested core and
  an isolated side-effecting shell (DisplayConfig, BT WinRT, DWM acrylic, SMTC
  thumbnail/timeline, gauge/arc rendering). Every native/WinRT call is
  try/caught with a graceful visual fallback so no failure crashes the app.
- **Process:** subagent-driven; TDD for every pure helper listed above; one
  commit per coherent task; smoke test (launch self-contained `build\WidgX.exe`
  or a debug build, confirm alive, terminate) after each; final whole-branch
  review; manual visual QA by the user (display required, especially for
  acrylic, gauges, spin animation, and WYSIWYG-canvas accuracy on a scaled
  monitor).
- **Distribution:** `rebuild.bat` produces the self-contained `build\` distro;
  unaffected by these changes.

## Success criteria

- Overlay widgets + Designer share a modern dark-glass look; Designer shows
  acrylic where supported.
- Screen picker shows `Model — W×H (Primary)`; design canvas matches the
  monitor's shape and placement is WYSIWYG.
- A Bluetooth widget shows a chosen device's battery % / `✕`.
- NowPlaying can show cover + progress, and optionally spin the cover.
- CPU/RAM/GPU render as animated radial gauges.
- Designer has a color picker and an Apply-without-close button.
- All existing tests still pass; new pure helpers are unit-tested; build stays
  clean; nothing previously shipped regresses.
