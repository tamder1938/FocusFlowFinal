using System;
using System.IO;
using System.Text.Json;
using LiteDB;

namespace FocusFlowFinal.Models;

public class AppSettings
{
    public int ThemeMode { get; set; } = 0;
    public string Language { get; set; } = "Русский";

    public bool SystemNotifications { get; set; } = true;
    public bool SoundNotifications { get; set; } = false;

    public string HotkeyDay { get; set; } = "Ctrl+D";
    public string HotkeyWeek { get; set; } = "Ctrl+W";
    public string HotkeyNewTask { get; set; } = "Ctrl+N";

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FocusFlow", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                // ЯВНО указываем System.Text.Json.JsonSerializer
                return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            string directory = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(directory);
            string json = System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}