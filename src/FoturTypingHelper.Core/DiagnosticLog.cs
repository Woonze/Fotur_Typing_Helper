namespace FoturTypingHelper.Core;

public static class DiagnosticLog
{
    private static readonly object Gate = new();
    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Fotur", "TypingHelper", "logs", "fotur-typing-helper.log");

    public static void Write(string area, Exception exception)
        => WriteLine(area, exception.ToString());

    public static void WriteMessage(string area, string message)
        => WriteLine(area, message);

    private static void WriteLine(string area, string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.AppendAllText(FilePath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {area}: {message}\n");
            }
        }
        catch { /* diagnostics must never break typing */ }
    }
}
