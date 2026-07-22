namespace FoturTypingHelper.Core;

public static class LayoutConverter
{
    private const string English = "`qwertyuiop[]asdfghjkl;'zxcvbnm,.~QWERTYUIOP{}ASDFGHJKL:\"ZXCVBNM<>";
    private const string Russian = "ёйцукенгшщзхъфывапролджэячсмитьбюЁЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮ";

    private static readonly IReadOnlyDictionary<char, char> EnToRu = English
        .Zip(Russian, (a, b) => (a, b)).ToDictionary(x => x.a, x => x.b);
    private static readonly IReadOnlyDictionary<char, char> RuToEn = Russian
        .Zip(English, (a, b) => (a, b)).ToDictionary(x => x.a, x => x.b);

    public static string ToRussian(string text) => Convert(text, EnToRu);
    public static string ToEnglish(string text) => Convert(text, RuToEn);

    private static string Convert(string text, IReadOnlyDictionary<char, char> map) =>
        string.Create(text.Length, (text, map), static (span, state) =>
        {
            for (var i = 0; i < state.text.Length; i++)
                span[i] = state.map.TryGetValue(state.text[i], out var replacement) ? replacement : state.text[i];
        });
}
