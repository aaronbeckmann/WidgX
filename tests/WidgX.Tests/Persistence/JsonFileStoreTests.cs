using System;
using System.IO;
using WidgX.Persistence;
using Xunit;

namespace WidgX.Tests.Persistence;

public class JsonFileStoreTests : IDisposable
{
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");

    [Fact]
    public void Load_Returns_Fallback_When_File_Missing()
    {
        var result = JsonFileStore.Load(_tempFile, new TestPayload { Value = 42 });
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Save_Then_Load_RoundTrips()
    {
        var payload = new TestPayload { Value = 7 };
        JsonFileStore.Save(_tempFile, payload);

        var result = JsonFileStore.Load(_tempFile, new TestPayload { Value = -1 });
        Assert.Equal(7, result.Value);
    }

    [Fact]
    public void Load_Returns_Fallback_When_File_Is_Malformed()
    {
        File.WriteAllText(_tempFile, "{ not valid json");
        var result = JsonFileStore.Load(_tempFile, new TestPayload { Value = 99 });
        Assert.Equal(99, result.Value);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }

    public class TestPayload
    {
        public int Value { get; set; }
    }
}
