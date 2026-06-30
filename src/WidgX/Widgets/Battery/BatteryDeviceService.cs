using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace WidgX.Widgets.Battery;

public record BatteryDevice(string Name, int Percent);

/// <summary>
/// Reports battery levels for any device Windows knows the level of — Bluetooth
/// audio, wireless controllers, and USB/HID peripherals alike — by reading the
/// battery DEVPKEY from the PnP device objects. No elevation required; degrades
/// gracefully to an empty list.
/// </summary>
public class BatteryDeviceService
{
    private const string BatteryKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";

    public async Task<List<BatteryDevice>> GetDevicesAsync()
    {
        // Highest reported level per (cleaned) device name, so a device reported
        // under multiple profiles appears once.
        var byName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var devices = await DeviceInformation.FindAllAsync(
                string.Empty, new[] { BatteryKey }, DeviceInformationKind.Device);

            foreach (var device in devices)
            {
                if (string.IsNullOrWhiteSpace(device.Name)) continue;
                if (!device.Properties.TryGetValue(BatteryKey, out var raw) || raw == null) continue;

                int percent;
                try { percent = Convert.ToInt32(raw); }
                catch { continue; }
                if (percent is < 0 or > 100) continue;

                var name = BatteryNames.Clean(device.Name);
                if (name.Length == 0) continue;

                if (!byName.TryGetValue(name, out var existing) || percent > existing)
                {
                    byName[name] = percent;
                }
            }
        }
        catch (Exception)
        {
            // Enumeration unavailable: return what we have.
        }

        return byName
            .Select(kv => new BatteryDevice(kv.Key, kv.Value))
            .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
