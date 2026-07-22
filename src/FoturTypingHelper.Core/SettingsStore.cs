using System.Text.Json;
using System.Text.Json.Serialization;

namespace FoturTypingHelper.Core;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string FilePath { get; }
    public PersistedState State { get; private set; }

    public SettingsStore(string? root = null)
    {
        root ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Fotur", "TypingHelper");
        Directory.CreateDirectory(root);
        FilePath = Path.Combine(root, "settings.json");
        State = Load();
    }

    private PersistedState Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new();
            return JsonSerializer.Deserialize<PersistedState>(File.ReadAllText(FilePath), JsonOptions) ?? new();
        }
        catch
        {
            var damagedPath = FilePath + ".damaged-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            try { File.Move(FilePath, damagedPath); } catch { /* best-effort recovery */ }
            return new();
        }
    }

    public void Save()
    {
        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(State, JsonOptions));
        File.Move(temporary, FilePath, true);
    }
}
