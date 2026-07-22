using FoturTypingHelper.Windows;

namespace FoturTypingHelper.Tests;

public sealed class DictationRuntimeTests
{
    [Fact]
    public void WhisperNativeRuntime_LoadsOnWindows()
    {
        if (!OperatingSystem.IsWindows()) return;
        var info = new LocalDictationService().GetRuntimeInfo();
        Assert.False(string.IsNullOrWhiteSpace(info));
    }
}
