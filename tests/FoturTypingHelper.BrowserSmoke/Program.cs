using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

if (!OperatingSystem.IsWindows()) return 0;
if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: BrowserSmoke <published FoturTypingHelper.App.exe>");
    return 2;
}

var chrome = new[]
{
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe")
}.FirstOrDefault(File.Exists);
if (chrome is null)
{
    Console.Error.WriteLine("Google Chrome not found.");
    return 3;
}

var testPage = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "input-test.html"));
var profile = Path.Combine(Path.GetTempPath(), $"fotur-browser-smoke-{Environment.ProcessId}");
var settingsRoot = Path.Combine(profile, "fotur-settings");
Directory.CreateDirectory(settingsRoot);
await File.WriteAllTextAsync(Path.Combine(settingsRoot, "settings.json"),
    """{"Settings":{"AutoCorrectionEnabled":true,"EarlyCorrection":false,"CorrectionConfidence":0.72}}""");
var foturStart = new ProcessStartInfo(args[0]) { UseShellExecute = false };
foturStart.Environment["FOTUR_SETTINGS_ROOT"] = settingsRoot;
using var fotur = Process.Start(foturStart);
await Task.Delay(1500);
using var browser = Process.Start(new ProcessStartInfo(chrome)
{
    UseShellExecute = false,
    ArgumentList =
    {
        $"--user-data-dir={profile}", "--no-first-run", "--disable-default-apps", "--disable-sync",
        "--disable-search-engine-choice-screen", $"--app={new Uri(testPage).AbsoluteUri}"
    }
});

var window = IntPtr.Zero;
for (var attempt = 0; attempt < 60 && window == IntPtr.Zero; attempt++)
{
    await Task.Delay(100);
    window = FindTestWindow();
}
if (window == IntPtr.Zero)
{
    Console.Error.WriteLine("Chrome test window not found.");
    TryStop(browser);
    TryStop(fotur);
    return 4;
}

Native.ShowWindow(window, 9);
Native.SetForegroundWindow(window);
var english = Native.LoadKeyboardLayout("00000409", 1);
Native.PostMessage(window, 0x0050, IntPtr.Zero, english);
await Task.Delay(500);

var scenarios = new[]
{
    (Keys: "ghbdtn rfr ltkf ", Expected: "привет как дела"),
    (Keys: "rjulf z ghble ljvjq ", Expected: "когда я приду домой"),
    (Keys: "lfdfq gjghj,etv rfr ,scnhj hf,jnftn ghjuhfvvf ", Expected: "давай попробуем как быстро работает программа")
};

var failed = false;
foreach (var scenario in scenarios)
{
    ClearField();
    foreach (var key in scenario.Keys)
    {
        SendKey(key);
        await Task.Delay(42);
    }
    await Task.Delay(700);
    var actual = GetTitle(window).Replace("RESULT:", "", StringComparison.Ordinal);
    var separator = actual.IndexOf(" - Google Chrome", StringComparison.Ordinal);
    if (separator >= 0) actual = actual[..separator];
    var passed = actual == scenario.Expected;
    Console.WriteLine($"{(passed ? "PASS" : "FAIL")}: {scenario.Keys} => {actual}");
    if (!passed)
    {
        Console.WriteLine($"EXPECTED: {scenario.Expected}");
        failed = true;
    }
}

TryStop(browser);
TryStop(fotur);
try { Directory.Delete(profile, true); } catch { }
return failed ? 1 : 0;

void ClearField()
{
    SendChord(0x11, 0x41);
    SendVirtualKey(0x08);
    Thread.Sleep(150);
}

void SendKey(char character)
{
    if (character == ' ') { SendVirtualKey(0x20); return; }
    if (character == ',') { SendVirtualKey(0xBC); return; }
    if (character is >= 'a' and <= 'z') { SendVirtualKey(char.ToUpperInvariant(character)); return; }
    throw new InvalidOperationException($"Unsupported test character: {character}");
}

void SendChord(ushort modifier, ushort key)
{
    SendInputs([Input.Down(modifier), Input.Down(key), Input.Up(key), Input.Up(modifier)]);
}

void SendVirtualKey(ushort key) => SendInputs([Input.Down(key), Input.Up(key)]);

void SendInputs(Input[] inputs)
{
    var sent = Native.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
    if (sent != inputs.Length) throw new InvalidOperationException($"SendInput sent {sent}/{inputs.Length} events.");
}

IntPtr FindTestWindow()
{
    var result = IntPtr.Zero;
    Native.EnumWindows((handle, _) =>
    {
        if (GetTitle(handle).Contains("FOTUR INPUT TEST", StringComparison.Ordinal) ||
            GetTitle(handle).StartsWith("RESULT:", StringComparison.Ordinal))
        {
            result = handle;
            return false;
        }
        return true;
    }, IntPtr.Zero);
    return result;
}

string GetTitle(IntPtr handle)
{
    var text = new StringBuilder(2048);
    Native.GetWindowText(handle, text, text.Capacity);
    return text.ToString();
}

void TryStop(Process? process)
{
    try { if (process is { HasExited: false }) process.Kill(true); } catch { }
}

[StructLayout(LayoutKind.Sequential)]
struct Input
{
    public uint Type;
    public InputUnion Union;
    public static Input Down(ushort key) => new() { Type = 1, Union = new() { Keyboard = new() { Vk = key } } };
    public static Input Up(ushort key) => new() { Type = 1, Union = new() { Keyboard = new() { Vk = key, Flags = 2 } } };
}

[StructLayout(LayoutKind.Explicit)]
struct InputUnion
{
    [FieldOffset(0)] public KeyboardInput Keyboard;
    [FieldOffset(0)] public MouseInput Mouse;
    [FieldOffset(0)] public HardwareInput Hardware;
}

[StructLayout(LayoutKind.Sequential)]
struct KeyboardInput
{
    public ushort Vk;
    public ushort Scan;
    public uint Flags;
    public uint Time;
    public UIntPtr ExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
struct MouseInput
{
    public int X, Y;
    public uint MouseData, Flags, Time;
    public UIntPtr ExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
struct HardwareInput
{
    public uint Message;
    public ushort ParameterLow, ParameterHigh;
}

static class Native
{
    internal delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetWindowText(IntPtr window, StringBuilder text, int maxCount);
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr LoadKeyboardLayout(string id, uint flags);
    [DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] internal static extern uint SendInput(uint count, Input[] inputs, int size);
}
