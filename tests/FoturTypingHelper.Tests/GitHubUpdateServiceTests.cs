using System.Runtime.InteropServices;
using FoturTypingHelper.App;
using FoturTypingHelper.Core;

namespace FoturTypingHelper.Tests;

public sealed class GitHubUpdateServiceTests
{
    private const string ReleaseJson =
        """
        {
          "tag_name": "v1.1.0",
          "assets": [
            {
              "name": "FoturTypingHelper-Setup-1.1.0-win-x64.exe",
              "browser_download_url": "https://example.test/windows"
            },
            {
              "name": "FoturTypingHelper-1.1.0-macos-arm64.zip",
              "browser_download_url": "https://example.test/macos-arm64"
            },
            {
              "name": "FoturTypingHelper-1.1.0-macos-x64.zip",
              "browser_download_url": "https://example.test/macos-x64"
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
            }
          ]
        }
        """;

    [Theory]
    [InlineData(true, Architecture.X64, "FoturTypingHelper-Setup-1.1.0-win-x64.exe", "https://example.test/windows-sums")]
    [InlineData(false, Architecture.Arm64, "FoturTypingHelper-1.1.0-macos-arm64.zip", "https://example.test/arm64-sums")]
    [InlineData(false, Architecture.X64, "FoturTypingHelper-1.1.0-macos-x64.zip", "https://example.test/x64-sums")]
    public void ReleaseAssetsAreSelectedForTheCurrentPlatform(
        bool windows,
        Architecture architecture,
        string expectedAsset,
        string expectedChecksum)
    {
        var selection = GitHubUpdateService.ParseRelease(ReleaseJson, windows, architecture);

        Assert.Equal(new Version(1, 1, 0), selection.Version);
        Assert.Equal(expectedAsset, selection.AssetName);
        Assert.Equal(expectedChecksum, selection.ChecksumUrl);
    }

    [Fact]
    public async Task CurrentReleaseReportsInstalledAndGitHubVersions()
    {
        using var http = new HttpClient(new JsonHandler(ReleaseJson));
        var service = new GitHubUpdateService(http, new Version(1, 1, 0));

        var result = await service.CheckAndInstallAsync(new AppSettings { AutoUpdateEnabled = true });

        Assert.NotNull(result);
        Assert.False(result.Restarting);
        Assert.Equal("Установлена актуальная версия 1.1.0 · GitHub: 1.1.0", result.Message);
    }

    [Fact]
    public async Task DisabledUpdaterStillReportsTheInstalledVersion()
    {
        using var http = new HttpClient(new JsonHandler(ReleaseJson));
        var service = new GitHubUpdateService(http, new Version(1, 1, 0));

        var result = await service.CheckAndInstallAsync(new AppSettings { AutoUpdateEnabled = false });

        Assert.Equal("Автообновление выключено · версия 1.1.0", result?.Message);
    }

    private sealed class JsonHandler(string payload) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(payload)
            });
    }
}
