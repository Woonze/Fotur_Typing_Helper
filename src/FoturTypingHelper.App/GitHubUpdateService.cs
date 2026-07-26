using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using FoturTypingHelper.Core;

namespace FoturTypingHelper.App;

internal sealed class GitHubUpdateService
{
    internal enum PlatformKind { WindowsX64, MacArm64, MacX64, LinuxX64, Unsupported }

    internal sealed record UpdateResult(string Message, bool Restarting = false);
    internal sealed record ReleaseSelection(
        Version Version,
        string? AssetName,
        string? AssetUrl,
        string? ChecksumUrl,
        PlatformKind Platform);

    internal const string LatestRelease = "https://api.github.com/repos/Woonze/Fotur_Typing_Helper/releases/latest";
    private readonly HttpClient _http;
    private readonly Version _localVersion;
    private readonly PlatformKind _platform;

    public GitHubUpdateService()
        : this(CreateClient(), Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0), DetectPlatform())
    {
    }

    internal GitHubUpdateService(HttpClient http, Version localVersion, PlatformKind platform = PlatformKind.WindowsX64)
    {
        _http = http;
        _localVersion = localVersion;
        _platform = platform;
    }

    public async Task<UpdateResult?> CheckAndInstallAsync(AppSettings settings, CancellationToken token = default)
    {
        if (!settings.AutoUpdateEnabled)
            return new($"Автообновление выключено · версия {_localVersion.ToString(3)}");

        try
        {
            using var response = await _http.GetAsync(LatestRelease, token);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new($"GitHub Releases пока не опубликованы · установлена версия {_localVersion.ToString(3)}");
            if (!response.IsSuccessStatusCode)
                return new($"Не удалось проверить обновления: GitHub вернул {(int)response.StatusCode} {response.ReasonPhrase}");

            var payload = await response.Content.ReadAsStringAsync(token);
            var release = ParseRelease(payload, _platform);
            if (release.Version <= _localVersion)
                return new($"Установлена актуальная версия {_localVersion.ToString(3)} · GitHub: {release.Version.ToString(3)}");
            if (release.Platform == PlatformKind.Unsupported)
                return new($"Доступна версия {release.Version.ToString(3)}, но автообновление этой платформы пока не поддерживается");
            if (release.AssetName is null || release.AssetUrl is null || release.ChecksumUrl is null)
                return new($"Доступна версия {release.Version.ToString(3)}, но пакет для этой платформы отсутствует в релизе");

            var name = release.AssetName;
            var temp = Path.Combine(Path.GetTempPath(), name);
            await Download(release.AssetUrl, temp, token);
            var checksums = await _http.GetStringAsync(release.ChecksumUrl, token);
            var expected = checksums.Split('\n')
                .FirstOrDefault(line => line.TrimEnd().EndsWith(name, StringComparison.Ordinal))?
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            await using var packageStream = File.OpenRead(temp);
            var actual = Convert.ToHexString(await SHA256.HashDataAsync(packageStream, token));
            if (expected is null || !actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Контрольная сумма обновления не совпадает.");

            var install = Install(temp, release.Version.ToString(3), release.Platform);
            return install ?? new($"Обновление {release.Version.ToString(3)} проверено по SHA-256 и устанавливается", true);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return new("Проверка обновлений прервана по таймауту");
        }
        catch (HttpRequestException ex)
        {
            DiagnosticLog.Write("AutoUpdateNetwork", ex);
            return new("Не удалось проверить обновления: нет соединения с GitHub");
        }
        catch (JsonException ex)
        {
            DiagnosticLog.Write("AutoUpdateJson", ex);
            return new("Не удалось проверить обновления: GitHub вернул неожиданный формат релиза");
        }
    }

    internal static ReleaseSelection ParseRelease(string payload, PlatformKind platform)
    {
        using var json = JsonDocument.Parse(payload);
        var tag = json.RootElement.GetProperty("tag_name").GetString()?.TrimStart('v')
            ?? throw new InvalidDataException("GitHub Release не содержит tag_name.");
        if (!Version.TryParse(tag, out var remote))
            throw new InvalidDataException($"Некорректная версия GitHub Release: {tag}");

        var (suffix, checksumName) = platform switch
        {
            PlatformKind.WindowsX64 => ("win-x64.exe", "SHA256SUMS.txt"),
            PlatformKind.MacArm64 => ("macos-arm64.zip", "SHA256SUMS-macos-arm64.txt"),
            PlatformKind.MacX64 => ("macos-x64.zip", "SHA256SUMS-macos-x64.txt"),
            PlatformKind.LinuxX64 => ("linux-x64.tar.gz", "SHA256SUMS-linux-x64.txt"),
            _ => ("", "")
        };
        if (platform == PlatformKind.Unsupported)
            return new(remote, null, null, null, platform);

        var assets = json.RootElement.GetProperty("assets").EnumerateArray().ToArray();
        var asset = assets.FirstOrDefault(item =>
            item.GetProperty("name").GetString()?.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) == true);
        var sums = assets.FirstOrDefault(item =>
            item.GetProperty("name").GetString()?.Equals(checksumName, StringComparison.Ordinal) == true);
        if (sums.ValueKind == JsonValueKind.Undefined)
            sums = assets.FirstOrDefault(item =>
                item.GetProperty("name").GetString()?.Equals("SHA256SUMS.txt", StringComparison.Ordinal) == true);

        return new(
            remote,
            asset.ValueKind == JsonValueKind.Undefined ? null : asset.GetProperty("name").GetString(),
            asset.ValueKind == JsonValueKind.Undefined ? null : asset.GetProperty("browser_download_url").GetString(),
            sums.ValueKind == JsonValueKind.Undefined ? null : sums.GetProperty("browser_download_url").GetString(),
            platform);
    }

    private static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Fotur-Typing-Helper-Updater/1.3");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return http;
    }

    private async Task Download(string url, string path, CancellationToken token)
    {
        await using var source = await _http.GetStreamAsync(url, token);
        await using var target = File.Create(path);
        await source.CopyToAsync(target, token);
    }

    private static UpdateResult? Install(string package, string version, PlatformKind platform)
    {
        if (platform == PlatformKind.WindowsX64)
        {
            Process.Start(new ProcessStartInfo(package, "/VERYSILENT /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS") { UseShellExecute = true });
            return null;
        }
        if (platform is PlatformKind.MacArm64 or PlatformKind.MacX64)
        {
            InstallMac(package, version);
            return null;
        }

        return new($"Обновление {version} проверено по SHA-256 и скачано: {package}. На Linux установите архив вручную поверх текущей папки.");
    }

    private static void InstallMac(string package, string version)
    {
        var extract = Path.Combine(Path.GetTempPath(), $"fotur-update-{version}");
        if (Directory.Exists(extract)) Directory.Delete(extract, true);
        Directory.CreateDirectory(extract);
        ZipFile.ExtractToDirectory(package, extract, true);
        var newBundle = Directory.GetDirectories(extract, "*.app").Single();
        var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Не найден путь приложения.");
        var marker = executable.IndexOf(".app/Contents/MacOS", StringComparison.Ordinal);
        if (marker < 0) throw new InvalidOperationException("Автообновление доступно только для установленного .app bundle.");
        var bundle = executable[..(marker + 4)];
        var script = Path.Combine(extract, "install-update.sh");
        var current = ShellQuote(bundle);
        var next = ShellQuote(bundle + ".new");
        var old = ShellQuote(bundle + ".old");
        File.WriteAllText(script, $"#!/bin/sh\nset -e\nsleep 2\nrm -rf {next} {old}\n/usr/bin/ditto {ShellQuote(newBundle)} {next}\nmv {current} {old}\nif mv {next} {current}; then\n  open {current}\n  rm -rf {old}\nelse\n  mv {old} {current}\n  exit 1\nfi\n");
        Process.Start(new ProcessStartInfo("/bin/sh", script) { UseShellExecute = false });
    }

    private static PlatformKind DetectPlatform()
    {
        var arch = RuntimeInformation.ProcessArchitecture;
        if (OperatingSystem.IsWindows() && arch == Architecture.X64) return PlatformKind.WindowsX64;
        if (OperatingSystem.IsMacOS() && arch == Architecture.Arm64) return PlatformKind.MacArm64;
        if (OperatingSystem.IsMacOS() && arch == Architecture.X64) return PlatformKind.MacX64;
        if (OperatingSystem.IsLinux() && arch == Architecture.X64) return PlatformKind.LinuxX64;
        return PlatformKind.Unsupported;
    }

    private static string ShellQuote(string value) => "'" + value.Replace("'", "'\\''", StringComparison.Ordinal) + "'";
}
