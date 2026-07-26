using FoturTypingHelper.Core;

namespace FoturTypingHelper.App;

internal sealed record PlatformServices(IKeyboardService Keyboard, IAudioRecorder Audio, IDictationService Dictation,
    ITextInjectionService Injection, IActiveWindowService ActiveWindow, IAutostartService Autostart);

internal static class PlatformServiceFactory
{
    public static PlatformServices Create(AppSettings settings)
    {
        if (OperatingSystem.IsMacOS())
        {
            var injection = Create<ITextInjectionService>("FoturTypingHelper.Mac.MacTextInjectionService, FoturTypingHelper.Mac");
            return new(
                Create<IKeyboardService>("FoturTypingHelper.Mac.MacKeyboardService, FoturTypingHelper.Mac", settings, injection),
                Create<IAudioRecorder>("FoturTypingHelper.Mac.MacAudioRecorder, FoturTypingHelper.Mac"),
                Create<IDictationService>("FoturTypingHelper.Mac.MacLocalDictationService, FoturTypingHelper.Mac"),
                injection,
                Create<IActiveWindowService>("FoturTypingHelper.Mac.MacActiveWindowService, FoturTypingHelper.Mac"),
                Create<IAutostartService>("FoturTypingHelper.Mac.MacAutostartService, FoturTypingHelper.Mac"));
        }
        if (OperatingSystem.IsLinux())
        {
            var injection = Create<ITextInjectionService>("FoturTypingHelper.Linux.LinuxTextInjectionService, FoturTypingHelper.Linux");
            return new(
                Create<IKeyboardService>("FoturTypingHelper.Linux.LinuxKeyboardService, FoturTypingHelper.Linux"),
                Create<IAudioRecorder>("FoturTypingHelper.Linux.LinuxAudioRecorder, FoturTypingHelper.Linux"),
                Create<IDictationService>("FoturTypingHelper.Linux.LinuxLocalDictationService, FoturTypingHelper.Linux"),
                injection,
                Create<IActiveWindowService>("FoturTypingHelper.Linux.LinuxActiveWindowService, FoturTypingHelper.Linux"),
                Create<IAutostartService>("FoturTypingHelper.Linux.LinuxAutostartService, FoturTypingHelper.Linux"));
        }
        var winInjection = Create<ITextInjectionService>("FoturTypingHelper.Windows.TextInjectionService, FoturTypingHelper.Windows");
        var active = Create<IActiveWindowService>("FoturTypingHelper.Windows.ActiveWindowService, FoturTypingHelper.Windows");
        return new(
            Create<IKeyboardService>("FoturTypingHelper.Windows.KeyboardHookService, FoturTypingHelper.Windows", settings, active, winInjection),
            Create<IAudioRecorder>("FoturTypingHelper.Windows.AudioRecorder, FoturTypingHelper.Windows"),
            Create<IDictationService>("FoturTypingHelper.Windows.LocalDictationService, FoturTypingHelper.Windows"),
            winInjection,
            active,
            Create<IAutostartService>("FoturTypingHelper.Windows.AutostartService, FoturTypingHelper.Windows"));
    }

    public static IDictationService CreateDictationService() =>
        OperatingSystem.IsMacOS() ? Create<IDictationService>("FoturTypingHelper.Mac.MacLocalDictationService, FoturTypingHelper.Mac")
        : OperatingSystem.IsLinux() ? Create<IDictationService>("FoturTypingHelper.Linux.LinuxLocalDictationService, FoturTypingHelper.Linux")
        : Create<IDictationService>("FoturTypingHelper.Windows.LocalDictationService, FoturTypingHelper.Windows");

    private static T Create<T>(string assemblyQualifiedTypeName, params object[] args)
    {
        var type = Type.GetType(assemblyQualifiedTypeName, throwOnError: true)!;
        return (T)Activator.CreateInstance(type, args)!;
    }
}
