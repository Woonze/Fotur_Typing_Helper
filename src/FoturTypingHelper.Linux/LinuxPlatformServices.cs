using System.Diagnostics;
using FoturTypingHelper.Core;
using Whisper.net;
using Whisper.net.Ggml;

namespace FoturTypingHelper.Linux;

public sealed class LinuxKeyboardService : IKeyboardService
{
    public event EventHandler<CorrectionApplied>? Corrected;
    public event EventHandler<bool>? DictationHotkeyChanged;
    public event EventHandler<string>? StatusChanged;

    public void Start() =>
        StatusChanged?.Invoke(this, "Linux experimental: глобальная автокоррекция и хоткеи пока отключены. Диктовку можно запускать кнопкой в окне.");

    public void RefreshSettings() { }
    public void Dispose() { }
}

public sealed class LinuxAudioRecorder : IAudioRecorder
{
    private Process? _process;
    private string? _path;
    public bool IsRecording => _process is { HasExited: false };
    public event EventHandler<double>? LevelChanged;

    public IReadOnlyList<AudioDeviceInfo> GetDevices() => [new(0, "Default ALSA/PulseAudio microphone", true)];

    public void Start(int deviceNumber = 0)
    {
        if (IsRecording) return;
        if (!CommandExists("arecord"))
            throw new InvalidOperationException("Linux recorder requires arecord. Install alsa-utils and allow microphone access.");
        _path = Path.Combine(Path.GetTempPath(), $"fotur-dictation-{Guid.NewGuid():N}.wav");
        _process = Process.Start(new ProcessStartInfo("arecord", $"-q -f S16_LE -r 16000 -c 1 \"{_path}\"")
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("Не удалось запустить arecord.");
        LevelChanged?.Invoke(this, 0.35);
    }

    public async Task<string?> StopAsync()
    {
        if (_process is null) return null;
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3));
            }
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
        return File.Exists(_path) && new FileInfo(_path).Length > 44 ? _path : null;
    }

    public void Dispose()
    {
        if (_process is { HasExited: false }) _process.Kill(entireProcessTree: true);
        _process?.Dispose();
    }

    private static bool CommandExists(string name)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("sh", $"-lc \"command -v {name}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            process?.WaitForExit(1500);
            return process?.ExitCode == 0;
        }
        catch { return false; }
    }
}

public sealed class LinuxLocalDictationService : IDictationService
{
    private readonly string _root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Fotur", "TypingHelper", "models");

    public event EventHandler<double>? DownloadProgress;

    public LinuxLocalDictationService() => Directory.CreateDirectory(_root);
    public string GetRuntimeInfo() => WhisperFactory.GetRuntimeInfo() ?? "Whisper CPU runtime loaded";
    public bool IsModelInstalled(string model) => File.Exists(GetModelPath(model));

    public async Task<string> TranscribeAsync(string audioPath, AppSettings settings, CancellationToken cancellationToken = default)
    {
        WavAudioProcessor.ProcessInPlace(audioPath, settings);
        var modelPath = await EnsureModelAsync(settings.SpeechModel, cancellationToken);
        using var factory = WhisperFactory.FromPath(modelPath);
        var builder = factory.CreateBuilder();
        if (string.Equals(settings.SpeechLanguage, "auto", StringComparison.OrdinalIgnoreCase))
            builder.WithLanguageDetection();
        else
            builder.WithLanguage(settings.SpeechLanguage);
        if (settings.DictationTaskMode == DictationTaskMode.TranslateToEnglish)
            builder.WithTranslate();
        if (settings.DictionaryPromptEnabled && settings.CustomDictionary.Count > 0)
            builder.WithPrompt(string.Join(", ", settings.CustomDictionary.Take(80)));
        using var processor = builder.Build();
        await using var audio = File.OpenRead(audioPath);
        var result = new List<string>();
        await foreach (var segment in processor.ProcessAsync(audio, cancellationToken))
            result.Add(segment.Text.Trim());
        try { File.Delete(audioPath); } catch { }
        return DictationTextPostProcessor.Process(
            VoiceCommandProcessor.Process(string.Join(" ", result), settings.VoiceCommandsEnabled),
            settings);
    }

    private async Task<string> EnsureModelAsync(string model, CancellationToken cancellationToken)
    {
        var path = GetModelPath(model);
        if (File.Exists(path)) return path;
        var type = model.ToLowerInvariant() switch
        {
            "tiny" => GgmlType.Tiny,
            "small" => GgmlType.Small,
            "medium" => GgmlType.Medium,
            _ => GgmlType.Base
        };
        await using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(type, cancellationToken: cancellationToken);
        await using var target = File.Create(path);
        var buffer = new byte[1024 * 128];
        long copied = 0;
        int read;
        while ((read = await modelStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            copied += read;
            if (modelStream.CanSeek && modelStream.Length > 0)
                DownloadProgress?.Invoke(this, (double)copied / modelStream.Length);
        }
        return path;
    }

    private string GetModelPath(string model) => Path.Combine(_root, $"ggml-{model.ToLowerInvariant()}.bin");
}

public sealed class LinuxTextInjectionService : ITextInjectionService
{
    public bool ActivateWindow(nint window) => true;

    public bool SendText(string text)
    {
        if (!OperatingSystem.IsLinux()) return false;
        if (string.Equals(Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"), "wayland", StringComparison.OrdinalIgnoreCase))
            return false;
        try
        {
            using var process = Process.Start(new ProcessStartInfo("xdotool", "type --clearmodifiers --delay 0 --file -")
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            if (process is null) return false;
            process.StandardInput.Write(text);
            process.StandardInput.Close();
            process.WaitForExit(3000);
            return process.ExitCode == 0;
        }
        catch { return false; }
    }
}

public sealed class LinuxActiveWindowService : IActiveWindowService
{
    public nint GetActiveWindowHandle() => 0;
}

public sealed class LinuxAutostartService : IAutostartService
{
    public void SetEnabled(bool enabled)
    {
        // Linux autostart has desktop-environment-specific paths. 1.3.0 keeps this a no-op
        // instead of writing a broken .desktop file without knowing the install location.
    }
}
