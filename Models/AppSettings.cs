using System;
using System.IO;
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

    // ===================== Часть 2-3: аккаунт / синхронизация =====================

    /// <summary>true — пользователь уже видел стартовый экран входа (LoginWindow)
    /// и сделал выбор (войти или работать локально). Используется, чтобы
    /// не показывать LoginWindow повторно при каждом запуске.</summary>
    public bool HasCompletedFirstRun { get; set; } = false;

    /// <summary>true — пользователь выбрал "Продолжить без синхронизации".
    /// Все функции работают локально, вкладка "Аккаунт" заблокирована.</summary>
    public bool IsLocalOnlyMode { get; set; } = true;

    /// <summary>JWT-токен сессии (заглушка). В реальной реализации должен
    /// храниться через защищённое хранилище (DPAPI / Keychain / libsecret),
    /// а не в обычном JSON-файле настроек.</summary>
    public string? AuthToken { get; set; }

    /// <summary>Email последнего авторизованного пользователя (для отображения в UI).</summary>
    public string? AccountEmail { get; set; }

    /// <summary>Логин (username) последнего авторизованного пользователя.</summary>
    public string? AccountUsername { get; set; }

    /// <summary>Включена ли облачная синхронизация (требует активной подписки или dev-доступа).</summary>
    public bool SyncEnabled { get; set; } = false;

    /// <summary>Дата/время последней успешной синхронизации (UTC).</summary>
    public DateTime? LastSyncUtc { get; set; }

    /// <summary>URL базового адреса серверного API. Настраивается через appsettings
    /// или хранится здесь после первого запуска (см. инструкцию по серверу).</summary>
    public string CloudApiBaseUrl { get; set; } = "https://api.focusflow.example.com";

    /// <summary>Автоматически отмечать задачу выполненной при завершении всех циклов таймера.</summary>
    public bool MarkTaskCompletedOnTimerFinish { get; set; } = false;

    /// <summary>Дата окончания оплаченной подписки (UTC). Null — подписка не куплена.</summary>
    public DateTime? SubscriptionExpiryDate { get; set; }

    /// <summary>Включён ли финансовый модуль.</summary>
    public bool FinanceModuleEnabled { get; set; } = false;

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
