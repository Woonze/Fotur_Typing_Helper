using Microsoft.Win32;
using FoturTypingHelper.Core;

namespace FoturTypingHelper.Windows;

public sealed class AutostartService : IAutostartService
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "FoturTypingHelper";

    public void SetEnabled(bool enabled)
    {
        if (!OperatingSystem.IsWindows()) return;
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true) ?? Registry.CurrentUser.CreateSubKey(KeyPath);
        if (enabled)
            key.SetValue(ValueName, $"\"{Environment.ProcessPath}\" --background");
        else
            key.DeleteValue(ValueName, false);
    }
}
