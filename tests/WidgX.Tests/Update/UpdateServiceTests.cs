using System;
using WidgX.Update;
using Xunit;

namespace WidgX.Tests.Update;

public class UpdateServiceTests
{
    [Theory]
    [InlineData("v1.2.0", 1, 2, 0)]
    [InlineData("1.2.0", 1, 2, 0)]
    [InlineData("V2.0", 2, 0, 0)]
    public void ParseTag_ParsesVersions(string tag, int major, int minor, int build)
    {
        Assert.Equal(new Version(major, minor, build), UpdateService.ParseTag(tag));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("vX")]
    [InlineData("latest")]
    public void ParseTag_RejectsInvalid(string? tag)
    {
        Assert.Null(UpdateService.ParseTag(tag));
    }

    [Fact]
    public void IsUpdateAvailable_NewerTag_True()
    {
        Assert.True(UpdateService.IsUpdateAvailable("v1.1.0", new Version(1, 0, 0)));
        Assert.True(UpdateService.IsUpdateAvailable("v2.0.0", new Version(1, 9, 9)));
    }

    [Fact]
    public void IsUpdateAvailable_SameOrOlder_False()
    {
        Assert.False(UpdateService.IsUpdateAvailable("v1.0.0", new Version(1, 0, 0, 0)));
        Assert.False(UpdateService.IsUpdateAvailable("v0.9.0", new Version(1, 0, 0)));
    }

    [Fact]
    public void IsUpdateAvailable_InvalidTag_False()
    {
        Assert.False(UpdateService.IsUpdateAvailable("nightly", new Version(1, 0, 0)));
    }
}
