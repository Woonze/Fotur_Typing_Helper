namespace FoturTypingHelper.Core;

[Flags]
public enum HotkeyModifiers { None = 0, Ctrl = 1, Alt = 2, Shift = 4 }

public sealed record HotkeyGesture(HotkeyModifiers Modifiers, string Key)
{
    private static readonly HashSet<string> NamedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Space", "Backspace", "Enter", "Tab", "Escape", "Insert", "Delete", "Home", "End",
        "PageUp", "PageDown", "Up", "Down", "Left", "Right"
    };

    public static bool TryParse(string? value, out HotkeyGesture gesture, out string error)
    {
        gesture = new(HotkeyModifiers.None, "");
        error = "";
        if (string.IsNullOrWhiteSpace(value)) { error = "Сочетание не задано"; return false; }
        var parts = value.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var modifiers = HotkeyModifiers.None;
        string? key = null;
        foreach (var part in parts)
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl" or "control": modifiers |= HotkeyModifiers.Ctrl; break;
                case "alt": modifiers |= HotkeyModifiers.Alt; break;
                case "shift": modifiers |= HotkeyModifiers.Shift; break;
                default:
                    if (key is not null) { error = "Допускается только одна основная клавиша"; return false; }
                    key = NormalizeKey(part);
                    break;
            }
        }
        if (key is null) { error = "Добавьте основную клавишу"; return false; }
        if (modifiers == HotkeyModifiers.None) { error = "Добавьте Ctrl, Alt или Shift"; return false; }
        if (!IsSupportedKey(key)) { error = $"Клавиша {key} пока не поддерживается"; return false; }
        gesture = new(modifiers, key);
        return true;
    }

    public static string? ValidatePair(string dictation, string undo)
    {
        if (!TryParse(dictation, out var first, out var firstError)) return "Диктовка: " + firstError;
        if (!TryParse(undo, out var second, out var secondError)) return "Отмена: " + secondError;
        if (first == second) return "Сочетания диктовки и отмены совпадают";
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Alt+F4", "Ctrl+Shift+Escape", "Ctrl+Alt+Delete"
        };
        if (reserved.Contains(first.ToString()) || reserved.Contains(second.ToString()))
            return "Это сочетание зарезервировано Windows";
        return null;
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Ctrl)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        parts.Add(Key);
        return string.Join('+', parts);
    }

    private static string NormalizeKey(string key)
    {
        if (key.Length == 1) return key.ToUpperInvariant();
        return NamedKeys.FirstOrDefault(x => x.Equals(key, StringComparison.OrdinalIgnoreCase))
            ?? (key.StartsWith('F') ? key.ToUpperInvariant() : key);
    }

    private static bool IsSupportedKey(string key) =>
        key.Length == 1 && char.IsLetterOrDigit(key[0]) ||
        NamedKeys.Contains(key) ||
        key.Length is 2 or 3 && key[0] == 'F' && int.TryParse(key[1..], out var number) && number is >= 1 and <= 24;
}
