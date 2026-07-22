using System.Text.Json.Serialization;

namespace FoturTypingHelper.Core;

public enum DictationHotkeyMode { Hold, Toggle }
public enum DictationOutputMode { Accurate, Fast }
public enum AppTheme { System, Dark, Light }

public sealed class AppSettings
{
    public bool AutoCorrectionEnabled { get; set; } = true;
    public bool DictationEnabled { get; set; } = true;
    public bool StartWithWindows { get; set; } = true;
    public bool EarlyCorrection { get; set; }
    public double CorrectionConfidence { get; set; } = 0.72;
    public bool VoiceCommandsEnabled { get; set; } = true;
    public bool LocalStatisticsEnabled { get; set; } = true;
    public bool MinimizeToTray { get; set; } = true;
    public string UiLanguage { get; set; } = "ru";
    public AppTheme Theme { get; set; } = AppTheme.Dark;
    public DictationHotkeyMode DictationHotkeyMode { get; set; } = DictationHotkeyMode.Hold;
    public DictationOutputMode DictationOutputMode { get; set; } = DictationOutputMode.Accurate;
    public string DictationHotkey { get; set; } = "Ctrl+Alt+Space";
    public string UndoHotkey { get; set; } = "Ctrl+Alt+Backspace";
    public string SpeechModel { get; set; } = "base";
    public string SpeechLanguage { get; set; } = "auto";
    public int MicrophoneDeviceNumber { get; set; } = 0;
    public List<string> CustomDictionary { get; set; } = [];
    public List<string> ExcludedProcesses { get; set; } =
    [
        "keepass", "keepassxc", "1password", "bitwarden", "credentialuibroker",
        "gamebar", "steamwebhelper"
    ];
}

public sealed class UsageStatistics
{
    public long Corrections { get; set; }
    public long Undos { get; set; }
    public long DictatedWords { get; set; }
    public long SavedKeystrokes { get; set; }
    public DateTime FirstRunUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PersistedState
{
    public AppSettings Settings { get; set; } = new();
    public UsageStatistics Statistics { get; set; } = new();
}
