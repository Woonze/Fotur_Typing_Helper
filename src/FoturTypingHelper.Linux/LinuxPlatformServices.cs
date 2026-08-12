using System.Diagnostics;
using System.Runtime.InteropServices;
using FoturTypingHelper.Core;
using Whisper.net;
using Whisper.net.Ggml;

namespace FoturTypingHelper.Linux;

public sealed class LinuxKeyboardService : IKeyboardService
{
    private readonly AppSettings _settings;
    private readonly LinuxTextInjectionService _injection;
    private LanguageScorer _scorer;
    private readonly System.Text.StringBuilder _word = new();
    private readonly List<string> _recentWords = [];
    private readonly object _gate = new();
    private Process? _xinput;
    private CancellationTokenSource? _cts;
    private LinuxX11KeyboardMapper? _mapper;
    private HotkeyGesture _dictationHotkey = new(HotkeyModifiers.Ctrl | HotkeyModifiers.Alt, "Space");
    private HotkeyGesture _undoHotkey = new(HotkeyModifiers.Ctrl | HotkeyModifiers.Alt, "Backspace");
    private CorrectionApplied? _lastCorrection;
    private DateTime _lastCorrectionUtc;
    private DateTime _ignoreEventsUntilUtc;
    private long _inputRevision;
    private bool _dictationGestureActive;
    private bool _dictationKeyCaptured;
    private LinuxRawKeyEventKind? _pendingKind;

    public event EventHandler<CorrectionApplied>? Corrected;
    public event EventHandler<bool>? DictationHotkeyChanged;
    public event EventHandler<string>? StatusChanged;

    public LinuxKeyboardService(AppSettings settings, LinuxTextInjectionService injection)
    {
        _settings = settings;
        _injection = injection;
        _scorer = new LanguageScorer(settings.CustomDictionary);
        RefreshHotkeys();
    }

    public void Start()
    {
        if (!OperatingSystem.IsLinux() || _xinput is { HasExited: false }) return;
        if (IsWayland())
        {
            StatusChanged?.Invoke(this, "Linux Wayland: диктовка работает, глобальная автокоррекция и хоткеи требуют X11/XWayland input-доступа.");
            return;
        }
        if (!CommandExists("xinput"))
        {
            StatusChanged?.Invoke(this, "Linux X11: установите xinput для глобальной автокоррекции и хоткеев.");
            return;
        }
        if (!CommandExists("xdotool"))
        {
            StatusChanged?.Invoke(this, "Linux X11: установите xdotool для замены текста и диктовки в активное поле.");
            return;
        }

        _mapper = new LinuxX11KeyboardMapper();
        _cts = new CancellationTokenSource();
        _xinput = Process.Start(new ProcessStartInfo("xinput", "test-xi2 --root")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });
        if (_xinput is null)
        {
            StatusChanged?.Invoke(this, "Linux X11: не удалось запустить xinput.");
            return;
        }
        _ = Task.Run(() => ReadXInputAsync(_xinput, _cts.Token));
        StatusChanged?.Invoke(this, "Linux X11: глобальная автокоррекция и хоткеи активны.");
    }

    public void RefreshSettings()
    {
        _scorer = new LanguageScorer(_settings.CustomDictionary);
        RefreshHotkeys();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        if (_xinput is { HasExited: false }) _xinput.Kill(entireProcessTree: true);
        _xinput?.Dispose();
        _cts?.Dispose();
        _mapper?.Dispose();
    }

    private async Task ReadXInputAsync(Process process, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && !process.StandardOutput.EndOfStream)
            {
                var line = await process.StandardOutput.ReadLineAsync(token);
                if (line is null) break;
                ProcessXInputLine(line);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, "Linux X11: keyboard hook остановлен: " + ex.Message);
        }
    }

    private void ProcessXInputLine(string line)
    {
        if (line.Contains("RawKeyPress", StringComparison.Ordinal)) { _pendingKind = LinuxRawKeyEventKind.Down; return; }
        if (line.Contains("RawKeyRelease", StringComparison.Ordinal)) { _pendingKind = LinuxRawKeyEventKind.Up; return; }
        if (_pendingKind is null || !line.TrimStart().StartsWith("detail:", StringComparison.Ordinal)) return;
        if (!int.TryParse(line[(line.IndexOf(':') + 1)..].Trim(), out var keycode)) { _pendingKind = null; return; }
        var kind = _pendingKind.Value;
        _pendingKind = null;
        HandleKey(keycode, kind == LinuxRawKeyEventKind.Down);
    }

    private void HandleKey(int keycode, bool down)
    {
        if (_mapper is null || DateTime.UtcNow < _ignoreEventsUntilUtc) return;
        var snapshot = _mapper.ReadKey(keycode);
        if (snapshot is null) return;
        if (down) Interlocked.Increment(ref _inputRevision);

        if (down && MatchesHotkey(snapshot.KeyName, snapshot.Modifiers, _dictationHotkey))
        {
            if (!_dictationGestureActive)
            {
                _dictationGestureActive = true;
                _dictationKeyCaptured = true;
                DictationHotkeyChanged?.Invoke(this, true);
            }
            return;
        }
        if (!down && _dictationKeyCaptured && string.Equals(snapshot.KeyName, _dictationHotkey.Key, StringComparison.OrdinalIgnoreCase))
        {
            _dictationKeyCaptured = false;
            if (_dictationGestureActive) DictationHotkeyChanged?.Invoke(this, false);
            _dictationGestureActive = false;
            return;
        }
        if (!down) return;
        if (MatchesHotkey(snapshot.KeyName, snapshot.Modifiers, _undoHotkey) && TryUndo()) return;
        if (!_settings.AutoCorrectionEnabled) return;
        if (snapshot.Modifiers is not HotkeyModifiers.None and not HotkeyModifiers.Shift) return;

        lock (_gate)
        {
            if (snapshot.KeyName == "Backspace")
            {
                if (_word.Length > 0) _word.Length--;
                _recentWords.Clear();
                return;
            }
            if (snapshot.KeyName is "Delete" or "Escape" or "Home" or "End" or "PageUp" or "PageDown" or "Left" or "Up" or "Right" or "Down")
            {
                _word.Clear();
                _recentWords.Clear();
                return;
            }
            if (snapshot.KeyName == "Space")
            {
                if (_word.Length == 0) _recentWords.Clear();
                else _ = EvaluateAndReplaceAfterTargetAsync(" ");
                return;
            }
            if (snapshot.KeyName is "Return" or "Tab")
            {
                _ = EvaluateAndReplaceAfterTargetAsync(snapshot.KeyName == "Tab" ? "\t" : "\n");
                _word.Clear();
                _recentWords.Clear();
                return;
            }
            if (snapshot.Character is { } character)
            {
                if (char.IsLetter(character) || character is '\'' or '-' || LayoutConverter.IsConvertible(character))
                {
                    _word.Append(character);
                }
                else
                {
                    _ = EvaluateAndReplaceAfterTargetAsync(character.ToString());
                    _word.Clear();
                    _recentWords.Clear();
                }
            }
        }
    }

    private async Task EvaluateAndReplaceAfterTargetAsync(string delimiter)
    {
        string current;
        string phrase;
        bool hasRecentWords;
        long revision;
        lock (_gate)
        {
            if (_word.Length < 2) return;
            current = _word.ToString();
            hasRecentWords = _recentWords.Count > 0;
            phrase = _recentWords.Count > 0 ? string.Join(' ', _recentWords.Append(current)) : current;
            _word.Clear();
            revision = Volatile.Read(ref _inputRevision);
        }

        // XInput2 raw events are notifications, not suppressible hooks. Give the target app a
        // short moment to receive the original delimiter, then replace the visible text in-place.
        await Task.Delay(45);
        // Do not race a user who continued typing, moved the caret or edited the field.
        if (revision != Volatile.Read(ref _inputRevision)) return;

        var decision = _scorer.Evaluate(current, _settings.CorrectionConfidence);
        if (hasRecentWords)
        {
            var phraseDecision = _scorer.Evaluate(phrase, Math.Max(0.56, _settings.CorrectionConfidence - 0.12));
            if (phraseDecision.ShouldCorrect) decision = phraseDecision;
        }
        if (!decision.ShouldCorrect)
        {
            lock (_gate) RememberWord(current);
            return;
        }

        var replacement = decision.Replacement + delimiter;
        var charactersToDelete = decision.Original.Length + delimiter.Length;
        if (_injection.ReplacePreviousCharacters(charactersToDelete, replacement))
        {
            _ignoreEventsUntilUtc = DateTime.UtcNow.AddMilliseconds(700);
            _lastCorrection = new(decision.Original, decision.Replacement, decision.Confidence);
            _lastCorrectionUtc = DateTime.UtcNow;
            lock (_gate) _recentWords.Clear();
            Corrected?.Invoke(this, _lastCorrection);
        }
    }

    private void RememberWord(string word)
    {
        var currentIsCyrillic = word.Any(c => c is >= 'А' and <= 'я' or 'Ё' or 'ё');
        if (_recentWords.Count > 0)
        {
            var previousIsCyrillic = _recentWords[^1].Any(c => c is >= 'А' and <= 'я' or 'Ё' or 'ё');
            if (currentIsCyrillic != previousIsCyrillic) _recentWords.Clear();
        }
        _recentWords.Add(word);
        if (_recentWords.Count > 23) _recentWords.RemoveAt(0);
    }

    private bool TryUndo()
    {
        if (_lastCorrection is null || DateTime.UtcNow - _lastCorrectionUtc > TimeSpan.FromSeconds(8)) return false;
        var ok = _injection.ReplacePreviousCharacters(_lastCorrection.Replacement.Length, _lastCorrection.Original);
        if (ok)
        {
            _ignoreEventsUntilUtc = DateTime.UtcNow.AddMilliseconds(700);
            _lastCorrection = null;
        }
        return ok;
    }

    private void RefreshHotkeys()
    {
        if (HotkeyGesture.TryParse(_settings.DictationHotkey, out var dictation, out _)) _dictationHotkey = dictation;
        if (HotkeyGesture.TryParse(_settings.UndoHotkey, out var undo, out _)) _undoHotkey = undo;
    }

    private static bool MatchesHotkey(string keyName, HotkeyModifiers modifiers, HotkeyGesture gesture) =>
        string.Equals(keyName, gesture.Key, StringComparison.OrdinalIgnoreCase) && modifiers == gesture.Modifiers;

    private static bool IsWayland() =>
        string.Equals(Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"), "wayland", StringComparison.OrdinalIgnoreCase);

    private static bool CommandExists(string name)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("sh", $"-lc \"command -v {name}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            process?.WaitForExit(1500);
            return process?.ExitCode == 0;
        }
        catch { return false; }
    }
}

internal enum LinuxRawKeyEventKind { Down, Up }

internal sealed record LinuxKeySnapshot(string KeyName, char? Character, HotkeyModifiers Modifiers);

internal sealed class LinuxX11KeyboardMapper : IDisposable
{
    private const uint XkbUseCoreKbd = 0x0100;
    private readonly nint _display;

    private static readonly Dictionary<string, char> Cyrillic = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Cyrillic_a"] = 'а', ["Cyrillic_be"] = 'б', ["Cyrillic_ve"] = 'в', ["Cyrillic_ghe"] = 'г',
        ["Cyrillic_de"] = 'д', ["Cyrillic_ie"] = 'е', ["Cyrillic_io"] = 'ё', ["Cyrillic_zhe"] = 'ж',
        ["Cyrillic_ze"] = 'з', ["Cyrillic_i"] = 'и', ["Cyrillic_shorti"] = 'й', ["Cyrillic_ka"] = 'к',
        ["Cyrillic_el"] = 'л', ["Cyrillic_em"] = 'м', ["Cyrillic_en"] = 'н', ["Cyrillic_o"] = 'о',
        ["Cyrillic_pe"] = 'п', ["Cyrillic_er"] = 'р', ["Cyrillic_es"] = 'с', ["Cyrillic_te"] = 'т',
        ["Cyrillic_u"] = 'у', ["Cyrillic_ef"] = 'ф', ["Cyrillic_ha"] = 'х', ["Cyrillic_tse"] = 'ц',
        ["Cyrillic_che"] = 'ч', ["Cyrillic_sha"] = 'ш', ["Cyrillic_shcha"] = 'щ', ["Cyrillic_hardsign"] = 'ъ',
        ["Cyrillic_yeru"] = 'ы', ["Cyrillic_softsign"] = 'ь', ["Cyrillic_e"] = 'э', ["Cyrillic_yu"] = 'ю',
        ["Cyrillic_ya"] = 'я'
    };

    public LinuxX11KeyboardMapper()
    {
        XInitThreads();
        _display = XOpenDisplay(nint.Zero);
        if (_display == nint.Zero) throw new InvalidOperationException("Не удалось подключиться к X11 display.");
    }

    public LinuxKeySnapshot? ReadKey(int keycode)
    {
        if (XkbGetState(_display, XkbUseCoreKbd, out var state) != 0) return null;
        var modifiers = ToHotkeyModifiers(state.Mods);
        var shifted = (state.Mods & 0x01) != 0;
        var keyName = GetKeyName(keycode);
        var character = GetCharacter(keycode, state.Group, shifted ? 1u : 0u);
        return new(keyName, character, modifiers);
    }

    private string GetKeyName(int keycode)
    {
        var keysym = XkbKeycodeToKeysym(_display, (byte)keycode, 0, 0);
        var raw = KeysymName(keysym);
        if (raw.Length == 1 && char.IsLetterOrDigit(raw[0])) return raw.ToUpperInvariant();
        return raw switch
        {
            "space" => "Space",
            "BackSpace" => "Backspace",
            "Return" => "Return",
            "Tab" => "Tab",
            "Escape" => "Escape",
            "Delete" => "Delete",
            _ when raw.StartsWith('F') && raw.Length <= 3 => raw,
            _ => raw
        };
    }

    private char? GetCharacter(int keycode, uint group, uint level)
    {
        var keysym = XkbKeycodeToKeysym(_display, (byte)keycode, group, level);
        if (keysym is >= 0x20 and <= 0x7e) return (char)keysym;
        var name = KeysymName(keysym);
        if (Cyrillic.TryGetValue(name, out var c)) return c;
        return name switch
        {
            "space" => ' ',
            "minus" => '-',
            "apostrophe" => '\'',
            "comma" => ',',
            "period" => '.',
            "semicolon" => ';',
            "slash" => '/',
            "bracketleft" => '[',
            "bracketright" => ']',
            _ => null
        };
    }

    private static string KeysymName(nint keysym)
    {
        var ptr = XKeysymToString(keysym);
        return ptr == nint.Zero ? "" : Marshal.PtrToStringAnsi(ptr) ?? "";
    }

    private static HotkeyModifiers ToHotkeyModifiers(byte mods)
    {
        var result = HotkeyModifiers.None;
        if ((mods & 0x01) != 0) result |= HotkeyModifiers.Shift;
        if ((mods & 0x04) != 0) result |= HotkeyModifiers.Ctrl;
        if ((mods & 0x08) != 0) result |= HotkeyModifiers.Alt;
        if ((mods & 0x40) != 0) result |= HotkeyModifiers.Meta;
        return result;
    }

    public void Dispose()
    {
        if (_display != nint.Zero) XCloseDisplay(_display);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XkbStateRec
    {
        public byte Group;
        public byte LockedGroup;
        public ushort BaseGroup;
        public ushort LatchedGroup;
        public byte Mods;
        public byte BaseMods;
        public byte LatchedMods;
        public byte LockedMods;
        public byte CompatState;
        public byte GrabMods;
        public byte CompatGrabMods;
        public byte LookupMods;
        public byte CompatLookupMods;
        public ushort PtrButtons;
    }

    [DllImport("libX11.so.6")] private static extern int XInitThreads();
    [DllImport("libX11.so.6")] private static extern nint XOpenDisplay(nint display);
    [DllImport("libX11.so.6")] private static extern int XCloseDisplay(nint display);
    [DllImport("libX11.so.6")] private static extern nint XkbKeycodeToKeysym(nint display, byte keycode, uint group, uint level);
    [DllImport("libX11.so.6")] private static extern int XkbGetState(nint display, uint deviceSpec, out XkbStateRec state);
    [DllImport("libX11.so.6")] private static extern nint XKeysymToString(nint keysym);
}

public sealed class LinuxAudioRecorder : IAudioRecorder
{
    private Process? _process;
    private string? _path;
    public bool IsRecording => _process is { HasExited: false };
    public event EventHandler<double>? LevelChanged;

    public IReadOnlyList<AudioDeviceInfo> GetDevices() => [new(0, "Default ALSA/PulseAudio microphone", true)];

    public void Start(int deviceNumber = 0)
    {
        if (IsRecording) return;
        if (!CommandExists("arecord"))
            throw new InvalidOperationException("Linux recorder requires arecord. Install alsa-utils and allow microphone access.");
        _path = Path.Combine(Path.GetTempPath(), $"fotur-dictation-{Guid.NewGuid():N}.wav");
        _process = Process.Start(new ProcessStartInfo("arecord", $"-q -f S16_LE -r 16000 -c 1 \"{_path}\"")
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("Не удалось запустить arecord.");
        LevelChanged?.Invoke(this, 0.35);
    }

    public async Task<string?> StopAsync()
    {
        if (_process is null) return null;
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3));
            }
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
        return File.Exists(_path) && new FileInfo(_path).Length > 44 ? _path : null;
    }

    public void Dispose()
    {
        if (_process is { HasExited: false }) _process.Kill(entireProcessTree: true);
        _process?.Dispose();
    }

    private static bool CommandExists(string name)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("sh", $"-lc \"command -v {name}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            process?.WaitForExit(1500);
            return process?.ExitCode == 0;
        }
        catch { return false; }
    }
}

public sealed class LinuxLocalDictationService : IDictationService
{
    private readonly string _root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Fotur", "TypingHelper", "models");

    public event EventHandler<double>? DownloadProgress;

    public LinuxLocalDictationService() => Directory.CreateDirectory(_root);
    public string GetRuntimeInfo() => WhisperFactory.GetRuntimeInfo() ?? "Whisper CPU runtime loaded";
    public bool IsModelInstalled(string model) => File.Exists(GetModelPath(model));

    public async Task<string> TranscribeAsync(string audioPath, AppSettings settings, CancellationToken cancellationToken = default)
    {
        WavAudioProcessor.ProcessInPlace(audioPath, settings);
        var modelPath = await EnsureModelAsync(settings.SpeechModel, cancellationToken);
        using var factory = WhisperFactory.FromPath(modelPath);
        var builder = factory.CreateBuilder();
        if (string.Equals(settings.SpeechLanguage, "auto", StringComparison.OrdinalIgnoreCase))
            builder.WithLanguageDetection();
        else
            builder.WithLanguage(settings.SpeechLanguage);
        if (settings.DictationTaskMode == DictationTaskMode.TranslateToEnglish)
            builder.WithTranslate();
        if (settings.DictionaryPromptEnabled && settings.CustomDictionary.Count > 0)
            builder.WithPrompt(string.Join(", ", settings.CustomDictionary.Take(80)));
        using var processor = builder.Build();
        await using var audio = File.OpenRead(audioPath);
        var result = new List<string>();
        await foreach (var segment in processor.ProcessAsync(audio, cancellationToken))
            result.Add(segment.Text.Trim());
        try { File.Delete(audioPath); } catch { }
        return DictationTextPostProcessor.Process(
            VoiceCommandProcessor.Process(string.Join(" ", result), settings.VoiceCommandsEnabled),
            settings);
    }

    private async Task<string> EnsureModelAsync(string model, CancellationToken cancellationToken)
    {
        var path = GetModelPath(model);
        if (File.Exists(path)) return path;
        var type = model.ToLowerInvariant() switch
        {
            "tiny" => GgmlType.Tiny,
            "small" => GgmlType.Small,
            "medium" => GgmlType.Medium,
            _ => GgmlType.Base
        };
        await using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(type, cancellationToken: cancellationToken);
        await using var target = File.Create(path);
        var buffer = new byte[1024 * 128];
        long copied = 0;
        int read;
        while ((read = await modelStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            copied += read;
            if (modelStream.CanSeek && modelStream.Length > 0)
                DownloadProgress?.Invoke(this, (double)copied / modelStream.Length);
        }
        return path;
    }

    private string GetModelPath(string model) => Path.Combine(_root, $"ggml-{model.ToLowerInvariant()}.bin");
}

public sealed class LinuxTextInjectionService : ITextInjectionService
{
    public bool ActivateWindow(nint window) => true;

    public bool SendText(string text)
    {
        if (!OperatingSystem.IsLinux()) return false;
        if (string.Equals(Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"), "wayland", StringComparison.OrdinalIgnoreCase))
            return false;
        try
        {
            using var process = Process.Start(new ProcessStartInfo("xdotool", "type --clearmodifiers --delay 0 --file -")
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            if (process is null) return false;
            process.StandardInput.Write(text);
            process.StandardInput.Close();
            process.WaitForExit(3000);
            return process.ExitCode == 0;
        }
        catch { return false; }
    }

    internal bool ReplacePreviousCharacters(int charactersToDelete, string replacement)
    {
        if (charactersToDelete < 0) return false;
        if (charactersToDelete > 0 && !SendBackspaces(charactersToDelete)) return false;
        return string.IsNullOrEmpty(replacement) || SendText(replacement);
    }

    private static bool SendBackspaces(int count)
    {
        if (!OperatingSystem.IsLinux() || count <= 0) return false;
        if (string.Equals(Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"), "wayland", StringComparison.OrdinalIgnoreCase))
            return false;
        try
        {
            using var process = Process.Start(new ProcessStartInfo("xdotool", $"key --clearmodifiers --repeat {count} --delay 0 BackSpace")
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            if (process is null) return false;
            process.WaitForExit(3000);
            return process.ExitCode == 0;
        }
        catch { return false; }
    }
}

public sealed class LinuxActiveWindowService : IActiveWindowService
{
    public nint GetActiveWindowHandle() => 0;
}

public sealed class LinuxAutostartService : IAutostartService
{
    public void SetEnabled(bool enabled)
    {
        // Linux autostart has desktop-environment-specific paths. 1.3.0 keeps this a no-op
        // instead of writing a broken .desktop file without knowing the install location.
    }
}
