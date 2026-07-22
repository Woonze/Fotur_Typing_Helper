using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FoturTypingHelper.Core;

namespace FoturTypingHelper.App;

public partial class App : Application
{
    private AppRuntime? _runtime;
    private MainWindow? _window;
    private TrayIcon? _tray;
    public static bool ExitRequested { get; private set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public void RestoreMainWindow() => _window?.Restore();

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var settingsRoot = Environment.GetEnvironmentVariable("FOTUR_SETTINGS_ROOT");
            var store = new SettingsStore(string.IsNullOrWhiteSpace(settingsRoot) ? null : settingsRoot);
            var services = PlatformServiceFactory.Create(store.State.Settings);
            _runtime = new AppRuntime(store, services.Keyboard, services.Audio, services.Dictation, services.Injection, services.ActiveWindow);
            _window = new MainWindow(store, services.Autostart, _runtime);
            desktop.MainWindow = _window;
            CreateTray(desktop, store);
            desktop.Exit += (_, _) => { _runtime.Dispose(); _tray?.Dispose(); store.Save(); };
            _runtime.Start();
            _ = Task.Run(async () =>
            {
                try
                {
                    var updateStatus = await new GitHubUpdateService().CheckAndInstallAsync(store.State.Settings);
                    if (updateStatus is not null) _runtime.ReportStatus(updateStatus);
                }
                catch (Exception ex) { DiagnosticLog.Write("AutoUpdate", ex); _runtime.ReportStatus("Не удалось проверить обновления"); }
            });
            var readyFile = Environment.GetEnvironmentVariable("FOTUR_READY_FILE");
            if (!string.IsNullOrWhiteSpace(readyFile))
                File.WriteAllText(readyFile, "ready");

            if (desktop.Args?.Contains("--background") == true)
            {
                EventHandler? hideOnce = null;
                hideOnce = (_, _) => { _window.Opened -= hideOnce; _window.Hide(); };
                _window.Opened += hideOnce;
            }
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void CreateTray(IClassicDesktopStyleApplicationLifetime desktop, SettingsStore store)
    {
        var open = new NativeMenuItem("Открыть Fotur Typing Helper");
        open.Click += (_, _) => _window?.Restore();
        var correction = new NativeMenuItem("Автокоррекция") { ToggleType = NativeMenuItemToggleType.CheckBox, IsChecked = store.State.Settings.AutoCorrectionEnabled };
        correction.Click += (_, _) => { store.State.Settings.AutoCorrectionEnabled = correction.IsChecked; store.Save(); };
        var dictation = new NativeMenuItem("Диктовка") { ToggleType = NativeMenuItemToggleType.CheckBox, IsChecked = store.State.Settings.DictationEnabled };
        dictation.Click += (_, _) => { store.State.Settings.DictationEnabled = dictation.IsChecked; store.Save(); };
        var exit = new NativeMenuItem("Выход");
        exit.Click += (_, _) => { ExitRequested = true; _window?.Close(); desktop.Shutdown(); };

        _tray = new TrayIcon
        {
            Icon = IconFactory.Create(), ToolTipText = "Fotur Typing Helper",
            Menu = new NativeMenu { Items = { open, new NativeMenuItemSeparator(), correction, dictation, new NativeMenuItemSeparator(), exit } }
        };
        _tray.Clicked += (_, _) => _window?.Restore();
        TrayIcon.SetIcons(this, new TrayIcons { _tray });
    }
}
