using Avalonia.Threading;
using FoturTypingHelper.Core;
using FoturTypingHelper.Windows;

namespace FoturTypingHelper.App;

public sealed class AppRuntime : IDisposable
{
    private readonly SettingsStore _store;
    private readonly KeyboardHookService _keyboard;
    private readonly AudioRecorder _recorder;
    private readonly LocalDictationService _dictation;
    private readonly TextInjectionService _injection;
    private readonly ActiveWindowService _activeWindow;
    private MicrophoneOverlay? _overlay;
    private bool _hotkeyDown;
    private bool _busy;
    private IntPtr _dictationTarget;
    public event EventHandler<string>? StatusChanged;
    public event EventHandler? StatisticsChanged;
    public bool IsModelInstalled => _dictation.IsModelInstalled(_store.State.Settings.SpeechModel);

    public AppRuntime(SettingsStore store, KeyboardHookService keyboard, AudioRecorder recorder,
        LocalDictationService dictation, TextInjectionService injection, ActiveWindowService activeWindow)
    {
        _store = store; _keyboard = keyboard; _recorder = recorder; _dictation = dictation; _injection = injection; _activeWindow = activeWindow;
        _keyboard.Corrected += OnCorrected;
        _keyboard.DictationHotkeyChanged += OnHotkey;
        _dictation.DownloadProgress += (_, progress) => StatusChanged?.Invoke(this, $"Загрузка модели: {progress:P0}");
    }

    public void Start() => _keyboard.Start();
    public void RefreshSettings() => _keyboard.RefreshSettings();

    private void OnCorrected(object? sender, CorrectionApplied correction)
    {
        if (!_store.State.Settings.LocalStatisticsEnabled) return;
        _store.State.Statistics.Corrections++;
        _store.State.Statistics.SavedKeystrokes += correction.Replacement.Length;
        _store.Save(); StatisticsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnHotkey(object? sender, bool isDown)
    {
        if (!_store.State.Settings.DictationEnabled) return;
        if (_store.State.Settings.DictationHotkeyMode == DictationHotkeyMode.Hold)
        {
            if (isDown && !_hotkeyDown) Dispatcher.UIThread.Post(() => _ = StartDictationAsync());
            if (!isDown && _hotkeyDown) Dispatcher.UIThread.Post(() => _ = StopDictationAsync());
        }
        else if (isDown && !_hotkeyDown) Dispatcher.UIThread.Post(() => _ = ToggleDictationAsync());
        _hotkeyDown = isDown;
    }

    public Task ToggleDictationAsync() => _recorder.IsRecording ? StopDictationAsync() : StartDictationAsync();

    private Task StartDictationAsync()
    {
        if (_busy || _recorder.IsRecording) return Task.CompletedTask;
        try
        {
            _dictationTarget = _activeWindow.GetActiveWindow().Handle;
            _recorder.Start(_store.State.Settings.MicrophoneDeviceNumber);
            _overlay = new MicrophoneOverlay { ShowActivated = false };
            _overlay.Show();
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
                _injection.SendText(text + " ");
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
        finally { _overlay?.Close(); _overlay = null; _busy = false; }
    }

    public void Dispose() { _keyboard.Dispose(); _recorder.Dispose(); }
}
