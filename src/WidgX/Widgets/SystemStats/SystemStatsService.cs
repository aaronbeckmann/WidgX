using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WidgX.Widgets.SystemStats;

public class SystemStatsService : IDisposable
{
    private PerformanceCounter? _cpuCounter;

    // GPU "Utilization Percentage" counters are rate counters: a freshly created
    // counter always reads 0 on its first sample. We keep them alive across polls
    // (keyed by instance name) so subsequent reads report real utilization.
    private readonly Dictionary<string, PerformanceCounter> _gpuCounters = new();
    private readonly DxgiVideoMemory _videoMemory = new();
    private ulong? _totalVramBytes;

    public static double ComputeRamUsagePercent(ulong totalBytes, ulong availableBytes)
    {
        if (totalBytes == 0) return 0;
        var usedBytes = totalBytes - availableBytes;
        return (double)usedBytes / totalBytes * 100.0;
    }

    public static double AggregateGpuUsage(IEnumerable<double> perEngineInstanceUtilization)
    {
        var sum = perEngineInstanceUtilization.Sum();
        return sum > 100.0 ? 100.0 : sum;
    }

    public static double VramPercent(ulong usedBytes, ulong totalBytes)
    {
        if (totalBytes == 0) return 0;
        var percent = (double)usedBytes / totalBytes * 100.0;
        if (percent < 0) return 0;
        return percent > 100.0 ? 100.0 : percent;
    }

    public double GetCpuUsagePercent()
    {
        _cpuCounter ??= new PerformanceCounter("Processor", "% Processor Time", "_Total");
        return _cpuCounter.NextValue();
    }

    public double GetRamUsagePercent()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!SystemStatsNativeMethods.GlobalMemoryStatusEx(ref status))
        {
            return 0;
        }
        return ComputeRamUsagePercent(status.ullTotalPhys, status.ullAvailPhys);
    }

    public double GetGpuUsagePercent()
    {
        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var liveInstances = new HashSet<string>();

            foreach (var instanceName in category.GetInstanceNames())
            {
                if (!instanceName.Contains("engtype_")) continue;
                liveInstances.Add(instanceName);

                if (_gpuCounters.ContainsKey(instanceName)) continue;

                foreach (var counter in category.GetCounters(instanceName))
                {
                    if (counter.CounterName == "Utilization Percentage" && !_gpuCounters.ContainsKey(instanceName))
                    {
                        _gpuCounters[instanceName] = counter;
                        try { counter.NextValue(); } catch { } // prime the rate counter
                    }
                    else
                    {
                        counter.Dispose();
                    }
                }
            }

            // Drop counters for engine instances that have gone away.
            foreach (var dead in _gpuCounters.Keys.Where(k => !liveInstances.Contains(k)).ToList())
            {
                _gpuCounters[dead].Dispose();
                _gpuCounters.Remove(dead);
            }

            var values = new List<double>();
            foreach (var counter in _gpuCounters.Values)
            {
                try { values.Add(counter.NextValue()); } catch { }
            }

            return AggregateGpuUsage(values);
        }
        catch (System.Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// System-wide dedicated VRAM in use as a percentage of the discrete GPU's
    /// total dedicated memory, or null if it can't be determined. Total comes
    /// from DXGI; used bytes come from the "GPU Adapter Memory" perf counter
    /// (system-wide, unlike DXGI's per-process QueryVideoMemoryInfo).
    /// </summary>
    public double? GetVramUsagePercent()
    {
        try
        {
            _totalVramBytes ??= _videoMemory.GetTotalDedicatedBytes();
            if (_totalVramBytes is null or 0) return null;

            var category = new PerformanceCounterCategory("GPU Adapter Memory");
            double usedBytes = 0;
            var any = false;

            foreach (var instance in category.GetInstanceNames())
            {
                foreach (var counter in category.GetCounters(instance))
                {
                    using (counter)
                    {
                        if (counter.CounterName == "Dedicated Usage")
                        {
                            usedBytes += counter.NextValue();
                            any = true;
                        }
                    }
                }
            }

            return any ? VramPercent((ulong)usedBytes, _totalVramBytes.Value) : null;
        }
        catch (System.Exception)
        {
            return null;
        }
    }

    public void Dispose()
    {
        _cpuCounter?.Dispose();
        _cpuCounter = null;

        foreach (var counter in _gpuCounters.Values)
        {
            counter.Dispose();
        }
        _gpuCounters.Clear();
    }
}
