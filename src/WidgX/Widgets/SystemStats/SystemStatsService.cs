using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WidgX.Widgets.SystemStats;

public class SystemStatsService
{
    private PerformanceCounter? _cpuCounter;

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
            var instanceNames = category.GetInstanceNames();
            var values = new List<double>();

            foreach (var instanceName in instanceNames)
            {
                foreach (var counter in category.GetCounters(instanceName))
                {
                    if (counter.CounterName == "Utilization Percentage")
                    {
                        values.Add(counter.NextValue());
                    }
                }
            }

            return AggregateGpuUsage(values);
        }
        catch (System.Exception)
        {
            return 0;
        }
    }
}
