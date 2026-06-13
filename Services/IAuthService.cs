using FocusFlowFinal.Models;
using System;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

/// <summary>
/// ИСПРАВЛЕНО (Часть 2, п.4, 8; Часть 3, п.11): сервис аутентификации.
///
/// Реальная реализация должна обращаться к серверному REST API
/// (см. инструкцию по серверу, эндпоинты /api/users/register, /api/users/login).
/// Сервер возвращает JWT-токен + UserProfile (включая isDeveloper/hasFreeAccess —
/// см. п.11 "режим разработчика").
///
/// Токен сохраняется локально через <see cref="AppSettings.AuthToken"/>.
/// ВАЖНО: для продакшена токен следует хранить через защищённое хранилище
/// (DPAPI на Windows / Keychain на macOS / libsecret на Linux), а не в
/// обычном JSON-файле — здесь это заглушка для прототипа.
/// </summary>
public interface IAuthService
{
    /// <summary>Текущий авторизованный пользователь (null, если не вошёл).</summary>
    UserProfile? CurrentUser { get; }

    /// <summary>true, если есть валидная локальная сессия (восстановлена при старте).</summary>
    bool IsAuthenticated { get; }

    /// <summary>Вход по email/паролю. Возвращает true при успехе и заполняет CurrentUser.</summary>
    Task<AuthResult> LoginAsync(string email, string password);

    /// <summary>Регистрация нового пользователя. После успеха — автоматический вход.</summary>
    Task<AuthResult> RegisterAsync(string email, string password, string username);

    /// <summary>
    /// Восстановление сессии по сохранённому токену (вызывается при старте приложения).
    /// Если токен истёк/недействителен — возвращает false и сбрасывает локальную сессию
    /// (см. "Часть 3, п.10: при ошибках аутентификации — сбрасывать сессию").
    /// </summary>
    Task<bool> RestoreSessionAsync();

    /// <summary>Выход из аккаунта — очищает токен и CurrentUser.</summary>
    Task LogoutAsync();

    /// <summary>
    /// Обновление профиля (username/email/avatar). Часть 2, п.5 — редактирование
    /// полей профиля во вкладке "Аккаунт".
    /// </summary>
    Task<AuthResult> UpdateProfileAsync(string? newUsername, string? newEmail, string? newAvatarPath);

    /// <summary>Смена пароля — требует текущий пароль для подтверждения.</summary>
    Task<AuthResult> ChangePasswordAsync(string currentPassword, string newPassword);
}

/// <summary>Унифицированный результат операций аутентификации.</summary>
public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public UserProfile? User { get; set; }

    public static AuthResult Ok(UserProfile user) => new() { Success = true, User = user };
    public static AuthResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
