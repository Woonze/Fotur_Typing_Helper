using Avalonia;
using System;
using System.Security.Cryptography;
using System.Text;

namespace FoturTypingHelper.App;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("--diagnose-whisper-runtime", StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var runtime = PlatformServiceFactory.CreateDictationService().GetRuntimeInfo();
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
        var namePrefix = OperatingSystem.IsWindows() ? @"Local\" : "";
        var mutexName = namePrefix + "FoturTypingHelper.SingleInstance" + instanceSuffix;
        var activateEventName = namePrefix + "FoturTypingHelper.Activate" + instanceSuffix;
        using var mutex = new Mutex(true, mutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            if (OperatingSystem.IsWindows()) FoturTypingHelper.Windows.ExistingInstanceActivator.TryActivate();
            if (OperatingSystem.IsWindows())
                try { EventWaitHandle.OpenExisting(activateEventName).Set(); } catch { }
            return;
        }

        using var activateEvent = OperatingSystem.IsWindows()
            ? new EventWaitHandle(false, EventResetMode.AutoReset, activateEventName)
            : null;
        if (activateEvent is not null) _ = Task.Run(() =>
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
