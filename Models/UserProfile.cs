using System;

namespace FocusFlowFinal.Models;

/// <summary>
/// ИСПРАВЛЕНО (Часть 2, п.4-5): профиль пользователя, полученный от
/// сервера авторизации после успешного входа/регистрации.
/// Хранится в памяти на время сессии; токен — отдельно в защищённом хранилище.
/// </summary>
public class UserProfile
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Путь к локальному файлу аватара или base64-строка (см. п. "Общие требования").</summary>
    public string? AvatarPath { get; set; }

    /// <summary>
    /// ИСПРАВЛЕНО (Часть 3, п.11): флаг "режим разработчика" — приходит от сервера.
    /// Если true — синхронизация доступна без подписки.
    /// </summary>
    public bool IsDeveloper { get; set; }

    /// <summary>
    /// ИСПРАВЛЕНО (Часть 3, п.11): флаг "бесплатный доступ", выставляемый
    /// разработчиком вручную для бета-тестеров/коллег через /admin.
    /// </summary>
    public bool HasFreeAccess { get; set; }

    /// <summary>Информация о текущей подписке (может быть null, если не покупалась).</summary>
    public SubscriptionInfo? Subscription { get; set; }

    /// <summary>
    /// ИСПРАВЛЕНО (Часть 3, п.11): итоговое разрешение на использование
    /// синхронизации — true, если активна подписка ИЛИ IsDeveloper ИЛИ HasFreeAccess.
    /// </summary>
    public bool HasSyncAccess =>
        IsDeveloper || HasFreeAccess || (Subscription?.IsActive ?? false);
}
