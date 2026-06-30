using System;
using System.Threading.Tasks;
using System.Windows;
using WidgX.Designer;
using WidgX.Overlay;
using WidgX.Persistence;
using WidgX.Tray;
using WidgX.Update;
using WidgX.Widgets.Bluetooth;
using WidgX.Widgets.Calendar;
using WidgX.Widgets.Clock;
using WidgX.Widgets.NowPlaying;
using WidgX.Widgets.SystemStats;
using WidgX.Widgets.Todos;
using WidgX.Widgets.Uptime;
using WidgX.Widgets.Weather;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace WidgX;

public partial class App : Application
{
    private bool _isBackgroundLaunch;
    private OverlayWindow? _overlayWindow;
    private TrayIconManager? _trayIconManager;
    private DesignerWindow? _designerWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Standalone "check for updates" mode (e.g. from the Start Menu shortcut):
        // show a dialog with the result and exit, without starting the overlay.
        if (WidgX.Startup.LaunchArgs.IsCheckUpdates(e.Args))
        {
            _ = RunStandaloneUpdateCheckAsync();
            return;
        }

        _isBackgroundLaunch = WidgX.Startup.LaunchArgs.IsBackgroundLaunch(e.Args);

        // Self-heal the autostart entry (older builds stored the .dll path, which
        // Windows can't launch) and keep it pointed at the current executable.
        WidgX.Startup.AutostartManager.RefreshIfEnabled();

        ClockWidgetRegistration.Register();
        CalendarWidgetRegistration.Register();
        TodoWidgetRegistration.Register();
        UptimeWidgetRegistration.Register();
        SystemStatsWidgetRegistration.Register();
        NowPlayingWidgetRegistration.Register();
        WeatherWidgetRegistration.Register();
        BluetoothWidgetRegistration.Register();

        var layout = LayoutStore.Load(AppPaths.LayoutFilePath);
        var screens = ScreenResolver.GetAllScreens();
        var selectedScreen = ScreenResolver.ResolveSelected(layout.SelectedScreenId, screens);

        _overlayWindow = new OverlayWindow(selectedScreen.Bounds);
        _overlayWindow.Show();
        _overlayWindow.ReloadLayout(layout);

        _trayIconManager = new TrayIconManager(
            onEditLayout: OpenDesigner,
            onToggleVisibility: () =>
            {
                if (_overlayWindow.IsVisible) _overlayWindow.Hide();
                else _overlayWindow.Show();
            },
            onCheckForUpdates: () => _ = CheckForUpdatesAsync(manual: true),
            onExit: () => Shutdown());

        if (!string.IsNullOrEmpty(layout.SelectedScreenId) && selectedScreen.Id != layout.SelectedScreenId)
        {
            _trayIconManager.SetTooltip($"WidgX (fallback: {layout.SelectedScreenId} disconnected, using {selectedScreen.FriendlyName})");
        }
        else
        {
            _trayIconManager.SetTooltip("WidgX");
        }

        _trayIconManager.Show();

        // Open the designer right away when requested (e.g. straight after install).
        if (WidgX.Startup.LaunchArgs.IsOpenDesigner(e.Args))
        {
            OpenDesigner();
        }

        // Quietly check for updates on startup; only notify if one is available.
        _ = CheckForUpdatesAsync(manual: false);
    }

    private void OpenDesigner()
    {
        // Keep a single designer instance; bring it forward if already open.
        if (_designerWindow != null)
        {
            _designerWindow.Activate();
            return;
        }

        var currentLayout = LayoutStore.Load(AppPaths.LayoutFilePath);
        _designerWindow = new DesignerWindow(currentLayout, savedLayout =>
        {
            LayoutStore.Save(AppPaths.LayoutFilePath, savedLayout);
            _overlayWindow!.ReloadLayout(savedLayout);

            // Move the overlay onto the (possibly changed) selected monitor.
            var screens = ScreenResolver.GetAllScreens();
            var selected = ScreenResolver.ResolveSelected(savedLayout.SelectedScreenId, screens);
            _overlayWindow.MoveToScreen(selected.Bounds);
        });
        _designerWindow.Closed += (_, _) => _designerWindow = null;
        _designerWindow.Show();
        _designerWindow.Activate();
    }

    private async Task CheckForUpdatesAsync(bool manual)
    {
        var current = typeof(App).Assembly.GetName().Version ?? new Version(1, 0, 0);
        var info = await new UpdateService().CheckAsync(current);

        if (info == null)
        {
            if (manual) _trayIconManager?.ShowBalloon("WidgX", "Couldn't check for updates. Try again later.");
            return;
        }

        if (info.IsAvailable)
        {
            _trayIconManager?.ShowBalloon(
                "WidgX update available",
                $"Version {info.LatestTag} is available. Click to download.",
                () => OpenUrl(info.Url));
        }
        else if (manual)
        {
            _trayIconManager?.ShowBalloon("WidgX", "You're running the latest version.");
        }
    }

    private async Task RunStandaloneUpdateCheckAsync()
    {
        var current = typeof(App).Assembly.GetName().Version ?? new Version(1, 0, 0);
        var info = await new UpdateService().CheckAsync(current);

        if (info == null)
        {
            MessageBox.Show("Couldn't check for updates. Please try again later.", "WidgX",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else if (info.IsAvailable)
        {
            var result = MessageBox.Show(
                $"WidgX {info.LatestTag} is available (you have {current.Major}.{current.Minor}.{current.Build}).\n\nOpen the download page?",
                "WidgX update available", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes) OpenUrl(info.Url);
        }
        else
        {
            MessageBox.Show("You're running the latest version of WidgX.", "WidgX",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        Shutdown();
    }

    private static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Ignore: best-effort open.
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIconManager?.Dispose();
        base.OnExit(e);
    }
}

