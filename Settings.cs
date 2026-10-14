using System;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace InkOverlay;

public sealed class Settings
{
    public string Color { get; set; } = AppState.Palette[0].ToString();
    public double Size { get; set; } = 4;
    public int FadeSeconds { get; set; }
    public bool ToolbarCollapsed { get; set; }
    /// <summary>The installed version that last ran, to tell the user when an update has been applied.</summary>
    public string? LastVersion { get; set; }

    static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "InkOverlay", "settings.json");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
        }
        catch (Exception)
        {
            // Corrupt or unreadable settings fall back to defaults.
        }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception)
        {
            // Losing preferences is not worth crashing on exit.
        }
    }

    public Color GetColor()
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(Color);
        }
        catch (Exception)
        {
            return AppState.Palette[0];
        }
    }
}
