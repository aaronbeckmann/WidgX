using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace WidgX.Widgets.Bluetooth;

public record BluetoothDeviceEntry(string Id, string Name);

public class BluetoothStatus
{
    public bool Connected { get; init; }
    public int? BatteryPercent { get; init; }
    public bool Charging { get; init; }
}

/// <summary>
/// Enumerates paired Bluetooth devices and reads their connection/battery state
/// via the Windows device-enumeration (AEP) APIs. No elevation required; all
/// calls degrade gracefully on failure.
/// </summary>
public class BluetoothDeviceService
{
    // Well-known property keys exposed on Bluetooth association endpoints.
    private const string BatteryKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";
    private const string ConnectedKey = "System.Devices.Aep.IsConnected";

    private const string PairedBluetoothSelector =
        "System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\"" +
        " AND System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True";

    private static readonly string[] RequestedProperties = { BatteryKey, ConnectedKey };

    public async Task<List<BluetoothDeviceEntry>> GetPairedDevicesAsync()
    {
        var devices = new List<BluetoothDeviceEntry>();
        try
        {
            var found = await DeviceInformation.FindAllAsync(
                PairedBluetoothSelector, RequestedProperties, DeviceInformationKind.AssociationEndpoint);

            foreach (var device in found)
            {
                if (!string.IsNullOrWhiteSpace(device.Name))
                {
                    devices.Add(new BluetoothDeviceEntry(device.Id, device.Name));
                }
            }
        }
        catch (Exception)
        {
            // Bluetooth radio off / API unavailable: return what we have.
        }
        return devices;
    }

    public async Task<BluetoothStatus> GetStatusAsync(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return new BluetoothStatus();

        try
        {
            var device = await DeviceInformation.CreateFromIdAsync(
                deviceId, RequestedProperties, DeviceInformationKind.AssociationEndpoint);

            var connected = device.Properties.TryGetValue(ConnectedKey, out var c) && c is bool b && b;

            int? battery = null;
            if (connected && device.Properties.TryGetValue(BatteryKey, out var raw) && raw != null)
            {
                try { battery = Convert.ToInt32(raw); }
                catch { battery = null; }
            }

            return new BluetoothStatus { Connected = connected, BatteryPercent = battery, Charging = false };
        }
        catch (Exception)
        {
            return new BluetoothStatus();
        }
    }
}
