namespace WidgX.Widgets.Bluetooth;

/// <summary>
/// Pure formatting of a Bluetooth device's power state for display.
/// </summary>
public static class BluetoothStatusFormatter
{
    /// <summary>
    /// "✕" when disconnected; otherwise the battery percentage (with a "⚡"
    /// suffix while charging), or "✓" when connected but the device doesn't
    /// report a battery level.
    /// </summary>
    public static string Format(bool connected, int? batteryPercent, bool charging)
    {
        if (!connected) return "✕";
        if (batteryPercent == null) return "✓";

        var suffix = charging ? " ⚡" : string.Empty;
        return $"{batteryPercent.Value}%{suffix}";
    }
}
