using NAudio.Wave;
using FoturTypingHelper.Core;

namespace FoturTypingHelper.Windows;

public sealed class AudioRecorder : IAudioRecorder
{
    private WaveInEvent? _input;
    private WaveFileWriter? _writer;
    private string? _path;

    public bool IsRecording => _input is not null;
    public event EventHandler<double>? LevelChanged;

    public IReadOnlyList<AudioDeviceInfo> GetDevices() => Enumerable.Range(0, WaveInEvent.DeviceCount)
        .Select(i => new AudioDeviceInfo(i, WaveInEvent.GetCapabilities(i).ProductName, i == 0)).ToArray();

    public void Start(int deviceNumber = 0)
    {
        if (IsRecording) return;
        _path = Path.Combine(Path.GetTempPath(), $"fotur-dictation-{Guid.NewGuid():N}.wav");
        _input = new WaveInEvent { DeviceNumber = deviceNumber, WaveFormat = new WaveFormat(16000, 1), BufferMilliseconds = 80 };
        _writer = new WaveFileWriter(_path, _input.WaveFormat);
        _input.DataAvailable += (_, e) =>
        {
            _writer?.Write(e.Buffer, 0, e.BytesRecorded);
            var peak = 0d;
            for (var i = 0; i + 1 < e.BytesRecorded; i += 2)
                peak = Math.Max(peak, Math.Abs(BitConverter.ToInt16(e.Buffer, i)) / 32768d);
            LevelChanged?.Invoke(this, peak);
        };
        _input.StartRecording();
    }

    public async Task<string?> StopAsync()
    {
        if (_input is null) return null;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _input.RecordingStopped += (_, _) => completion.TrySetResult();
        _input.StopRecording();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(3));
        _writer?.Dispose();
        _writer = null;
        _input.Dispose();
        _input = null;
        return _path;
    }

    public void Dispose()
    {
        _input?.Dispose();
        _writer?.Dispose();
    }
}
