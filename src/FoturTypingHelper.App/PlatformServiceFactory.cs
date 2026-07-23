using FoturTypingHelper.Core;
using FoturTypingHelper.Mac;
using FoturTypingHelper.Windows;

namespace FoturTypingHelper.App;

internal sealed record PlatformServices(IKeyboardService Keyboard, IAudioRecorder Audio, IDictationService Dictation,
    ITextInjectionService Injection, IActiveWindowService ActiveWindow, IAutostartService Autostart);

internal static class PlatformServiceFactory
{
    public static PlatformServices Create(AppSettings settings)
    {
        if (OperatingSystem.IsMacOS())
        {
            var injection = new MacTextInjectionService();
            return new(new MacKeyboardService(settings, injection), new MacAudioRecorder(), new MacLocalDictationService(),
                injection, new MacActiveWindowService(), new MacAutostartService());
        }
        var winInjection = new TextInjectionService(); var active = new ActiveWindowService();
        return new(new KeyboardHookService(settings, active, winInjection), new AudioRecorder(), new LocalDictationService(),
            winInjection, active, new AutostartService());
    }

    public static IDictationService CreateDictationService() =>
        OperatingSystem.IsMacOS() ? new MacLocalDictationService() : new LocalDictationService();
}
