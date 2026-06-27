using WidgX.Models;

namespace WidgX.Widgets;

public interface IWidget
{
    System.Windows.Controls.UserControl View { get; }
    void Configure(WidgetInstance config);
    void StartUpdates();
    void StopUpdates();
}
