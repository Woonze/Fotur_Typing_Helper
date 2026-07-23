using FoturTypingHelper.Core;

namespace FoturTypingHelper.Mac;

public sealed class MacActiveWindowService : IActiveWindowService { public nint GetActiveWindowHandle() => 0; }

public sealed class MacAutostartService : IAutostartService
{
    public void SetEnabled(bool enabled)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents", "tech.fotur.typinghelper.plist");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!enabled) { if (File.Exists(path)) File.Delete(path); return; }
        var executable = Environment.ProcessPath ?? "";
        var marker = executable.IndexOf(".app/Contents/MacOS", StringComparison.Ordinal);
        var bundle = marker >= 0 ? executable[..(marker + 4)] : executable;
        File.WriteAllText(path, $"""<?xml version="1.0" encoding="UTF-8"?><!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd"><plist version="1.0"><dict><key>Label</key><string>tech.fotur.typinghelper</string><key>ProgramArguments</key><array><string>/usr/bin/open</string><string>-g</string><string>{System.Security.SecurityElement.Escape(bundle)}</string><string>--args</string><string>--background</string></array><key>RunAtLoad</key><true/></dict></plist>""");
    }
}
