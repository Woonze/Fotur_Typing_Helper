using FoturTypingHelper.Core;
using Whisper.net;
using Whisper.net.Ggml;
using Whisper.net.LibraryLoader;

namespace FoturTypingHelper.Windows;

public sealed class LocalDictationService : IDictationService
{
    private readonly string _modelsRoot;
    public event EventHandler<double>? DownloadProgress;

    public LocalDictationService()
    {
        _modelsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Fotur", "TypingHelper", "models");
        Directory.CreateDirectory(_modelsRoot);
        ConfigureNativeRuntime();
    }

    public string GetRuntimeInfo()
    {
        ConfigureNativeRuntime();
        return WhisperFactory.GetRuntimeInfo() ?? "Whisper CPU runtime loaded";
    }

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
        var formatted = VoiceCommandProcessor.Process(string.Join(" ", result), settings.VoiceCommandsEnabled);
        return DictationTextPostProcessor.Process(formatted, settings);
    }

    public bool IsModelInstalled(string model) => File.Exists(GetModelPath(model));

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

    private string GetModelPath(string model) => Path.Combine(_modelsRoot, $"ggml-{model.ToLowerInvariant()}.bin");

    private static void ConfigureNativeRuntime()
    {
        if (!OperatingSystem.IsWindows()) return;
        var path = Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x64", "whisper.dll");
        if (File.Exists(path)) RuntimeOptions.LibraryPath = path;
    }
}
