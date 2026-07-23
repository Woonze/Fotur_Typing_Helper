using System.Runtime.InteropServices;

namespace FoturTypingHelper.Mac;

internal static class MacNative
{
    internal const string ApplicationServices = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
    internal const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    internal const string Carbon = "/System/Library/Frameworks/Carbon.framework/Carbon";
    internal const string OpenAL = "/System/Library/Frameworks/OpenAL.framework/OpenAL";
    internal const ulong Marker = 0xF07A2026;
    internal const int KeyDown = 10, KeyUp = 11, FlagsChanged = 12;
    internal const int TapDisabledByTimeout = -2, TapDisabledByUserInput = -1;
    internal const int KeyboardEventKeycode = 9, EventSourceUserData = 42;
    internal const ulong Shift = 1UL << 17, Control = 1UL << 18, Alternate = 1UL << 19, Command = 1UL << 20;

    internal delegate nint EventTapCallback(nint proxy, int type, nint cgEvent, nint userInfo);

    [DllImport(ApplicationServices)] internal static extern bool CGPreflightListenEventAccess();
    [DllImport(ApplicationServices)] internal static extern bool CGRequestListenEventAccess();
    [DllImport(ApplicationServices)] internal static extern bool CGPreflightPostEventAccess();
    [DllImport(ApplicationServices)] internal static extern bool CGRequestPostEventAccess();
    [DllImport(ApplicationServices)] internal static extern nint CGEventTapCreate(uint tap, uint place, uint options, ulong mask, EventTapCallback callback, nint userInfo);
    [DllImport(ApplicationServices)] internal static extern void CGEventTapEnable(nint tap, bool enable);
    [DllImport(ApplicationServices)] internal static extern long CGEventGetIntegerValueField(nint cgEvent, int field);
    [DllImport(ApplicationServices)] internal static extern void CGEventSetIntegerValueField(nint cgEvent, int field, long value);
    [DllImport(ApplicationServices)] internal static extern ulong CGEventGetFlags(nint cgEvent);
    [DllImport(ApplicationServices)] internal static extern void CGEventKeyboardGetUnicodeString(nint cgEvent, nuint maxLength, out nuint actualLength, [Out] char[] unicodeString);
    [DllImport(ApplicationServices)] internal static extern nint CGEventCreateKeyboardEvent(nint source, ushort virtualKey, bool keyDown);
    [DllImport(ApplicationServices)] internal static extern void CGEventKeyboardSetUnicodeString(nint cgEvent, nuint length, char[] unicodeString);
    [DllImport(ApplicationServices)] internal static extern void CGEventPost(uint tap, nint cgEvent);
    [DllImport(CoreFoundation)] internal static extern nint CFMachPortCreateRunLoopSource(nint allocator, nint port, nint order);
    [DllImport(CoreFoundation)] internal static extern nint CFRunLoopGetMain();
    [DllImport(CoreFoundation)] internal static extern void CFRunLoopAddSource(nint runLoop, nint source, nint mode);
    [DllImport(CoreFoundation)] internal static extern void CFRelease(nint value);
    [DllImport(CoreFoundation)] internal static extern nint CFStringCreateWithCString(nint allocator, string text, uint encoding);
    [DllImport(Carbon)] internal static extern nint TISCopyInputSourceForLanguage(nint language);
    [DllImport(Carbon)] internal static extern int TISSelectInputSource(nint inputSource);

    [DllImport(OpenAL)] internal static extern nint alcCaptureOpenDevice(string? deviceName, uint frequency, int format, int bufferSize);
    [DllImport(OpenAL)] internal static extern void alcCaptureStart(nint device);
    [DllImport(OpenAL)] internal static extern void alcCaptureStop(nint device);
    [DllImport(OpenAL)] internal static extern bool alcCaptureCloseDevice(nint device);
    [DllImport(OpenAL)] internal static extern void alcCaptureSamples(nint device, nint buffer, int samples);
    [DllImport(OpenAL)] internal static extern void alcGetIntegerv(nint device, int parameter, int size, out int value);
}
