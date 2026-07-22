using Avalonia;
using System;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace FoturTypingHelper.App;

class Program
{
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

        var instanceSuffix = InstanceSuffix();
        var mutexName = @"Local\FoturTypingHelper.SingleInstance" + instanceSuffix;
        var activateEventName = @"Local\FoturTypingHelper.Activate" + instanceSuffix;
        using var mutex = new Mutex(true, mutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            FoturTypingHelper.Windows.ExistingInstanceActivator.TryActivate();
            try { EventWaitHandle.OpenExisting(activateEventName).Set(); } catch { }
            return;
        }

        using var activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, activateEventName);
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

    private static string InstanceSuffix()
    {
        var id = Environment.GetEnvironmentVariable("FOTUR_INSTANCE_ID");
        if (string.IsNullOrWhiteSpace(id)) return "";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(id));
        return "." + Convert.ToHexString(hash, 0, 6);
    }
}
