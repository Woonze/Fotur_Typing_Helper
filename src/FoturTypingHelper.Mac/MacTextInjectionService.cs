using FoturTypingHelper.Core;

namespace FoturTypingHelper.Mac;

public sealed class MacTextInjectionService : ITextInjectionService
{
    public bool ActivateWindow(nint window) => true;
    public bool CanPostEvents => MacNative.CGPreflightPostEventAccess();
    public bool SendText(string text)
    {
        if (!EnsurePostAccess()) return false;
        PostUnicode(text);
        return true;
    }

    internal bool ReplacePrevious(string original, string replacement, TextLanguage language)
    {
        if (!EnsurePostAccess()) return false;
        for (var i = 0; i < original.Length; i++) PostKey(51);
        PostUnicode(replacement);
        SwitchLayout(language);
        return true;
    }

    private static bool EnsurePostAccess()
    {
        if (MacNative.CGPreflightPostEventAccess()) return true;
        MacNative.CGRequestPostEventAccess();
        return MacNative.CGPreflightPostEventAccess();
    }

    private static void PostKey(ushort key)
    {
        foreach (var down in new[] { true, false })
        {
            var e = MacNative.CGEventCreateKeyboardEvent(0, key, down);
            MacNative.CGEventSetIntegerValueField(e, MacNative.EventSourceUserData, (long)MacNative.Marker);
            MacNative.CGEventPost(0, e); MacNative.CFRelease(e);
        }
    }

    private static void PostUnicode(string text)
    {
        foreach (var character in text)
        {
            foreach (var down in new[] { true, false })
            {
                var e = MacNative.CGEventCreateKeyboardEvent(0, 0, down);
                MacNative.CGEventSetIntegerValueField(e, MacNative.EventSourceUserData, (long)MacNative.Marker);
                MacNative.CGEventKeyboardSetUnicodeString(e, 1, [character]);
                MacNative.CGEventPost(0, e); MacNative.CFRelease(e);
            }
        }
    }

    private static void SwitchLayout(TextLanguage language)
    {
        if (language == TextLanguage.Unknown) return;
        var code = MacNative.CFStringCreateWithCString(0, language == TextLanguage.Russian ? "ru" : "en", 0x08000100);
        var source = MacNative.TISCopyInputSourceForLanguage(code);
        if (source != 0) { MacNative.TISSelectInputSource(source); MacNative.CFRelease(source); }
        MacNative.CFRelease(code);
    }
}
