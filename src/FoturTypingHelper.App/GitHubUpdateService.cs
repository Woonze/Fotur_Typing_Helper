using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Net.Http.Headers;
using System.Text.Json;
using FoturTypingHelper.Core;

namespace FoturTypingHelper.App;

internal sealed class GitHubUpdateService
{
    internal sealed record UpdateResult(string Message, bool Restarting = false);
    internal sealed record ReleaseSelection(
        Version Version,
        string? AssetName,
        string? AssetUrl,
        string? ChecksumUrl);

    internal const string LatestRelease = "https://api.github.com/repos/Woonze/Fotur_Typing_Helper/releases/latest";
    private readonly HttpClient _http;
    private readonly Version _localVersion;

    public GitHubUpdateService()
        : this(CreateClient(), Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0))
    {
    }

    internal GitHubUpdateService(HttpClient http, Version localVersion)
    {
        _http = http;
        _localVersion = localVersion;
    }

    public async Task<UpdateResult?> CheckAndInstallAsync(AppSettings settings, CancellationToken token = default)
    {
        if (!settings.AutoUpdateEnabled)
            return new($"Автообновление выключено · версия {_localVersion.ToString(3)}");

        using var response = await _http.GetAsync(LatestRelease, token);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync(token);
        var release = ParseRelease(
            payload,
            OperatingSystem.IsWindows(),
            RuntimeInformation.ProcessArchitecture);
        if (release.Version <= _localVersion)
            return new($"Установлена актуальная версия {_localVersion.ToString(3)} · GitHub: {release.Version.ToString(3)}");
        if (release.AssetName is null || release.AssetUrl is null || release.ChecksumUrl is null)
            return new($"Доступна версия {release.Version.ToString(3)}, но пакет этой платформы отсутствует");

        var name = release.AssetName;
        var temp = Path.Combine(Path.GetTempPath(), name);
        await Download(release.AssetUrl, temp, token);
        var checksums = await _http.GetStringAsync(release.ChecksumUrl, token);
        var expected = checksums.Split('\n').FirstOrDefault(line => line.EndsWith(name, StringComparison.Ordinal))?.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        await using var packageStream = File.OpenRead(temp);
        var actual = Convert.ToHexString(await SHA256.HashDataAsync(packageStream, token));
        if (expected is null || !actual.Equals(expected, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Контрольная сумма обновления не совпадает.");
        Install(temp, release.Version.ToString(3));
        return new($"Обновление {release.Version.ToString(3)} проверено по SHA-256 и устанавливается", true);
    }

    internal static ReleaseSelection ParseRelease(string payload, bool windows, Architecture architecture)
    {
        using var json = JsonDocument.Parse(payload);
        var tag = json.RootElement.GetProperty("tag_name").GetString()?.TrimStart('v')
            ?? throw new InvalidDataException("GitHub Release не содержит tag_name.");
        if (!Version.TryParse(tag, out var remote))
            throw new InvalidDataException($"Некорректная версия GitHub Release: {tag}");

        var suffix = windows ? "win-x64.exe"
            : architecture == Architecture.Arm64 ? "macos-arm64.zip" : "macos-x64.zip";
        var checksumName = windows ? "SHA256SUMS.txt"
            : architecture == Architecture.Arm64 ? "SHA256SUMS-macos-arm64.txt" : "SHA256SUMS-macos-x64.txt";
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
            sums.ValueKind == JsonValueKind.Undefined ? null : sums.GetProperty("browser_download_url").GetString());
    }

    private static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Fotur-Typing-Helper-Updater/1.1");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return http;
    }

    private async Task Download(string url, string path, CancellationToken token)
    {
        await using var source = await _http.GetStreamAsync(url, token); await using var target = File.Create(path); await source.CopyToAsync(target, token);
    }

    private static void Install(string package, string version)
    {
        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo(package, "/VERYSILENT /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS") { UseShellExecute = true });
            return;
        }
        var extract = Path.Combine(Path.GetTempPath(), $"fotur-update-{version}");
        if (Directory.Exists(extract)) Directory.Delete(extract, true);
        Directory.CreateDirectory(extract);
        ZipFile.ExtractToDirectory(package, extract, true);
        var newBundle = Directory.GetDirectories(extract, "*.app").Single();
        var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Не найден путь приложения.");
        var marker = executable.IndexOf(".app/Contents/MacOS", StringComparison.Ordinal);
        if (marker < 0) throw new InvalidOperationException("Автообновление доступно для установленного .app bundle.");
        var bundle = executable[..(marker + 4)];
        var script = Path.Combine(extract, "install-update.sh");
        var current = ShellQuote(bundle);
        var next = ShellQuote(bundle + ".new");
        var old = ShellQuote(bundle + ".old");
        File.WriteAllText(script, $"#!/bin/sh\nset -e\nsleep 2\nrm -rf {next} {old}\n/usr/bin/ditto {ShellQuote(newBundle)} {next}\nmv {current} {old}\nif mv {next} {current}; then\n  open {current}\n  rm -rf {old}\nelse\n  mv {old} {current}\n  exit 1\nfi\n");
        Process.Start(new ProcessStartInfo("/bin/sh", script) { UseShellExecute = false });
    }

    private static string ShellQuote(string value) => "'" + value.Replace("'", "'\\''", StringComparison.Ordinal) + "'";
}
