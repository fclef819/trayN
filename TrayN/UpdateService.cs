using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace TrayN;

internal sealed class UpdateService
{
    private const string GitHubOwner = "fclef819";
    private const string GitHubRepository = "trayN";
    private static readonly Uri LatestReleaseUri = new($"https://api.github.com/repos/{GitHubOwner}/{GitHubRepository}/releases/latest");

    private readonly HttpClient httpClient = new();

    public UpdateService()
    {
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("trayN/0.1.0");
        httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<UpdateInfo?> CheckLatestAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(LatestReleaseUri, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (release is null || release.Draft || release.Prerelease || string.IsNullOrWhiteSpace(release.TagName))
        {
            return null;
        }

        var newVersion = ParseVersion(release.TagName);
        var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
        if (newVersion is null || newVersion <= NormalizeVersion(currentVersion))
        {
            return null;
        }

        var exeAsset = release.Assets.FirstOrDefault(a => string.Equals(a.Name, "trayN.exe", StringComparison.OrdinalIgnoreCase));
        var hashAsset = release.Assets.FirstOrDefault(a => string.Equals(a.Name, "trayN.exe.sha256", StringComparison.OrdinalIgnoreCase));
        if (!Uri.TryCreate(exeAsset?.BrowserDownloadUrl, UriKind.Absolute, out var exeUri) ||
            !Uri.TryCreate(hashAsset?.BrowserDownloadUrl, UriKind.Absolute, out var shaUri))
        {
            return null;
        }

        return new UpdateInfo
        {
            CurrentVersion = NormalizeVersion(currentVersion),
            NewVersion = newVersion,
            ReleaseNotes = release.Body ?? string.Empty,
            ExeUri = exeUri,
            Sha256Uri = shaUri
        };
    }

    public async Task<string> DownloadAndVerifyAsync(UpdateInfo updateInfo, CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "trayN-update-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var exePath = Path.Combine(tempDirectory, "trayN.exe");
        var hashPath = Path.Combine(tempDirectory, "trayN.exe.sha256");

        try
        {
            await DownloadFileAsync(updateInfo.ExeUri, exePath, cancellationToken).ConfigureAwait(false);
            await DownloadFileAsync(updateInfo.Sha256Uri, hashPath, cancellationToken).ConfigureAwait(false);

            var expectedHash = ParseSha256File(await File.ReadAllTextAsync(hashPath, cancellationToken).ConfigureAwait(false));
            var actualHash = await ComputeSha256Async(exePath, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
            {
                Directory.Delete(tempDirectory, true);
                throw new InvalidOperationException("Downloaded trayN.exe did not match trayN.exe.sha256.");
            }

            return exePath;
        }
        catch
        {
            try
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
            catch
            {
            }

            throw;
        }
    }

    public static bool CanWriteApplicationDirectory(out string reason)
    {
        try
        {
            var appPath = Environment.ProcessPath;
            var directory = Path.GetDirectoryName(appPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                reason = "The application directory could not be resolved.";
                return false;
            }

            var probe = Path.Combine(directory, ".trayn-write-test-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            reason = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            reason = ex.Message;
            return false;
        }
    }

    public static bool TryStartUpdater(string downloadedExePath)
    {
        var currentExe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExe))
        {
            return false;
        }

        var updaterExe = Path.Combine(AppContext.BaseDirectory, "trayN.Updater.exe");
        if (!File.Exists(updaterExe))
        {
            MessageBox.Show("trayN.Updater.exe was not found next to trayN.exe.", "trayN", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = updaterExe,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("--pid");
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
        startInfo.ArgumentList.Add("--new-exe");
        startInfo.ArgumentList.Add(downloadedExePath);
        startInfo.ArgumentList.Add("--current-exe");
        startInfo.ArgumentList.Add(currentExe);

        Process.Start(startInfo);
        return true;
    }

    private async Task DownloadFileAsync(Uri uri, string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var file = File.Create(path);
        await stream.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
    }

    private static string ParseSha256File(string content)
    {
        var first = content.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (first is null || first.Length != 64 || first.Any(c => !Uri.IsHexDigit(c)))
        {
            throw new InvalidOperationException("Invalid trayN.exe.sha256 content.");
        }

        return first;
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }

    private static Version? ParseVersion(string tag)
    {
        if (!tag.StartsWith('v'))
        {
            return null;
        }

        return Version.TryParse(tag[1..], out var version) ? NormalizeVersion(version) : null;
    }

    private static Version NormalizeVersion(Version version) =>
        new(version.Major, version.Minor, Math.Max(0, version.Build));
}
