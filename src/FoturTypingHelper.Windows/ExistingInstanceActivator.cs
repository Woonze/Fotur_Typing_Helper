using System.Text;

namespace FoturTypingHelper.Windows;

public static class ExistingInstanceActivator
{
    public static bool TryActivate()
    {
        var found = IntPtr.Zero;
        NativeMethods.EnumWindows((window, _) =>
        {
            var title = new StringBuilder(256);
            NativeMethods.GetWindowText(window, title, title.Capacity);
            if (!string.Equals(title.ToString(), "Fotur Typing Helper", StringComparison.Ordinal)) return true;
            found = window;
            return false;
        }, IntPtr.Zero);
        if (found == IntPtr.Zero) return false;
        NativeMethods.ShowWindow(found, 9);
        return NativeMethods.SetForegroundWindow(found);
    }
}
