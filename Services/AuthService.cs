using FocusFlowFinal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

/// <summary>
/// ИСПРАВЛЕНО (Часть 2, п.4, 8; Часть 3, п.9-11): заглушка <see cref="IAuthService"/>.
///
/// РЕЖИМ ПРОТОТИПА: пользователи "регистрируются" в локальном in-memory списке
/// (плюс сохраняются в LiteDB-файле users_local.db, чтобы переживать перезапуск
/// приложения на одном устройстве). Пароли хешируются (см. <see cref="HashPassword"/>) —
/// НЕ используйте этот хеш в продакшене, замените на bcrypt на сервере.
///
/// ДЛЯ ИНТЕГРАЦИИ РЕАЛЬНОГО БЭКЕНДА: замените тела методов на вызовы
/// _httpClient.PostAsJsonAsync($"{baseUrl}/api/users/login", ...) и т.д.
/// согласно инструкции по серверу (POST /api/users/register, /api/users/login).
/// Эндпоинты должны возвращать JSON вида:
///   { "token": "...", "userId": "...", "username": "...", "email": "...",
///     "isDeveloper": false, "hasFreeAccess": false, "subscription": {...} }
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly LocalUserStore _localStore;

    public UserProfile? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

    public AuthService()
    {
        var settings = AppSettings.Load();
        _httpClient = new HttpClient { BaseAddress = new Uri(settings.CloudApiBaseUrl) };
        _localStore = new LocalUserStore();
    }

    public Task<AuthResult> RegisterAsync(string email, string password, string username)
    {
        // TODO (реальный бэкенд): заменить на
        // var resp = await _httpClient.PostAsJsonAsync("/api/users/register",
        //     new { email, password, username });
        // обработать resp.StatusCode, десериализовать AuthResult из JSON.

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(username))
            return Task.FromResult(AuthResult.Fail(LocalizationService.Instance["AuthFillAllFields"]));

        if (_localStore.FindByEmail(email) != null)
            return Task.FromResult(AuthResult.Fail(LocalizationService.Instance["AuthEmailTaken"]));

        var record = new LocalUserRecord
        {
            UserId       = Guid.NewGuid().ToString(),
            Email        = email,
            Username     = username,
            PasswordHash = HashPassword(password),
            // ИСПРАВЛЕНО (Часть 3, п.11): по умолчанию обычный пользователь —
            // is_developer/can_use_free выставляются только вручную на сервере/в /admin.
            IsDeveloper  = false,
            HasFreeAccess = false
        };
        _localStore.Add(record);

        var profile = ToProfile(record);
        CurrentUser = profile;
        PersistSession(profile);

        return Task.FromResult(AuthResult.Ok(profile));
    }

    public Task<AuthResult> LoginAsync(string email, string password)
    {
        // TODO (реальный бэкенд): заменить на POST /api/users/login { email, password }
        // и сохранить полученный JWT в AppSettings.AuthToken.

        var record = _localStore.FindByEmail(email);
        if (record == null || record.PasswordHash != HashPassword(password))
            return Task.FromResult(AuthResult.Fail(LocalizationService.Instance["AuthInvalidCredentials"]));

        var profile = ToProfile(record);
        CurrentUser = profile;
        PersistSession(profile);

        return Task.FromResult(AuthResult.Ok(profile));
    }

    public Task<bool> RestoreSessionAsync()
    {
        // TODO (реальный бэкенд): отправить AuthToken на /api/users/me для проверки
        // валидности и получения актуального профиля (isDeveloper/hasFreeAccess/subscription
        // могли измениться на сервере). При 401 — вызвать LogoutAsync() и вернуть false
        // (см. Часть 3, п.10: "при ошибках аутентификации — сбрасывать сессию").

        var settings = AppSettings.Load();
        if (string.IsNullOrEmpty(settings.AuthToken) || string.IsNullOrEmpty(settings.AccountEmail))
            return Task.FromResult(false);

        var record = _localStore.FindByEmail(settings.AccountEmail);
        if (record == null)
        {
            // Токен есть, но пользователь не найден локально — сбрасываем сессию
            settings.AuthToken = null;
            settings.AccountEmail = null;
            settings.AccountUsername = null;
            settings.Save();
            return Task.FromResult(false);
        }

        CurrentUser = ToProfile(record);
        return Task.FromResult(true);
    }

    public Task LogoutAsync()
    {
        CurrentUser = null;

        var settings = AppSettings.Load();
        settings.AuthToken = null;
        settings.AccountEmail = null;
        settings.AccountUsername = null;
        settings.SyncEnabled = false;
        settings.Save();

        return Task.CompletedTask;
    }

    public Task<AuthResult> UpdateProfileAsync(string? newUsername, string? newEmail, string? newAvatarPath)
    {
        // TODO (реальный бэкенд): PUT /api/users/me { username, email, avatarUrl }

        if (CurrentUser == null)
            return Task.FromResult(AuthResult.Fail(LocalizationService.Instance["AuthNotLoggedIn"]));

        var record = _localStore.FindByEmail(CurrentUser.Email);
        if (record == null)
            return Task.FromResult(AuthResult.Fail(LocalizationService.Instance["AuthNotLoggedIn"]));

        if (!string.IsNullOrWhiteSpace(newUsername)) record.Username = newUsername;
        if (!string.IsNullOrWhiteSpace(newEmail))    record.Email    = newEmail;
        if (newAvatarPath != null)                   record.AvatarPath = newAvatarPath;

        _localStore.Update(record);

        CurrentUser = ToProfile(record);
        PersistSession(CurrentUser);

        return Task.FromResult(AuthResult.Ok(CurrentUser));
    }

    public Task<AuthResult> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        // TODO (реальный бэкенд): POST /api/users/change-password { currentPassword, newPassword }
        // Пароли на сервере хранятся через bcrypt — см. инструкцию по безопасности.

        if (CurrentUser == null)
            return Task.FromResult(AuthResult.Fail(LocalizationService.Instance["AuthNotLoggedIn"]));

        var record = _localStore.FindByEmail(CurrentUser.Email);
        if (record == null || record.PasswordHash != HashPassword(currentPassword))
            return Task.FromResult(AuthResult.Fail(LocalizationService.Instance["AuthWrongPassword"]));

        record.PasswordHash = HashPassword(newPassword);
        _localStore.Update(record);

        return Task.FromResult(AuthResult.Ok(CurrentUser));
    }

    private void PersistSession(UserProfile profile)
    {
        var settings = AppSettings.Load();
        // TODO (реальный бэкенд): здесь должен сохраняться настоящий JWT,
        // полученный от сервера, а не email в качестве "токена".
        settings.AuthToken       = $"local-session:{profile.UserId}";
        settings.AccountEmail    = profile.Email;
        settings.AccountUsername = profile.Username;
        settings.Save();
    }

    private static UserProfile ToProfile(LocalUserRecord r) => new()
    {
        UserId        = r.UserId,
        Username      = r.Username,
        Email         = r.Email,
        AvatarPath    = r.AvatarPath,
        IsDeveloper   = r.IsDeveloper,
        HasFreeAccess = r.HasFreeAccess,
        Subscription  = r.Subscription
    };

    /// <summary>
    /// ВНИМАНИЕ: это НЕ криптографически безопасный хеш для продакшена.
    /// Используется только для локальной заглушки. На сервере пароли
    /// должны хешироваться через bcrypt (см. Часть 3, п.10).
    /// </summary>
    private static string HashPassword(string password)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}

/// <summary>Локальная запись пользователя (заглушка вместо серверной таблицы users).</summary>
internal class LocalUserRecord
{
    [LiteDB.BsonId]
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }

    // ИСПРАВЛЕНО (Часть 3, п.11): на сервере — столбцы users.is_developer, users.can_use_free
    public bool IsDeveloper { get; set; }
    public bool HasFreeAccess { get; set; }
    public SubscriptionInfo? Subscription { get; set; }
}

/// <summary>
/// Локальное хранилище "пользователей" на LiteDB — заглушка серверной таблицы users.
/// В реальной реализации этот класс не нужен — все запросы идут на сервер.
/// </summary>
internal class LocalUserStore
{
    private readonly string _dbPath;

    public LocalUserStore()
    {
        var folder = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FocusFlow");
        System.IO.Directory.CreateDirectory(folder);
        _dbPath = System.IO.Path.Combine(folder, "users_local.db");
    }

    private LiteDB.LiteDatabase Open() => new(_dbPath);

    public LocalUserRecord? FindByEmail(string email)
    {
        using var db = Open();
        return db.GetCollection<LocalUserRecord>("users")
                 .FindOne(u => u.Email == email);
    }

    public void Add(LocalUserRecord record)
    {
        using var db = Open();
        db.GetCollection<LocalUserRecord>("users").Insert(record);
    }

    public void Update(LocalUserRecord record)
    {
        using var db = Open();
        db.GetCollection<LocalUserRecord>("users").Update(record);
    }
}
