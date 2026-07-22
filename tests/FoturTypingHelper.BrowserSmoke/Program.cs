using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using FoturTypingHelper.Core;

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
var readyFile = Path.Combine(settingsRoot, "ready");
Directory.CreateDirectory(settingsRoot);
await File.WriteAllTextAsync(Path.Combine(settingsRoot, "settings.json"),
    """{"Settings":{"AutoCorrectionEnabled":true,"EarlyCorrection":false,"CorrectionConfidence":0.72}}""");
var foturStart = new ProcessStartInfo(args[0]) { UseShellExecute = false };
foturStart.Environment["FOTUR_SETTINGS_ROOT"] = settingsRoot;
foturStart.Environment["FOTUR_INSTANCE_ID"] = $"browser-smoke-{Environment.ProcessId}";
foturStart.Environment["FOTUR_READY_FILE"] = readyFile;
using var fotur = Process.Start(foturStart);
for (var attempt = 0; attempt < 100 && !File.Exists(readyFile); attempt++)
{
    if (fotur?.HasExited == true)
        throw new InvalidOperationException($"Fotur exited before the keyboard hook was ready ({fotur.ExitCode}).");
    await Task.Delay(100);
}
if (!File.Exists(readyFile)) throw new TimeoutException("Fotur keyboard hook did not become ready in 10 seconds.");
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

if (!FocusWindow(window))
{
    Console.Error.WriteLine("Chrome test window could not become foreground.");
    TryStop(browser);
    TryStop(fotur);
    return 5;
}
var scenarios = SmokePhraseCatalog.All;
if (scenarios.Count != 150) throw new InvalidOperationException($"Expected exactly 150 phrases, got {scenarios.Count}.");

var failed = false;
var passedCount = 0;
for (var index = 0; index < scenarios.Count; index++)
{
    var scenario = scenarios[index];
    ClearField();
    await EnsureOppositeLayout(scenario.Target);
    foreach (var key in scenario.Keys)
    {
        SendKey(key);
        await Task.Delay(35);
    }
    // A word boundary is what tells a global keyboard hook that the phrase is complete.
    SendVirtualKey(0x20);
    await Task.Delay(450);
    var actual = GetTitle(window).Replace("RESULT:", "", StringComparison.Ordinal).TrimEnd();
    var separator = actual.IndexOf(" - Google Chrome", StringComparison.Ordinal);
    if (separator >= 0) actual = actual[..separator];
    var passed = actual == scenario.Expected;
    Console.WriteLine($"{index + 1:000}/150 {(passed ? "PASS" : "FAIL")}: {scenario.Expected}");
    if (!passed)
    {
        Console.WriteLine($"  ACTUAL:   {actual}");
        Console.WriteLine($"  EXPECTED: {scenario.Expected}");
        failed = true;
    }
    else passedCount++;
}
Console.WriteLine($"RESULT: {passedCount}/150 phrases passed.");

TryStop(browser);
TryStop(fotur);
try { Directory.Delete(profile, true); } catch { }
return failed ? 1 : 0;

void ClearField()
{
    if (!FocusWindow(window)) throw new InvalidOperationException("Chrome lost foreground focus.");
    if (Native.GetWindowRect(window, out var rect))
    {
        Native.SetCursorPos((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
        Native.MouseEvent(0x0002, 0, 0, 0, UIntPtr.Zero);
        Native.MouseEvent(0x0004, 0, 0, 0, UIntPtr.Zero);
    }
    Thread.Sleep(100);
    SendVirtualKey(0x0D);
    SendChord(0x11, 0x41);
    SendVirtualKey(0x08);
    Thread.Sleep(150);
}

bool FocusWindow(IntPtr handle)
{
    for (var attempt = 0; attempt < 12; attempt++)
    {
        Native.ShowWindow(handle, 9);
        Native.SetWindowPos(handle, new IntPtr(-1), 0, 0, 0, 0, 0x0003);
        Native.SetWindowPos(handle, new IntPtr(-2), 0, 0, 0, 0, 0x0003);
        Native.BringWindowToTop(handle);
        Native.SetForegroundWindow(handle);
        if (Native.GetWindowRect(handle, out var rect))
        {
            Native.SetCursorPos((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
            Native.MouseEvent(0x0002, 0, 0, 0, UIntPtr.Zero);
            Native.MouseEvent(0x0004, 0, 0, 0, UIntPtr.Zero);
        }
        if (Native.GetForegroundWindow() == handle) return true;
        Thread.Sleep(100);
    }
    return false;
}

void SendKey(char character)
{
    if (character is >= 'А' and <= 'я' or 'Ё' or 'ё')
        character = LayoutConverter.ToEnglish(character.ToString())[0];
    if (character == ' ') { SendVirtualKey(0x20); return; }
    if (character == ',') { SendVirtualKey(0xBC); return; }
    if (character == '.') { SendVirtualKey(0xBE); return; }
    if (character == ';') { SendVirtualKey(0xBA); return; }
    if (character == '\'') { SendVirtualKey(0xDE); return; }
    if (character == '[') { SendVirtualKey(0xDB); return; }
    if (character == ']') { SendVirtualKey(0xDD); return; }
    if (character == '`') { SendVirtualKey(0xC0); return; }
    if (character == '<') { SendChord(0x10, 0xBC); return; }
    if (character == '>') { SendChord(0x10, 0xBE); return; }
    if (character == ':') { SendChord(0x10, 0xBA); return; }
    if (character == '"') { SendChord(0x10, 0xDE); return; }
    if (character == '{') { SendChord(0x10, 0xDB); return; }
    if (character == '}') { SendChord(0x10, 0xDD); return; }
    if (character == '~') { SendChord(0x10, 0xC0); return; }
    if (character is >= 'A' and <= 'Z') { SendChord(0x10, character); return; }
    if (character is >= 'a' and <= 'z') { SendVirtualKey(char.ToUpperInvariant(character)); return; }
    throw new InvalidOperationException($"Unsupported test character: {character}");
}

async Task EnsureOppositeLayout(TargetLanguage target)
{
    var layoutId = target == TargetLanguage.Russian ? "00000409" : "00000419";
    var expectedLanguage = target == TargetLanguage.Russian ? 0x0409 : 0x0419;
    var layout = Native.LoadKeyboardLayout(layoutId, 1);
    Native.PostMessage(window, 0x0050, IntPtr.Zero, layout);
    var thread = Native.GetWindowThreadProcessId(window, out _);
    for (var attempt = 0; attempt < 20; attempt++)
    {
        await Task.Delay(25);
        if ((Native.GetKeyboardLayout(thread).ToInt64() & 0xFFFF) == expectedLanguage) return;
    }
    throw new InvalidOperationException($"Could not verify opposite keyboard layout {layoutId} before phrase.");
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
    [StructLayout(LayoutKind.Sequential)] internal struct Rect { internal int Left, Top, Right, Bottom; }
    internal delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetWindowText(IntPtr window, StringBuilder text, int maxCount);
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern bool BringWindowToTop(IntPtr window);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] internal static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll", EntryPoint = "mouse_event")] internal static extern void MouseEvent(uint flags, uint x, uint y, uint data, UIntPtr extraInfo);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr LoadKeyboardLayout(string id, uint flags);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] internal static extern IntPtr GetKeyboardLayout(uint threadId);
    [DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] internal static extern uint SendInput(uint count, Input[] inputs, int size);
}
