using System.Diagnostics;

namespace FoturTypingHelper.Mac;

public sealed record MacPermissionSnapshot(bool InputMonitoring, bool Accessibility);

public static class MacSystemSettings
{
    public static MacPermissionSnapshot GetPermissions()
    {
        if (!OperatingSystem.IsMacOS()) return new(true, true);
        return new(MacNative.CGPreflightListenEventAccess(), MacNative.CGPreflightPostEventAccess());
    }

    public static void OpenInputMonitoring() => Open("x-apple.systempreferences:com.apple.preference.security?Privacy_ListenEvent");
    public static void OpenAccessibility() => Open("x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility");
    public static void OpenMicrophone() => Open("x-apple.systempreferences:com.apple.preference.security?Privacy_Microphone");

    private static void Open(string uri)
    {
        if (!OperatingSystem.IsMacOS()) return;
        Process.Start(new ProcessStartInfo("open", uri) { UseShellExecute = false });
    }
}
