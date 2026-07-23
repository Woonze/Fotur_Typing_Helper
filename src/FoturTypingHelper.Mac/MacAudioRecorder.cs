using System.Runtime.InteropServices;
using FoturTypingHelper.Core;

namespace FoturTypingHelper.Mac;

public sealed class MacAudioRecorder : IAudioRecorder
{
    private nint _device;
    private readonly MemoryStream _pcm = new();
    private Timer? _timer;
    private string? _path;
    public bool IsRecording => _device != 0;
    public event EventHandler<double>? LevelChanged;
    public IReadOnlyList<AudioDeviceInfo> GetDevices() => [new(0, "Системный микрофон macOS", true)];

    public void Start(int deviceNumber = 0)
    {
        if (IsRecording) return;
        _pcm.SetLength(0); _path = Path.Combine(Path.GetTempPath(), $"fotur-dictation-{Guid.NewGuid():N}.wav");
        _device = MacNative.alcCaptureOpenDevice(null, 16000, 0x1101, 160000);
        if (_device == 0) throw new InvalidOperationException("macOS не предоставила доступ к микрофону.");
        MacNative.alcCaptureStart(_device); _timer = new Timer(_ => Capture(), null, 50, 50);
    }

    private void Capture()
    {
        if (_device == 0) return;
        MacNative.alcGetIntegerv(_device, 0x312, 1, out var count);
        if (count <= 0) return;
        var bytes = new byte[count * 2]; var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try { MacNative.alcCaptureSamples(_device, handle.AddrOfPinnedObject(), count); }
        finally { handle.Free(); }
        lock (_pcm) _pcm.Write(bytes);
        var peak = 0d; for (var i = 0; i + 1 < bytes.Length; i += 2) peak = Math.Max(peak, Math.Abs(BitConverter.ToInt16(bytes, i)) / 32768d);
        LevelChanged?.Invoke(this, peak);
    }

    public Task<string?> StopAsync()
    {
        if (_device == 0) return Task.FromResult<string?>(null);
        _timer?.Dispose(); _timer = null; Capture(); MacNative.alcCaptureStop(_device); MacNative.alcCaptureCloseDevice(_device); _device = 0;
        WriteWave(_path!, _pcm.ToArray()); return Task.FromResult<string?>(_path);
    }

    private static void WriteWave(string path, byte[] data)
    {
        using var w = new BinaryWriter(File.Create(path)); w.Write("RIFF"u8); w.Write(36 + data.Length); w.Write("WAVE"u8);
        w.Write("fmt "u8); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(16000); w.Write(32000); w.Write((short)2); w.Write((short)16);
        w.Write("data"u8); w.Write(data.Length); w.Write(data);
    }
    public void Dispose() { _timer?.Dispose(); if (_device != 0) { MacNative.alcCaptureStop(_device); MacNative.alcCaptureCloseDevice(_device); } }
}
