using System.Text;
using System.Text.RegularExpressions;

namespace FoturTypingHelper.Core;

public static class WavAudioProcessor
{
    public static void ProcessInPlace(string path, AppSettings settings)
    {
        if ((!settings.VoiceActivityDetectionEnabled && !settings.NoiseSuppressionEnabled) || !File.Exists(path)) return;
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length <= 44 || Encoding.ASCII.GetString(bytes, 0, 4) != "RIFF") return;
        var threshold = (int)Math.Clamp(settings.VoiceActivityThreshold * short.MaxValue, 120, 6000);
        var samples = new short[(bytes.Length - 44) / 2];
        Buffer.BlockCopy(bytes, 44, samples, 0, samples.Length * 2);

        if (settings.NoiseSuppressionEnabled)
            for (var i = 0; i < samples.Length; i++)
                if (Math.Abs(samples[i]) < threshold * 0.55) samples[i] = 0;

        if (settings.VoiceActivityDetectionEnabled)
        {
            var padding = 16000 / 3;
            var first = Array.FindIndex(samples, value => Math.Abs(value) >= threshold);
            var last = Array.FindLastIndex(samples, value => Math.Abs(value) >= threshold);
            if (first < 0) { WriteWave(path, []); return; }
            first = Math.Max(0, first - padding);
            last = Math.Min(samples.Length - 1, last + padding);
            samples = samples[first..(last + 1)];
        }
        WriteWave(path, samples);
    }

    private static void WriteWave(string path, short[] samples)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.ASCII);
        var dataSize = samples.Length * 2;
        writer.Write("RIFF"u8); writer.Write(36 + dataSize); writer.Write("WAVE"u8);
        writer.Write("fmt "u8); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
        writer.Write(16000); writer.Write(32000); writer.Write((short)2); writer.Write((short)16);
        writer.Write("data"u8); writer.Write(dataSize);
        foreach (var sample in samples) writer.Write(sample);
    }
}

public static partial class DictationTextPostProcessor
{
    [GeneratedRegex(@"\b(?:ээ+|эм+|ну\s+вот|как\s+бы|um+|uh+|you\s+know)\b[,]?\s*", RegexOptions.IgnoreCase)]
    private static partial Regex Fillers();

    public static string Process(string text, AppSettings settings)
    {
        var result = settings.FillerWordsRemovalEnabled ? Fillers().Replace(text, "") : text;
        result = Regex.Replace(result, @"\s+([,.!?])", "$1");
        result = Regex.Replace(result, @"[ \t]{2,}", " ").Trim();
        return result.Length == 0 ? result : char.ToUpper(result[0]) + result[1..];
    }
}
