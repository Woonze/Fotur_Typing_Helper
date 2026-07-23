using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using FoturTypingHelper.Core;
using System.Reflection;

namespace FoturTypingHelper.App;

public partial class MainWindow : Window
{
    private readonly SettingsStore _store;
    private readonly IAutostartService _autostart;
    private readonly AppRuntime _runtime;
    private IReadOnlyList<AudioDeviceInfo> _microphones = [];
    private bool _loading = true;

    public MainWindow(SettingsStore store, IAutostartService autostart, AppRuntime runtime)
    {
        _store = store; _autostart = autostart; _runtime = runtime;
        InitializeComponent(); Icon = IconFactory.Create(); LoadSettings();
        Closing += OnClosing;
        PropertyChanged += (_, e) => { if (e.Property == WindowStateProperty && WindowState == WindowState.Minimized && _store.State.Settings.MinimizeToTray) Hide(); };
        _runtime.StatusChanged += (_, text) => Avalonia.Threading.Dispatcher.UIThread.Post(() => StatusText.Text = text);
        _runtime.StatisticsChanged += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(UpdateStatistics);
        _runtime.MicrophoneLevelChanged += (_, level) => Avalonia.Threading.Dispatcher.UIThread.Post(() => MicrophoneLevelBar.Value = level);
    }

    public void Restore() { Show(); WindowState = WindowState.Normal; Activate(); }
    private void LoadSettings()
    {
        var s = _store.State.Settings;
        AutoCorrectionToggle.IsChecked = CorrectionEnabledSettings.IsChecked = s.AutoCorrectionEnabled;
        DictationToggle.IsChecked = DictationEnabledSettings.IsChecked = s.DictationEnabled;
        EarlyCorrectionCheck.IsChecked = s.EarlyCorrection; ConfidenceSlider.Value = s.CorrectionConfidence;
        VoiceCommandsCheck.IsChecked = s.VoiceCommandsEnabled; AutostartToggle.IsChecked = s.StartWithWindows;
        AutoUpdateToggle.IsChecked = s.AutoUpdateEnabled;
        VadToggle.IsChecked = s.VoiceActivityDetectionEnabled;
        NoiseToggle.IsChecked = s.NoiseSuppressionEnabled;
        DictionaryPromptToggle.IsChecked = s.DictionaryPromptEnabled;
        FillerToggle.IsChecked = s.FillerWordsRemovalEnabled;
        TrayToggle.IsChecked = s.MinimizeToTray; StatisticsToggle.IsChecked = s.LocalStatisticsEnabled;
        HotkeyModeCombo.SelectedIndex = s.DictationHotkeyMode == DictationHotkeyMode.Hold ? 0 : 1;
        ModelCombo.SelectedIndex = s.SpeechModel switch { "tiny" => 0, "small" => 2, "medium" => 3, _ => 1 };
        SpeechLanguageCombo.SelectedIndex = s.SpeechLanguage switch { "ru" => 1, "en" => 2, _ => 0 };
        TranslationToggle.IsChecked = s.DictationTaskMode == DictationTaskMode.TranslateToEnglish;
        _microphones = _runtime.Microphones;
        MicrophoneCombo.ItemsSource = _microphones.Select(device => device.IsDefault ? $"{device.Name} · по умолчанию" : device.Name).ToArray();
        MicrophoneCombo.SelectedIndex = Math.Max(0, _microphones.ToList().FindIndex(device => device.Number == s.MicrophoneDeviceNumber));
        DictionaryList.ItemsSource = s.CustomDictionary.ToArray();
        ExcludedProcessesText.Text = string.Join(Environment.NewLine, s.ExcludedProcesses);
        DictationHotkeyBox.Text = s.DictationHotkey;
        UndoHotkeyBox.Text = s.UndoHotkey;
        UpdateHotkeyLabels();
        ConfidenceValueText.Text = $"{s.CorrectionConfidence:P0}";
        ModelStatusText.Text = $"Whisper {s.SpeechModel} · " + (_runtime.IsModelInstalled ? "готова" : "загрузится при первом запуске");
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.1.0";
        var platform = OperatingSystem.IsMacOS()
            ? (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64 ? "macOS Apple Silicon" : "macOS Intel")
            : "Windows x64";
        AboutVersionText.Text = $"Версия {version} · 23 июля 2026 · MIT · {platform}";
        UpdateStatistics(); _loading = false;
    }

    private void SettingChanged(object? sender, RoutedEventArgs e) => SaveControls();
    private void SettingChanged(object? sender, SelectionChangedEventArgs e) => SaveControls();
    private void SettingChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e) => SaveControls();
    private void SaveControls()
    {
        if (_loading) return;
        var s = _store.State.Settings;
        if (ReferenceEquals(AutoCorrectionToggle, FocusManager?.GetFocusedElement())) CorrectionEnabledSettings.IsChecked = AutoCorrectionToggle.IsChecked;
        else AutoCorrectionToggle.IsChecked = CorrectionEnabledSettings.IsChecked;
        if (ReferenceEquals(DictationToggle, FocusManager?.GetFocusedElement())) DictationEnabledSettings.IsChecked = DictationToggle.IsChecked;
        else DictationToggle.IsChecked = DictationEnabledSettings.IsChecked;
        s.AutoCorrectionEnabled = AutoCorrectionToggle.IsChecked == true; s.DictationEnabled = DictationToggle.IsChecked == true;
        s.EarlyCorrection = EarlyCorrectionCheck.IsChecked == true; s.CorrectionConfidence = ConfidenceSlider.Value;
        s.VoiceCommandsEnabled = VoiceCommandsCheck.IsChecked == true; s.StartWithWindows = AutostartToggle.IsChecked == true;
        s.AutoUpdateEnabled = AutoUpdateToggle.IsChecked == true;
        s.VoiceActivityDetectionEnabled = VadToggle.IsChecked == true;
        s.NoiseSuppressionEnabled = NoiseToggle.IsChecked == true;
        s.DictionaryPromptEnabled = DictionaryPromptToggle.IsChecked == true;
        s.FillerWordsRemovalEnabled = FillerToggle.IsChecked == true;
        s.MinimizeToTray = TrayToggle.IsChecked == true; s.LocalStatisticsEnabled = StatisticsToggle.IsChecked == true;
        s.DictationHotkeyMode = HotkeyModeCombo.SelectedIndex == 1 ? DictationHotkeyMode.Toggle : DictationHotkeyMode.Hold;
        s.SpeechModel = ModelCombo.SelectedIndex switch { 0 => "tiny", 2 => "small", 3 => "medium", _ => "base" };
        s.SpeechLanguage = SpeechLanguageCombo.SelectedIndex switch { 1 => "ru", 2 => "en", _ => "auto" };
        s.DictationTaskMode = TranslationToggle.IsChecked == true
            ? DictationTaskMode.TranslateToEnglish
            : DictationTaskMode.Transcribe;
        if (MicrophoneCombo.SelectedIndex >= 0 && MicrophoneCombo.SelectedIndex < _microphones.Count)
            s.MicrophoneDeviceNumber = _microphones[MicrophoneCombo.SelectedIndex].Number;
        ConfidenceValueText.Text = $"{s.CorrectionConfidence:P0}";
        _store.Save(); _autostart.SetEnabled(s.StartWithWindows); _runtime.RefreshSettings();
    }

    private void ExcludedProcessesChanged(object? sender, RoutedEventArgs e)
    {
        _store.State.Settings.ExcludedProcesses = (ExcludedProcessesText.Text ?? "").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToList();
        _store.Save();
    }
    private void AddDictionaryWord(object? sender, RoutedEventArgs e)
    {
        var word = DictionaryInput.Text?.Trim();
        if (string.IsNullOrEmpty(word) || _store.State.Settings.CustomDictionary.Contains(word, StringComparer.OrdinalIgnoreCase)) return;
        _store.State.Settings.CustomDictionary.Add(word); DictionaryInput.Clear();
        DictionaryList.ItemsSource = _store.State.Settings.CustomDictionary.ToArray();
        _store.Save(); _runtime.RefreshSettings();
    }

    private void ClearStatistics(object? sender, RoutedEventArgs e)
    {
        _store.State.Statistics = new(); _store.Save(); UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        var stats = _store.State.Statistics;
        CorrectionsStat.Text = stats.Corrections.ToString("N0");
        DictatedStat.Text = stats.DictatedWords.ToString("N0");
        SavedStat.Text = stats.SavedKeystrokes.ToString("N0");
    }

    private async void ToggleDictation(object? sender, RoutedEventArgs e) => await _runtime.ToggleDictationAsync();
    private void ShowDashboard(object? sender, RoutedEventArgs e) => ShowPanel(DashboardPanel);
    private void ShowCorrection(object? sender, RoutedEventArgs e) => ShowPanel(CorrectionPanel);
    private void ShowDictation(object? sender, RoutedEventArgs e) => ShowPanel(DictationPanel);
    private void ShowDictionary(object? sender, RoutedEventArgs e) => ShowPanel(DictionaryPanel);
    private void ShowSettings(object? sender, RoutedEventArgs e) => ShowPanel(SettingsPanel);
    private void ShowAbout(object? sender, RoutedEventArgs e) => ShowPanel(AboutPanel);
    private void ShowPanel(Control target)
    {
        foreach (var panel in new[] { DashboardPanel, CorrectionPanel, DictationPanel, DictionaryPanel, SettingsPanel, AboutPanel })
            panel.IsVisible = ReferenceEquals(panel, target);
        var pairs = new (Control Panel, Button Button)[]
        {
            (DashboardPanel, NavDashboard), (CorrectionPanel, NavCorrection), (DictationPanel, NavDictation),
            (DictionaryPanel, NavDictionary), (SettingsPanel, NavSettings), (AboutPanel, NavAbout)
        };
        foreach (var pair in pairs) pair.Button.Classes.Set("active", ReferenceEquals(pair.Panel, target));
    }

    private void CaptureHotkey(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox box || IsModifierKey(e.Key)) return;
        var parts = new List<string>();
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Meta)) parts.Add("Cmd");
        parts.Add(KeyName(e.Key));
        var raw = string.Join('+', parts);
        if (HotkeyGesture.TryParse(raw, out var gesture, out var parseError)) box.Text = gesture.ToString();
        else { ShowHotkeyError(parseError); e.Handled = true; return; }

        var error = HotkeyGesture.ValidatePair(DictationHotkeyBox.Text ?? "", UndoHotkeyBox.Text ?? "");
        if (error is not null) ShowHotkeyError(error);
        else
        {
            var settings = _store.State.Settings;
            settings.DictationHotkey = DictationHotkeyBox.Text!;
            settings.UndoHotkey = UndoHotkeyBox.Text!;
            HotkeyValidationText.Text = "Сочетания свободны и сохранены";
            HotkeyValidationText.Foreground = Brush.Parse("#52E58A");
            _store.Save(); _runtime.RefreshSettings(); UpdateHotkeyLabels();
        }
        e.Handled = true;
    }

    private void UpdateHotkeyLabels()
    {
        SidebarHotkeyText.Text = PrettyHotkey(_store.State.Settings.DictationHotkey);
        UndoHotkeySummary.Text = PrettyHotkey(_store.State.Settings.UndoHotkey) + " · доступна 8 секунд";
    }

    private void ShowHotkeyError(string error)
    {
        HotkeyValidationText.Text = error;
        HotkeyValidationText.Foreground = Brush.Parse("#FF7A8A");
    }

    private static bool IsModifierKey(Key key) => key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin;
    private static string PrettyHotkey(string value) => value.Replace("+", " + ");
    private static string KeyName(Key key)
    {
        var name = key.ToString();
        if (name is "Back") return "Backspace";
        if (name.StartsWith('D') && name.Length == 2 && char.IsDigit(name[1])) return name[1].ToString();
        return name;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (App.ExitRequested || !_store.State.Settings.MinimizeToTray) return;
        e.Cancel = true; Hide();
    }
}
