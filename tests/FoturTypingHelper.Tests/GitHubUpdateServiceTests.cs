using FoturTypingHelper.App;

namespace FoturTypingHelper.Tests;

public sealed class GitHubUpdateServiceTests
{
    private const string ReleaseJson =
        """
        {
          "tag_name": "v1.3.1",
          "assets": [
            {
              "name": "FoturTypingHelper-Setup-1.3.1-win-x64.exe",
              "browser_download_url": "https://example.test/windows"
            },
            {
              "name": "FoturTypingHelper-1.3.1-macos-arm64.zip",
              "browser_download_url": "https://example.test/macos-arm64"
            },
            {
              "name": "FoturTypingHelper-1.3.1-macos-x64.zip",
              "browser_download_url": "https://example.test/macos-x64"
            },
            {
              "name": "FoturTypingHelper-1.3.1-linux-x64.tar.gz",
              "browser_download_url": "https://example.test/linux-x64"
            },
            {
              "name": "SHA256SUMS.txt",
              "browser_download_url": "https://example.test/windows-sums"
            },
            {
              "name": "SHA256SUMS-macos-arm64.txt",
              "browser_download_url": "https://example.test/arm64-sums"
            },
            {
              "name": "SHA256SUMS-macos-x64.txt",
              "browser_download_url": "https://example.test/x64-sums"
            },
            {
              "name": "SHA256SUMS-linux-x64.txt",
              "browser_download_url": "https://example.test/linux-sums"
            }
          ]
        }
        """;

    [Theory]
    [InlineData((int)GitHubUpdateService.PlatformKind.WindowsX64, "FoturTypingHelper-Setup-1.3.1-win-x64.exe", "https://example.test/windows-sums")]
    [InlineData((int)GitHubUpdateService.PlatformKind.MacArm64, "FoturTypingHelper-1.3.1-macos-arm64.zip", "https://example.test/arm64-sums")]
    [InlineData((int)GitHubUpdateService.PlatformKind.MacX64, "FoturTypingHelper-1.3.1-macos-x64.zip", "https://example.test/x64-sums")]
    [InlineData((int)GitHubUpdateService.PlatformKind.LinuxX64, "FoturTypingHelper-1.3.1-linux-x64.tar.gz", "https://example.test/linux-sums")]
    public void ReleaseAssetsAreSelectedForTheCurrentPlatform(
        int platform,
        string expectedAsset,
        string expectedChecksum)
    {
        var selection = GitHubUpdateService.ParseRelease(ReleaseJson, (GitHubUpdateService.PlatformKind)platform);

        Assert.Equal(new Version(1, 3, 1), selection.Version);
        Assert.Equal(expectedAsset, selection.AssetName);
        Assert.Equal(expectedChecksum, selection.ChecksumUrl);
    }

    [Fact]
    public async Task CurrentReleaseReportsInstalledAndGitHubVersions()
    {
        using var http = new HttpClient(new JsonHandler(ReleaseJson));
        var service = new GitHubUpdateService(http, new Version(1, 3, 1));

        var result = await service.CheckAndInstallAsync(new Core.AppSettings { AutoUpdateEnabled = true });

        Assert.NotNull(result);
        Assert.False(result.Restarting);
        Assert.Equal("Установлена актуальная версия 1.3.1 · GitHub: 1.3.1", result.Message);
    }

    [Fact]
    public async Task DisabledUpdaterStillReportsTheInstalledVersion()
    {
        using var http = new HttpClient(new JsonHandler(ReleaseJson));
        var service = new GitHubUpdateService(http, new Version(1, 3, 1));

        var result = await service.CheckAndInstallAsync(new Core.AppSettings { AutoUpdateEnabled = false });

        Assert.Equal("Автообновление выключено · версия 1.3.1", result?.Message);
    }

    [Fact]
    public async Task MissingLatestReleaseDoesNotThrow()
    {
        using var http = new HttpClient(new JsonHandler("", System.Net.HttpStatusCode.NotFound));
        var service = new GitHubUpdateService(http, new Version(1, 3, 1));

        var result = await service.CheckAndInstallAsync(new Core.AppSettings { AutoUpdateEnabled = true });

        Assert.Equal("GitHub Releases пока не опубликованы · установлена версия 1.3.1", result?.Message);
    }

    private sealed class JsonHandler(string payload, System.Net.HttpStatusCode status = System.Net.HttpStatusCode.OK) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(payload)
            });
    }
}
