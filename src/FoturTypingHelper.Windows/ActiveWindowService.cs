using System.Diagnostics;
using System.Text;

namespace FoturTypingHelper.Windows;

public sealed record ActiveWindowInfo(IntPtr Handle, uint ThreadId, string ProcessName, bool IsPasswordField);

public sealed class ActiveWindowService
{
    private ActiveWindowInfo? _cached;
    private long _cacheValidUntil;

    public ActiveWindowInfo GetActiveWindow()
    {
        var window = NativeMethods.GetForegroundWindow();
        var now = Environment.TickCount64;
        if (_cached is { } cached && cached.Handle == window && now < _cacheValidUntil)
            return cached;

        var thread = NativeMethods.GetWindowThreadProcessId(window, out var pid);
        var processName = "unknown";
        try { processName = Process.GetProcessById((int)pid).ProcessName.ToLowerInvariant(); } catch { }
        _cached = new(window, thread, processName, IsNativePasswordField(thread));
        _cacheValidUntil = now + 200;
        return _cached;
    }

    private static bool IsNativePasswordField(uint thread)
    {
        var info = new NativeMethods.GuiThreadInfo { Size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.GuiThreadInfo>() };
        if (!NativeMethods.GetGUIThreadInfo(thread, ref info) || info.Focus == IntPtr.Zero) return false;
        var className = new StringBuilder(128);
        NativeMethods.GetClassName(info.Focus, className, className.Capacity);
        if (!className.ToString().Contains("Edit", StringComparison.OrdinalIgnoreCase)) return false;
        return NativeMethods.SendMessageTimeout(
            info.Focus,
            NativeMethods.EmGetPasswordChar,
            IntPtr.Zero,
            IntPtr.Zero,
            NativeMethods.SmtoAbortIfHung,
            30,
            out var result) != IntPtr.Zero && result != IntPtr.Zero;
    }
}
