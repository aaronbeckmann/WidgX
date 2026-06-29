using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace WidgX.Overlay;

public class ScreenInfo
{
    public string Id { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public Rect Bounds { get; set; }
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
            Bounds = new Rect(screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height),
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
