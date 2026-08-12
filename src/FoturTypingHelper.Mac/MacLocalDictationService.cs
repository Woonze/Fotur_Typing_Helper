using FoturTypingHelper.Core;
using Whisper.net;
using Whisper.net.Ggml;
using Whisper.net.LibraryLoader;

namespace FoturTypingHelper.Mac;

public sealed class MacLocalDictationService : IDictationService
{
    private readonly string _root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fotur", "TypingHelper", "models");
    public event EventHandler<double>? DownloadProgress;
    public MacLocalDictationService() { Directory.CreateDirectory(_root); RuntimeOptions.RuntimeLibraryOrder = [RuntimeLibrary.CoreML, RuntimeLibrary.Cpu]; }
    public string GetRuntimeInfo() => WhisperFactory.GetRuntimeInfo() ?? "Whisper CoreML/CPU runtime loaded";
    public bool IsModelInstalled(string model) => IsUsableModel(Path.Combine(_root, $"ggml-{model}.bin"), model);

    public async Task<string> TranscribeAsync(string audioPath, AppSettings settings, CancellationToken cancellationToken = default)
    {
        WavAudioProcessor.ProcessInPlace(audioPath, settings);
        var path = await EnsureModel(settings.SpeechModel, cancellationToken);
        using var factory = WhisperFactory.FromPath(path); var builder = factory.CreateBuilder();
        if (settings.SpeechLanguage == "auto") builder.WithLanguageDetection(); else builder.WithLanguage(settings.SpeechLanguage);
        if (settings.DictationTaskMode == DictationTaskMode.TranslateToEnglish) builder.WithTranslate();
        if (settings.DictionaryPromptEnabled && settings.CustomDictionary.Count > 0) builder.WithPrompt(string.Join(", ", settings.CustomDictionary.Take(80)));
        using var processor = builder.Build(); await using var audio = File.OpenRead(audioPath); var parts = new List<string>();
        await foreach (var segment in processor.ProcessAsync(audio, cancellationToken)) parts.Add(segment.Text.Trim());
        try { File.Delete(audioPath); } catch { }
        return DictationTextPostProcessor.Process(VoiceCommandProcessor.Process(string.Join(" ", parts), settings.VoiceCommandsEnabled), settings);
    }

    private async Task<string> EnsureModel(string model, CancellationToken token)
    {
        var path = Path.Combine(_root, $"ggml-{model}.bin"); if (IsUsableModel(path, model)) return path;
        if (File.Exists(path)) File.Delete(path);
        var type = model switch { "tiny" => GgmlType.Tiny, "small" => GgmlType.Small, "medium" => GgmlType.Medium, _ => GgmlType.Base };
        await using var source = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(type, cancellationToken: token);
        var temporaryPath = path + ".download";
        try
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            await using var target = File.Create(temporaryPath);
            var buffer = new byte[131072]; long copied = 0; int read;
            while ((read = await source.ReadAsync(buffer, token)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read), token);
                copied += read;
                if (source.CanSeek && source.Length > 0) DownloadProgress?.Invoke(this, (double)copied / source.Length);
            }
            await target.FlushAsync(token);
            File.Move(temporaryPath, path, true);
        }
        catch
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); } catch { }
            throw;
        }
        return path;
    }

    private static bool IsUsableModel(string path, string model) => File.Exists(path) && new FileInfo(path).Length >= MinimumModelBytes(model);
    private static long MinimumModelBytes(string model) => model.ToLowerInvariant() switch
    {
        "tiny" => 40L * 1024 * 1024,
        "small" => 300L * 1024 * 1024,
        "medium" => 900L * 1024 * 1024,
        _ => 90L * 1024 * 1024
    };
}
