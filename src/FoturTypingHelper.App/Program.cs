using Avalonia;
using System;
using System.Runtime.Versioning;

namespace FoturTypingHelper.App;

class Program
{
    private const string MutexName = @"Local\FoturTypingHelper.SingleInstance";
    private const string ActivateEventName = @"Local\FoturTypingHelper.Activate";
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    [SupportedOSPlatform("windows")]
    public static void Main(string[] args)
    {
        if (args.Contains("--diagnose-whisper-runtime", StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var runtime = new FoturTypingHelper.Windows.LocalDictationService().GetRuntimeInfo();
                FoturTypingHelper.Core.DiagnosticLog.WriteMessage("WhisperRuntime", runtime);
            }
            catch (Exception ex)
            {
                FoturTypingHelper.Core.DiagnosticLog.Write("WhisperRuntime", ex);
                Environment.ExitCode = 2;
            }
            return;
        }

        using var mutex = new Mutex(true, MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            FoturTypingHelper.Windows.ExistingInstanceActivator.TryActivate();
            try { EventWaitHandle.OpenExisting(ActivateEventName).Set(); } catch { }
            return;
        }

        using var activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
        _ = Task.Run(() =>
        {
            while (true)
            {
                try { activateEvent.WaitOne(); }
                catch (ObjectDisposedException) { return; }
                Avalonia.Threading.Dispatcher.UIThread.Post(() => (Application.Current as App)?.RestoreMainWindow());
            }
        });
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
