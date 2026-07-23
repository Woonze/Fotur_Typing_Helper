using System.Text;
using FoturTypingHelper.Core;

namespace FoturTypingHelper.Mac;

public sealed class MacKeyboardService : IKeyboardService
{
    private readonly AppSettings _settings;
    private readonly MacTextInjectionService _injection;
    private readonly StringBuilder _word = new();
    private readonly List<string> _recent = [];
    private LanguageScorer _scorer;
    private readonly MacNative.EventTapCallback _callback;
    private nint _tap, _source;
    private bool _dictationDown;
    private HotkeyGesture _dictationHotkey = new(HotkeyModifiers.Ctrl | HotkeyModifiers.Alt, "Space");
    private HotkeyGesture _undoHotkey = new(HotkeyModifiers.Ctrl | HotkeyModifiers.Alt, "Backspace");
    private CorrectionApplied? _lastCorrection;
    private DateTime _lastCorrectionUtc;

    public event EventHandler<CorrectionApplied>? Corrected;
    public event EventHandler<bool>? DictationHotkeyChanged;
    public event EventHandler<string>? StatusChanged;

    public MacKeyboardService(AppSettings settings, MacTextInjectionService injection)
    { _settings = settings; _injection = injection; _scorer = new(settings.CustomDictionary); _callback = OnEvent; RefreshHotkeys(); }

    public void Start()
    {
        if (!OperatingSystem.IsMacOS() || _tap != 0) return;
        if (!MacNative.CGPreflightListenEventAccess())
        {
            MacNative.CGRequestListenEventAccess();
            StatusChanged?.Invoke(this, "Разрешите Fotur отслеживать клавиатуру: Настройки системы → Конфиденциальность и безопасность → Мониторинг ввода, затем перезапустите приложение.");
        }
        if (!_injection.CanPostEvents)
        {
            MacNative.CGRequestPostEventAccess();
            StatusChanged?.Invoke(this, PostAccessMessage);
        }
        _tap = MacNative.CGEventTapCreate(0, 0, 0, (1UL << MacNative.KeyDown) | (1UL << MacNative.KeyUp) | (1UL << MacNative.FlagsChanged), _callback, 0);
        if (_tap == 0)
        {
            StatusChanged?.Invoke(this, "Глобальные хоткеи недоступны: включите для Fotur «Мониторинг ввода» в настройках конфиденциальности macOS и перезапустите приложение. Тестовая кнопка продолжит работать.");
            return;
        }
        _source = MacNative.CFMachPortCreateRunLoopSource(0, _tap, 0);
        var commonModes = MacNative.CFStringCreateWithCString(0, "kCFRunLoopCommonModes", 0x08000100);
        MacNative.CFRunLoopAddSource(MacNative.CFRunLoopGetMain(), _source, commonModes);
        MacNative.CFRelease(commonModes);
        MacNative.CGEventTapEnable(_tap, true);
    }

    public void RefreshSettings() { _scorer = new(_settings.CustomDictionary); RefreshHotkeys(); }

    private nint OnEvent(nint proxy, int type, nint e, nint userInfo)
    {
        if (type is MacNative.TapDisabledByTimeout or MacNative.TapDisabledByUserInput)
        {
            MacNative.CGEventTapEnable(_tap, true);
            StatusChanged?.Invoke(this, "Глобальный перехват клавиш macOS восстановлен");
            return e;
        }
        if ((ulong)MacNative.CGEventGetIntegerValueField(e, MacNative.EventSourceUserData) == MacNative.Marker) return e;
        var key = (ushort)MacNative.CGEventGetIntegerValueField(e, MacNative.KeyboardEventKeycode);
        var flags = MacNative.CGEventGetFlags(e);
        var isHotkey = MatchesHotkey(key, flags, _dictationHotkey);
        if (type == MacNative.KeyDown && isHotkey)
        {
            if (!_dictationDown)
            {
                _dictationDown = true;
                DictationHotkeyChanged?.Invoke(this, true);
            }
            // Suppress both the initial press and every macOS key-repeat event.
            return 0;
        }
        if (_dictationDown && ((type == MacNative.KeyUp && KeyName(key) == _dictationHotkey.Key) ||
            (type == MacNative.FlagsChanged && !HasRequiredModifiers(flags, _dictationHotkey.Modifiers))))
        { _dictationDown = false; DictationHotkeyChanged?.Invoke(this, false); return 0; }
        if (type == MacNative.KeyDown && MatchesHotkey(key, flags, _undoHotkey) && TryUndo()) return 0;
        if (type != MacNative.KeyDown || !_settings.AutoCorrectionEnabled) return e;
        if (key == 49) { Evaluate(); _word.Clear(); return e; }
        if (key is 36 or 48) { Evaluate(); _word.Clear(); _recent.Clear(); return e; }
        if (key == 51) { if (_word.Length > 0) _word.Length--; return e; }
        var chars = new char[4]; MacNative.CGEventKeyboardGetUnicodeString(e, 4, out var length, chars);
        if (length > 0 && (char.IsLetter(chars[0]) || LayoutConverter.IsConvertible(chars[0]))) _word.Append(chars[0]);
        else if (length > 0) { Evaluate(); _word.Clear(); _recent.Clear(); }
        return e;
    }

    private void Evaluate()
    {
        if (_word.Length < 2) return;
        var current = _word.ToString();
        var phrase = _recent.Count == 0 ? current : string.Join(' ', _recent.Append(current));
        var decision = _scorer.Evaluate(phrase, Math.Max(0.56, _settings.CorrectionConfidence - (_recent.Count > 0 ? 0.12 : 0)));
        if (!decision.ShouldCorrect) { _recent.Add(current); if (_recent.Count > 23) _recent.RemoveAt(0); return; }
        if (!_injection.ReplacePrevious(decision.Original, decision.Replacement, decision.Language))
        {
            StatusChanged?.Invoke(this, PostAccessMessage);
            _recent.Clear();
            return;
        }
        _lastCorrection = new(decision.Original, decision.Replacement, decision.Confidence);
        _lastCorrectionUtc = DateTime.UtcNow;
        _recent.Clear(); Corrected?.Invoke(this, _lastCorrection);
    }

    private bool TryUndo()
    {
        if (_lastCorrection is null || DateTime.UtcNow - _lastCorrectionUtc > TimeSpan.FromSeconds(8)) return false;
        if (!_injection.ReplacePrevious(_lastCorrection.Replacement, _lastCorrection.Original, TextLanguage.Unknown))
        {
            StatusChanged?.Invoke(this, PostAccessMessage);
            return false;
        }
        _lastCorrection = null;
        return true;
    }

    private void RefreshHotkeys()
    {
        if (HotkeyGesture.TryParse(_settings.DictationHotkey, out var dictation, out _)) _dictationHotkey = dictation;
        if (HotkeyGesture.TryParse(_settings.UndoHotkey, out var undo, out _)) _undoHotkey = undo;
    }

    private static bool MatchesHotkey(ushort key, ulong flags, HotkeyGesture gesture) =>
        string.Equals(KeyName(key), gesture.Key, StringComparison.OrdinalIgnoreCase) &&
        (Modifiers(flags) & gesture.Modifiers) == gesture.Modifiers;

    private static bool HasRequiredModifiers(ulong flags, HotkeyModifiers required) =>
        (Modifiers(flags) & required) == required;

    private static HotkeyModifiers Modifiers(ulong flags)
    {
        var result = HotkeyModifiers.None;
        if ((flags & MacNative.Control) != 0) result |= HotkeyModifiers.Ctrl;
        if ((flags & MacNative.Alternate) != 0) result |= HotkeyModifiers.Alt;
        if ((flags & MacNative.Shift) != 0) result |= HotkeyModifiers.Shift;
        if ((flags & MacNative.Command) != 0) result |= HotkeyModifiers.Meta;
        return result;
    }

    private static string KeyName(ushort key) => key switch
    {
        49 => "Space", 51 => "Backspace", 36 => "Enter", 48 => "Tab", 53 => "Escape",
        18 => "1", 19 => "2", 20 => "3", 21 => "4", 23 => "5", 22 => "6", 26 => "7", 28 => "8", 25 => "9", 29 => "0",
        122 => "F1", 120 => "F2", 99 => "F3", 118 => "F4", 96 => "F5", 97 => "F6",
        98 => "F7", 100 => "F8", 101 => "F9", 109 => "F10", 103 => "F11", 111 => "F12",
        0 => "A", 11 => "B", 8 => "C", 2 => "D", 14 => "E", 3 => "F", 5 => "G", 4 => "H",
        34 => "I", 38 => "J", 40 => "K", 37 => "L", 46 => "M", 45 => "N", 31 => "O", 35 => "P",
        12 => "Q", 15 => "R", 1 => "S", 17 => "T", 32 => "U", 9 => "V", 13 => "W", 7 => "X",
        16 => "Y", 6 => "Z", _ => $"VK{key:X2}"
    };

    private const string PostAccessMessage = "macOS не разрешает заменять и вставлять текст. Включите Fotur Typing Helper: Настройки системы → Конфиденциальность и безопасность → Универсальный доступ, затем полностью перезапустите приложение.";

    public void Dispose()
    {
        if (_tap != 0) { MacNative.CGEventTapEnable(_tap, false); MacNative.CFRelease(_tap); }
        if (_source != 0) MacNative.CFRelease(_source);
        _tap = _source = 0;
    }
}
