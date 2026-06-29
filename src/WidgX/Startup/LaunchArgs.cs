using System.Linq;

namespace WidgX.Startup;

public static class LaunchArgs
{
    public static bool IsBackgroundLaunch(string[] args)
    {
        return args.Contains("--background");
    }
}
