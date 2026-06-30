using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace WidgX.Widgets.Bluetooth;

public record BluetoothDeviceEntry(string Id, string Name);

public class BluetoothStatus
{
    public bool Connected { get; init; }
    public int? BatteryPercent { get; init; }
    public bool Charging { get; init; }
}

public record BluetoothDeviceStatus(string Name, BluetoothStatus Status);

/// <summary>
/// Enumerates paired Bluetooth devices (classic and LE) and reads their
/// connection/battery state. Uses the pairing-state device selectors, whose
/// association endpoints carry an accurate IsConnected flag. No elevation
/// required; all calls degrade gracefully on failure.
/// </summary>
public class BluetoothDeviceService
{
    private const string BatteryKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";
    private const string AepIsConnectedKey = "System.Devices.Aep.IsConnected";
    private const string ConnectedKey = "{83DA6326-97A6-4088-9453-A1923F573B29} 15";

    private static readonly string[] RequestedProperties = { BatteryKey, AepIsConnectedKey, ConnectedKey };

    public async Task<List<BluetoothDeviceEntry>> GetPairedDevicesAsync()
    {
        var entries = new List<BluetoothDeviceEntry>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var device in await EnumeratePairedAsync())
        {
            if (string.IsNullOrWhiteSpace(device.Name) || !seenNames.Add(device.Name)) continue;
            entries.Add(new BluetoothDeviceEntry(device.Id, device.Name));
        }
        return entries;
    }

    public async Task<BluetoothStatus> GetStatusAsync(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return new BluetoothStatus();

        foreach (var device in await EnumeratePairedAsync())
        {
            if (device.Id == deviceId) return BuildStatus(device);
        }
        return new BluetoothStatus();
    }

    /// <summary>Connected paired devices and their battery levels.</summary>
    public async Task<List<BluetoothDeviceStatus>> GetConnectedStatusesAsync()
    {
        var result = new List<BluetoothDeviceStatus>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var device in await EnumeratePairedAsync())
        {
            if (string.IsNullOrWhiteSpace(device.Name) || !seenNames.Add(device.Name)) continue;

            var status = BuildStatus(device);
            if (status.Connected)
            {
                result.Add(new BluetoothDeviceStatus(device.Name, status));
            }
        }
        return result;
    }

    private static async Task<List<DeviceInformation>> EnumeratePairedAsync()
    {
        var all = new List<DeviceInformation>();
        string[] selectors =
        {
            BluetoothDevice.GetDeviceSelectorFromPairingState(true),
            BluetoothLEDevice.GetDeviceSelectorFromPairingState(true)
        };

        foreach (var selector in selectors)
        {
            try
            {
                var found = await DeviceInformation.FindAllAsync(selector, RequestedProperties);
                all.AddRange(found);
            }
            catch (Exception)
            {
                // Radio off / API unavailable: skip this selector.
            }
        }
        return all;
    }

    private static BluetoothStatus BuildStatus(DeviceInformation device)
    {
        var battery = ReadBattery(device);
        var connected = ReadBool(device, AepIsConnectedKey)
                        || ReadBool(device, ConnectedKey)
                        || battery.HasValue;

        return new BluetoothStatus { Connected = connected, BatteryPercent = battery, Charging = false };
    }

    private static bool ReadBool(DeviceInformation device, string key)
        => device.Properties.TryGetValue(key, out var value) && value is bool b && b;

    private static int? ReadBattery(DeviceInformation device)
    {
        if (device.Properties.TryGetValue(BatteryKey, out var raw) && raw != null)
        {
            try { return Convert.ToInt32(raw); }
            catch { return null; }
        }
        return null;
    }
}
