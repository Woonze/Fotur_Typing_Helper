using Avalonia.Threading;
using FoturTypingHelper.Core;

namespace FoturTypingHelper.App;

public sealed class AppRuntime : IDisposable
{
    private readonly SettingsStore _store;
    private readonly IKeyboardService _keyboard;
    private readonly IAudioRecorder _recorder;
    private readonly IDictationService _dictation;
    private readonly ITextInjectionService _injection;
    private readonly IActiveWindowService _activeWindow;
    private ScreenEdgeOverlay? _overlay;
    private bool _hotkeyDown;
    private bool _busy;
    private IntPtr _dictationTarget;
    public event EventHandler<string>? StatusChanged;
    public event EventHandler? StatisticsChanged;
    public event EventHandler<double>? MicrophoneLevelChanged;
    public IReadOnlyList<AudioDeviceInfo> Microphones => _recorder.GetDevices();
    public bool IsModelInstalled => _dictation.IsModelInstalled(_store.State.Settings.SpeechModel);

    public AppRuntime(SettingsStore store, IKeyboardService keyboard, IAudioRecorder recorder,
        IDictationService dictation, ITextInjectionService injection, IActiveWindowService activeWindow)
    {
        _store = store; _keyboard = keyboard; _recorder = recorder; _dictation = dictation; _injection = injection; _activeWindow = activeWindow;
        _keyboard.Corrected += OnCorrected;
        _keyboard.DictationHotkeyChanged += OnHotkey;
        _keyboard.StatusChanged += (_, text) => StatusChanged?.Invoke(this, text);
        _dictation.DownloadProgress += (_, progress) => StatusChanged?.Invoke(this, $"Загрузка модели: {progress:P0}");
        _recorder.LevelChanged += (_, level) => MicrophoneLevelChanged?.Invoke(this, level);
    }

    public void Start() => _keyboard.Start();
    public void RefreshSettings() => _keyboard.RefreshSettings();
    public void ReportStatus(string text) => StatusChanged?.Invoke(this, text);

    private void OnCorrected(object? sender, CorrectionApplied correction)
    {
        if (!_store.State.Settings.LocalStatisticsEnabled) return;
        _store.State.Statistics.Corrections++;
        _store.State.Statistics.SavedKeystrokes += correction.Replacement.Length;
        _store.Save(); StatisticsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnHotkey(object? sender, bool isDown)
    {
        if (!_store.State.Settings.DictationEnabled)
        {
            _hotkeyDown = false;
            return;
        }
        if (_store.State.Settings.DictationHotkeyMode == DictationHotkeyMode.Hold)
        {
            if (isDown && !_hotkeyDown)
            {
                _hotkeyDown = true;
                Dispatcher.UIThread.Post(() => _ = StartDictationAsync());
            }
            else if (!isDown)
            {
                _hotkeyDown = false;
                Dispatcher.UIThread.Post(() => _ = StopDictationAsync());
            }
        }
        else
        {
            if (isDown && !_hotkeyDown) Dispatcher.UIThread.Post(() => _ = ToggleDictationAsync());
            _hotkeyDown = isDown;
        }
    }

    public Task ToggleDictationAsync() => _recorder.IsRecording ? StopDictationAsync() : StartDictationAsync();

    private Task StartDictationAsync()
    {
        if (_busy || _recorder.IsRecording) return Task.CompletedTask;
        try
        {
            _dictationTarget = _activeWindow.GetActiveWindowHandle();
            _recorder.Start(_store.State.Settings.MicrophoneDeviceNumber);
            _overlay = ScreenEdgeOverlay.ShowRecording();
            StatusChanged?.Invoke(this, "Слушаю микрофон…");
        }
        catch (Exception ex)
        {
            DiagnosticLog.Write("Microphone", ex);
            StatusChanged?.Invoke(this, "Микрофон недоступен: " + ex.Message);
        }
        return Task.CompletedTask;
    }

    private async Task StopDictationAsync()
    {
        if (_busy || !_recorder.IsRecording) return;
        _busy = true;
        try
        {
            _overlay?.SetProcessing();
            StatusChanged?.Invoke(this, IsModelInstalled ? "Распознаю локально…" : "Загружаю локальную модель…");
            var path = await _recorder.StopAsync();
            if (path is null) return;
            var text = await _dictation.TranscribeAsync(path, _store.State.Settings);
            if (!string.IsNullOrWhiteSpace(text))
            {
                _injection.ActivateWindow(_dictationTarget);
                var inserted = _injection.SendText(text + " ");
                if (!inserted)
                {
                    StatusChanged?.Invoke(this, "Текст распознан, но macOS запретила его вставку. Включите Fotur в Настройки системы → Конфиденциальность и безопасность → Универсальный доступ и перезапустите приложение.");
                    return;
                }
                if (_store.State.Settings.LocalStatisticsEnabled)
                {
                    _store.State.Statistics.DictatedWords += text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                    _store.State.Statistics.SavedKeystrokes += text.Length;
                    _store.Save(); StatisticsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            StatusChanged?.Invoke(this, string.IsNullOrWhiteSpace(text) ? "Речь не распознана" : "Текст вставлен");
        }
        catch (Exception ex)
        {
            DiagnosticLog.Write("Dictation", ex);
            StatusChanged?.Invoke(this, "Ошибка диктовки: " + ex.Message);
        }
        finally { _overlay?.Dispose(); _overlay = null; _busy = false; }
    }

    public void Dispose() { _keyboard.Dispose(); _recorder.Dispose(); }
}
