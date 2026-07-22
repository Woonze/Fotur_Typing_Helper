namespace FoturTypingHelper.Core;

public sealed record CorrectionApplied(string Original, string Replacement, double Confidence);
public sealed record AudioDeviceInfo(int Number, string Name, bool IsDefault = false);

public interface IKeyboardService : IDisposable
{
    event EventHandler<CorrectionApplied>? Corrected;
    event EventHandler<bool>? DictationHotkeyChanged;
    void Start();
    void RefreshSettings();
}

public interface IAudioRecorder : IDisposable
{
    bool IsRecording { get; }
    event EventHandler<double>? LevelChanged;
    IReadOnlyList<AudioDeviceInfo> GetDevices();
    void Start(int deviceNumber = 0);
    Task<string?> StopAsync();
}

public interface IDictationService
{
    event EventHandler<double>? DownloadProgress;
    Task<string> TranscribeAsync(string audioPath, AppSettings settings, CancellationToken cancellationToken = default);
    bool IsModelInstalled(string model);
    string GetRuntimeInfo();
}

public interface ITextInjectionService
{
    bool ActivateWindow(nint window);
    void SendText(string text);
}

public interface IActiveWindowService { nint GetActiveWindowHandle(); }
public interface IAutostartService { void SetEnabled(bool enabled); }
