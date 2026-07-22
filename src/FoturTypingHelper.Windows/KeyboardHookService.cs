using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using FoturTypingHelper.Core;

namespace FoturTypingHelper.Windows;

public sealed record CorrectionApplied(string Original, string Replacement, double Confidence);

public sealed class KeyboardHookService : IDisposable
{
    private readonly AppSettings _settings;
    private readonly ActiveWindowService _activeWindow;
    private readonly TextInjectionService _injection;
    private readonly NativeMethods.HookProc _callback;
    private LanguageScorer _scorer;
    private readonly StringBuilder _word = new();
    private readonly List<string> _recentWords = [];
    private HotkeyGesture _dictationHotkey = new(HotkeyModifiers.Ctrl | HotkeyModifiers.Alt, "Space");
    private HotkeyGesture _undoHotkey = new(HotkeyModifiers.Ctrl | HotkeyModifiers.Alt, "Backspace");
    private IntPtr _hook;
    private CorrectionApplied? _lastCorrection;
    private DateTime _lastCorrectionUtc;
    private bool _dictationGestureActive;
    private bool _dictationKeyCaptured;

    public event EventHandler<CorrectionApplied>? Corrected;
    public event EventHandler<bool>? DictationHotkeyChanged;

    public KeyboardHookService(AppSettings settings, ActiveWindowService activeWindow, TextInjectionService injection)
    {
        _settings = settings;
        _activeWindow = activeWindow;
        _injection = injection;
        _scorer = new LanguageScorer(settings.CustomDictionary);
        _callback = HookCallback;
        RefreshHotkeys();
    }

    public void Start()
    {
        if (!OperatingSystem.IsWindows() || _hook != IntPtr.Zero) return;
        _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WhKeyboardLl, _callback, IntPtr.Zero, 0);
        if (_hook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "Не удалось включить клавиатурный hook");
    }

    public void RefreshSettings()
    {
        _scorer = new LanguageScorer(_settings.CustomDictionary);
        RefreshHotkeys();
    }

    private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code < 0) return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
        var data = Marshal.PtrToStructure<NativeMethods.KbdLlHookStruct>(lParam);
        if (data.ExtraInfo == TextInjectionService.InjectionMarker)
            return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);

        var message = wParam.ToInt32();
        var down = message is NativeMethods.WmKeyDown or NativeMethods.WmSysKeyDown;
        var up = message is NativeMethods.WmKeyUp or NativeMethods.WmSysKeyUp;
        var dictationKey = string.Equals(VirtualKeyName((int)data.VkCode), _dictationHotkey.Key, StringComparison.OrdinalIgnoreCase);
        if (down && dictationKey && CurrentModifiers() == _dictationHotkey.Modifiers)
        {
            if (!_dictationGestureActive)
            {
                _dictationGestureActive = true;
                _dictationKeyCaptured = true;
                DictationHotkeyChanged?.Invoke(this, true);
            }
            return new IntPtr(1);
        }
        if (up && dictationKey && _dictationKeyCaptured)
        {
            _dictationKeyCaptured = false;
            if (_dictationGestureActive) DictationHotkeyChanged?.Invoke(this, false);
            _dictationGestureActive = false;
            return new IntPtr(1);
        }
        if (up && _dictationGestureActive && IsRequiredModifier((int)data.VkCode, _dictationHotkey.Modifiers))
        {
            _dictationGestureActive = false;
            DictationHotkeyChanged?.Invoke(this, false);
        }

        if (!down) return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
        if (MatchesHotkey((int)data.VkCode, _undoHotkey) && TryUndo()) return new IntPtr(1);
        if (!_settings.AutoCorrectionEnabled) return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);

        var active = _activeWindow.GetActiveWindow();
        if (active.IsPasswordField || _settings.ExcludedProcesses.Any(p =>
                active.ProcessName.Contains(p, StringComparison.OrdinalIgnoreCase)))
        {
            _word.Clear();
            _recentWords.Clear();
            return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
        }

        var vk = (int)data.VkCode;
        if (vk == NativeMethods.VkBack)
        {
            if (_word.Length > 0) _word.Length--;
        }
        else if (vk == NativeMethods.VkSpace)
        {
            if (_word.Length == 0) _recentWords.Clear();
            else if (!EvaluateAndReplace(active)) RememberWord(_word.ToString());
            _word.Clear();
        }
        else if (vk is NativeMethods.VkReturn or NativeMethods.VkTab)
        {
            EvaluateAndReplace(active);
            _word.Clear(); _recentWords.Clear();
        }
        else if (TryGetCharacter(data.VkCode, data.ScanCode, active.ThreadId, out var character))
        {
            if (char.IsLetter(character) || character is '\'' or '-' || LayoutConverter.IsConvertible(character))
            {
                _word.Append(character);
                if (_settings.EarlyCorrection && TryEarlyCorrection(active)) return new IntPtr(1);
            }
            else
            {
                EvaluateAndReplace(active);
                _word.Clear(); _recentWords.Clear();
            }
        }

        return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
    }

    private bool EvaluateAndReplace(ActiveWindowInfo active)
    {
        if (_word.Length < 2) return false;
        var current = _word.ToString();
        var decision = _scorer.Evaluate(current, _settings.CorrectionConfidence);
        if (_recentWords.Count > 0)
        {
            var phrase = string.Join(' ', _recentWords.Append(current));
            var phraseDecision = _scorer.Evaluate(phrase, Math.Max(0.56, _settings.CorrectionConfidence - 0.12));
            if (phraseDecision.ShouldCorrect) decision = phraseDecision;
            else if (decision.ShouldCorrect && phraseDecision.Confidence >= 0.52)
            {
                var converted = decision.Language == TextLanguage.Russian
                    ? LayoutConverter.ToRussian(phrase)
                    : LayoutConverter.ToEnglish(phrase);
                decision = new(true, phrase, converted, decision.Language, decision.Confidence);
            }
        }
        if (!decision.ShouldCorrect) return false;
        _injection.ReplacePrevious(decision.Original, decision.Replacement, decision.Language, active.Handle);
        _lastCorrection = new(decision.Original, decision.Replacement, decision.Confidence);
        _lastCorrectionUtc = DateTime.UtcNow;
        _recentWords.Clear();
        Corrected?.Invoke(this, _lastCorrection);
        return true;
    }

    private bool TryEarlyCorrection(ActiveWindowInfo active)
    {
        if (_word.Length < 4 || _recentWords.Count > 0) return false;
        var decision = _scorer.Evaluate(_word.ToString(), Math.Max(0.9, _settings.CorrectionConfidence + 0.15));
        if (!decision.ShouldCorrect) return false;
        // The current key has not reached the target window yet, so only the preceding characters are deleted.
        _injection.ReplacePreviousCharacters(decision.Original.Length - 1, decision.Replacement, decision.Language, active.Handle);
        _lastCorrection = new(decision.Original, decision.Replacement, decision.Confidence);
        _lastCorrectionUtc = DateTime.UtcNow;
        _word.Clear(); _recentWords.Clear();
        Corrected?.Invoke(this, _lastCorrection);
        return true;
    }

    private void RememberWord(string word)
    {
        var currentIsCyrillic = word.Any(c => c is >= 'А' and <= 'я' or 'Ё' or 'ё');
        if (_recentWords.Count > 0)
        {
            var previousIsCyrillic = _recentWords[^1].Any(c => c is >= 'А' and <= 'я' or 'Ё' or 'ё');
            if (currentIsCyrillic != previousIsCyrillic) _recentWords.Clear();
        }
        _recentWords.Add(word);
        if (_recentWords.Count > 23) _recentWords.RemoveAt(0);
    }

    private bool TryUndo()
    {
        if (_lastCorrection is null || DateTime.UtcNow - _lastCorrectionUtc > TimeSpan.FromSeconds(8)) return false;
        var active = _activeWindow.GetActiveWindow();
        _injection.ReplacePrevious(_lastCorrection.Replacement, _lastCorrection.Original, TextLanguage.Unknown, active.Handle);
        _lastCorrection = null;
        return true;
    }

    private static bool TryGetCharacter(uint vk, uint scan, uint thread, out char character)
    {
        var layout = NativeMethods.GetKeyboardLayout(thread);
        if (TryGetKnownLayoutCharacter(vk, layout, out character)) return true;

        var state = new byte[256];
        var buffer = new StringBuilder(8);
        NativeMethods.GetKeyboardState(state);
        // WH_KEYBOARD_LL runs before Windows updates the asynchronous key state.
        // Mark the current key as pressed so ToUnicodeEx is deterministic even for fast/injected input.
        if (vk < state.Length) state[vk] |= 0x80;
        var effectiveScan = scan == 0 ? NativeMethods.MapVirtualKeyEx(vk, 0, layout) : scan;
        var result = NativeMethods.ToUnicodeEx(vk, effectiveScan, state, buffer, buffer.Capacity, 0, layout);
        character = result > 0 ? buffer[0] : '\0';
        if (result > 0 && char.IsLetter(character))
        {
            var upper = Modifier(NativeMethods.VkLShift, NativeMethods.VkRShift) ^
                        ((NativeMethods.GetKeyState(NativeMethods.VkCapital) & 1) != 0);
            character = upper ? char.ToUpper(character) : char.ToLower(character);
        }
        return result > 0;
    }

    private static bool TryGetKnownLayoutCharacter(uint vk, IntPtr layout, out char character)
    {
        var shift = Modifier(NativeMethods.VkLShift, NativeMethods.VkRShift);
        var caps = (NativeMethods.GetKeyState(NativeMethods.VkCapital) & 1) != 0;
        char physical;
        if (vk is >= 0x41 and <= 0x5A)
        {
            physical = (char)vk;
            physical = shift ^ caps ? char.ToUpperInvariant(physical) : char.ToLowerInvariant(physical);
        }
        else
        {
            physical = vk switch
            {
                0xC0 => shift ? '~' : '`',
                0xDB => shift ? '{' : '[',
                0xDD => shift ? '}' : ']',
                0xBA => shift ? ':' : ';',
                0xDE => shift ? '"' : '\'',
                0xBC => shift ? '<' : ',',
                0xBE => shift ? '>' : '.',
                _ => '\0'
            };
            if (physical == '\0') { character = '\0'; return false; }
        }

        var languageId = unchecked((ushort)layout.ToInt64());
        character = languageId == 0x0419 ? LayoutConverter.ToRussian(physical.ToString())[0] : physical;
        return languageId is 0x0409 or 0x0419;
    }

    private static bool Modifier(int left, int right) =>
        (NativeMethods.GetAsyncKeyState(left) & 0x8000) != 0 || (NativeMethods.GetAsyncKeyState(right) & 0x8000) != 0;

    private void RefreshHotkeys()
    {
        if (HotkeyGesture.TryParse(_settings.DictationHotkey, out var dictation, out _)) _dictationHotkey = dictation;
        if (HotkeyGesture.TryParse(_settings.UndoHotkey, out var undo, out _)) _undoHotkey = undo;
    }

    private static bool MatchesHotkey(int vk, HotkeyGesture gesture) =>
        string.Equals(VirtualKeyName(vk), gesture.Key, StringComparison.OrdinalIgnoreCase) && CurrentModifiers() == gesture.Modifiers;

    private static bool IsRequiredModifier(int vk, HotkeyModifiers modifiers) =>
        (vk is NativeMethods.VkLControl or NativeMethods.VkRControl && modifiers.HasFlag(HotkeyModifiers.Ctrl)) ||
        (vk is NativeMethods.VkLMenu or NativeMethods.VkRMenu && modifiers.HasFlag(HotkeyModifiers.Alt)) ||
        (vk is NativeMethods.VkLShift or NativeMethods.VkRShift && modifiers.HasFlag(HotkeyModifiers.Shift));

    private static HotkeyModifiers CurrentModifiers()
    {
        var result = HotkeyModifiers.None;
        if (Modifier(NativeMethods.VkLControl, NativeMethods.VkRControl)) result |= HotkeyModifiers.Ctrl;
        if (Modifier(NativeMethods.VkLMenu, NativeMethods.VkRMenu)) result |= HotkeyModifiers.Alt;
        if (Modifier(NativeMethods.VkLShift, NativeMethods.VkRShift)) result |= HotkeyModifiers.Shift;
        return result;
    }

    private static string VirtualKeyName(int vk) => vk switch
    {
        NativeMethods.VkSpace => "Space", NativeMethods.VkBack => "Backspace",
        NativeMethods.VkReturn => "Enter", NativeMethods.VkTab => "Tab", NativeMethods.VkEscape => "Escape",
        0x2D => "Insert", 0x2E => "Delete", 0x24 => "Home", 0x23 => "End",
        0x21 => "PageUp", 0x22 => "PageDown", 0x26 => "Up", 0x28 => "Down", 0x25 => "Left", 0x27 => "Right",
        >= 0x41 and <= 0x5A => ((char)vk).ToString(), >= 0x30 and <= 0x39 => ((char)vk).ToString(),
        >= 0x70 and <= 0x87 => $"F{vk - 0x6F}", _ => $"VK{vk:X2}"
    };

    public void Dispose()
    {
        if (_hook != IntPtr.Zero) NativeMethods.UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
        GC.SuppressFinalize(this);
    }
}
