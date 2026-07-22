using System.Text.RegularExpressions;

namespace FoturTypingHelper.Core;

public static partial class VoiceCommandProcessor
{
    private static readonly (Regex Pattern, string Replacement)[] Commands =
    [
        (Command(@"новая строка|new line"), Environment.NewLine),
        (Command(@"точка|period|full stop"), "."),
        (Command(@"запятая|comma"), ","),
        (Command(@"вопросительный знак|question mark"), "?"),
        (Command(@"восклицательный знак|exclamation mark"), "!")
    ];

    public static string Process(string text, bool enabled)
    {
        var result = text.Trim();
        if (enabled)
            foreach (var (pattern, replacement) in Commands)
                result = pattern.Replace(result, replacement);

        result = SpaceBeforePunctuation().Replace(result, "$1");
        result = MultipleSpaces().Replace(result, " ").Trim();
        if (result.Length > 0) result = char.ToUpper(result[0]) + result[1..];
        return result;
    }

    private static Regex Command(string value) => new($@"\b(?:{value})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    [GeneratedRegex(@"\s+([,.!?])")]
    private static partial Regex SpaceBeforePunctuation();
    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex MultipleSpaces();
}
