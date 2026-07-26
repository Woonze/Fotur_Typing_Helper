using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace FoturTypingHelper.App;

internal static class MacWindowInterop
{
    public static void MakeClickThrough(Window window)
    {
        if (!OperatingSystem.IsMacOS()) return;
        var handle = window.TryGetPlatformHandle();
        if (handle is null || !string.Equals(handle.HandleDescriptor, "NSWindow", StringComparison.OrdinalIgnoreCase))
            return;
        objc_msgSend(handle.Handle, sel_registerName("setIgnoresMouseEvents:"), true);
    }

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    private static extern nint sel_registerName(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend(nint receiver, nint selector, bool value);
}
