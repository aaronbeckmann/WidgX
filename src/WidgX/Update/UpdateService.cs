using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace WidgX.Update;

public record UpdateInfo(bool IsAvailable, string LatestTag, string Url);

/// <summary>
/// Checks GitHub for the latest published release and compares it to the running
/// version. The version comparison is pure and tested; the network call degrades
/// gracefully to null on any failure.
/// </summary>
public class UpdateService
{
    private const string LatestReleaseApi =
        "https://api.github.com/repos/aaronbeckmann/WidgX/releases/latest";

    private static readonly HttpClient Http = CreateClient();

    /// <summary>Parses a release tag like "v1.2.0" into a 3-part version, or null.</summary>
    public static Version? ParseTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;
        var text = tag.Trim().TrimStart('v', 'V');
        return Version.TryParse(text, out var version) ? Normalize(version) : null;
    }

    public static bool IsUpdateAvailable(string? latestTag, Version current)
    {
        var latest = ParseTag(latestTag);
        return latest != null && latest > Normalize(current);
    }

    public async Task<UpdateInfo?> CheckAsync(Version current)
    {
        try
        {
            var json = await Http.GetStringAsync(LatestReleaseApi);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() ?? string.Empty : string.Empty;
            var url = root.TryGetProperty("html_url", out var u) ? u.GetString() ?? string.Empty : string.Empty;

            return new UpdateInfo(IsUpdateAvailable(tag, current), tag, url);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static Version Normalize(Version v)
        => new(Math.Max(0, v.Major), Math.Max(0, v.Minor), Math.Max(0, v.Build));

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        // GitHub's API rejects requests without a User-Agent.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("WidgX-Updater");
        return client;
    }
}
