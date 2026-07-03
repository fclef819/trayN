using System.Text.Json.Serialization;

namespace TrayN;

internal sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubAsset> Assets { get; set; } = [];
}

internal sealed class GitHubAsset
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }
}

internal sealed class UpdateInfo
{
    public required Version CurrentVersion { get; init; }
    public required Version NewVersion { get; init; }
    public required string ReleaseNotes { get; init; }
    public required Uri ExeUri { get; init; }
    public required Uri Sha256Uri { get; init; }
}
