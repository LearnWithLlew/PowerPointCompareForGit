using System.Text.Json;

namespace PowerPointCompareForGit;

// ~/.createpowerpointcheckpoint/settings.json
internal sealed record Settings(string? DiffTool = null, string? LibreOfficePath = null)
{
    // Saved as camelCase ("diffTool", "libreOfficePath"); read case-insensitively so hand edits in either case work.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    internal static string DefaultPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".createpowerpointcheckpoint", "settings.json");

    internal static Settings Load(string path)
        => File.Exists(path) ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(path), JsonOptions) ?? new Settings() : new Settings();

    internal void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }
}
