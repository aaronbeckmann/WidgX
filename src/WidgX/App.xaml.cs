using System.Windows;
using WidgX.Designer;
using WidgX.Overlay;
using WidgX.Persistence;
using WidgX.Tray;
using WidgX.Widgets.Calendar;
using WidgX.Widgets.Clock;
using WidgX.Widgets.SystemStats;
using WidgX.Widgets.Todos;
using WidgX.Widgets.Uptime;
using Application = System.Windows.Application;

namespace WidgX;

public partial class App : Application
{
    private OverlayWindow? _overlayWindow;
    private TrayIconManager? _trayIconManager;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ClockWidgetRegistration.Register();
        CalendarWidgetRegistration.Register();
        TodoWidgetRegistration.Register();
        UptimeWidgetRegistration.Register();
        SystemStatsWidgetRegistration.Register();

        var layout = LayoutStore.Load(AppPaths.LayoutFilePath);
        var screens = ScreenResolver.GetAllScreens();
        var selectedScreen = ScreenResolver.ResolveSelected(layout.SelectedScreenId, screens);

        _overlayWindow = new OverlayWindow(selectedScreen.BoundsDip);
        _overlayWindow.Show();
        _overlayWindow.ReloadLayout(layout);

        _trayIconManager = new TrayIconManager(
            onEditLayout: () =>
            {
                var currentLayout = LayoutStore.Load(AppPaths.LayoutFilePath);
                var designer = new DesignerWindow(currentLayout, savedLayout =>
                {
                    LayoutStore.Save(AppPaths.LayoutFilePath, savedLayout);
                    _overlayWindow!.ReloadLayout(savedLayout);
                });
                designer.Show();
            },
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

