using System.Collections.Generic;
using WidgX.Widgets.SystemStats;
using Xunit;

namespace WidgX.Tests.Widgets.SystemStats;

public class SystemStatsCalculationTests
{
    [Fact]
    public void ComputeRamUsagePercent_Computes_Correctly()
    {
        var result = SystemStatsService.ComputeRamUsagePercent(totalBytes: 16_000_000_000, availableBytes: 4_000_000_000);
        Assert.Equal(75.0, result, precision: 1);
    }

    [Fact]
    public void AggregateGpuUsage_Sums_Engine_Instances()
    {
        var result = SystemStatsService.AggregateGpuUsage(new List<double> { 10.0, 15.0, 5.0 });
        Assert.Equal(30.0, result, precision: 1);
    }

    [Fact]
    public void AggregateGpuUsage_Clamps_At_100()
    {
        var result = SystemStatsService.AggregateGpuUsage(new List<double> { 60.0, 60.0 });
        Assert.Equal(100.0, result, precision: 1);
    }
}
