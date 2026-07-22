using FoturTypingHelper.Core;
using FoturTypingHelper.Windows;

if (!OperatingSystem.IsWindows()) return 0;

try
{
    using var recorder = new AudioRecorder();
    var dictation = new LocalDictationService();
    Console.WriteLine(dictation.GetRuntimeInfo());
    recorder.Start();
    Console.WriteLine("Recording microphone for 1.5 seconds...");
    await Task.Delay(1500);
    var audio = await recorder.StopAsync();
    if (audio is null || !File.Exists(audio) || new FileInfo(audio).Length <= 44)
        throw new InvalidOperationException("Microphone produced no WAV audio.");

    var text = await dictation.TranscribeAsync(audio, new AppSettings
    {
        SpeechModel = "base",
        SpeechLanguage = "auto",
        DictationTaskMode = DictationTaskMode.Transcribe,
        VoiceCommandsEnabled = true
    });
    Console.WriteLine($"PASS: microphone, model and Whisper pipeline completed. Result: {text}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}
