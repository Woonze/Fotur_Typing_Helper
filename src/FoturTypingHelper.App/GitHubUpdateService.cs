using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using FoturTypingHelper.Core;

namespace FoturTypingHelper.App;

internal sealed class GitHubUpdateService
{
    private const string LatestRelease = "https://api.github.com/repos/Woonze/Fotur_Typing_Helper/releases/latest";
    private readonly HttpClient _http = new() { DefaultRequestHeaders = { { "User-Agent", "Fotur-Typing-Helper-Updater" } } };

    public async Task<string?> CheckAndInstallAsync(AppSettings settings, CancellationToken token = default)
    {
        if (!settings.AutoUpdateEnabled) return null;
        using var response = await _http.GetAsync(LatestRelease, token); response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(token));
        var tag = json.RootElement.GetProperty("tag_name").GetString()?.TrimStart('v');
        if (!Version.TryParse(tag, out var remote)) return null;
        var local = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);
        if (remote <= local) return "Установлена актуальная версия";

        var suffix = OperatingSystem.IsWindows() ? "win-x64.exe"
            : RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "macos-arm64.zip" : "macos-x64.zip";
        var assets = json.RootElement.GetProperty("assets").EnumerateArray().ToArray();
        var asset = assets.FirstOrDefault(a => a.GetProperty("name").GetString()?.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) == true);
        var checksumName = OperatingSystem.IsWindows() ? "SHA256SUMS.txt"
            : RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "SHA256SUMS-macos-arm64.txt" : "SHA256SUMS-macos-x64.txt";
        var sums = assets.FirstOrDefault(a => a.GetProperty("name").GetString() == checksumName);
        if (sums.ValueKind == JsonValueKind.Undefined)
            sums = assets.FirstOrDefault(a => a.GetProperty("name").GetString() == "SHA256SUMS.txt");
        if (asset.ValueKind == JsonValueKind.Undefined || sums.ValueKind == JsonValueKind.Undefined) return "Обновление найдено, но пакет платформы отсутствует";
        var name = asset.GetProperty("name").GetString()!;
        var temp = Path.Combine(Path.GetTempPath(), name);
        await Download(asset.GetProperty("browser_download_url").GetString()!, temp, token);
        var checksums = await _http.GetStringAsync(sums.GetProperty("browser_download_url").GetString()!, token);
        var expected = checksums.Split('\n').FirstOrDefault(line => line.EndsWith(name, StringComparison.Ordinal))?.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        await using var packageStream = File.OpenRead(temp);
        var actual = Convert.ToHexString(await SHA256.HashDataAsync(packageStream, token));
        if (expected is null || !actual.Equals(expected, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Контрольная сумма обновления не совпадает.");
        Install(temp, remote.ToString(3));
        return $"Обновление {remote.ToString(3)} загружено и устанавливается";
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
        var extract = Path.Combine(Path.GetTempPath(), $"fotur-update-{version}"); Directory.CreateDirectory(extract);
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
        File.WriteAllText(script, $"#!/bin/sh\nsleep 2\nrm -rf {next}\n/usr/bin/ditto {ShellQuote(newBundle)} {next}\nmv {current} {old}\nmv {next} {current}\nopen {current}\nrm -rf {old}\n");
        Process.Start(new ProcessStartInfo("/bin/sh", script) { UseShellExecute = false });
    }

    private static string ShellQuote(string value) => "'" + value.Replace("'", "'\\''", StringComparison.Ordinal) + "'";
}
