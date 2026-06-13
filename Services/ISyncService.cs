using System;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

/// <summary>
/// ИСПРАВЛЕНО (Часть 2, п.3): высокоуровневый сервис синхронизации.
/// Объединяет <see cref="IAuthService"/> (токен/доступ) и
/// <see cref="ICloudDatabaseService"/> (передача данных), а также
/// читает/пишет локальную LiteDB через <see cref="IDatabaseService"/>.
///
/// Логика запуска:
///   - при старте приложения (если SyncEnabled == true);
///   - каждые 5 минут по таймеру (см. MainViewModel);
///   - по кнопке "Синхронизировать сейчас" в настройках.
/// </summary>
public interface ISyncService
{
    /// <summary>true, если синхронизация доступна (есть подписка/dev-доступ И включена в настройках).</summary>
    bool IsSyncAvailable { get; }

    /// <summary>
    /// Выполняет один цикл синхронизации: собирает локальные изменения с момента
    /// LastSyncUtc, отправляет на сервер, применяет полученные изменения
    /// к локальной БД (конфликты — по LastModified, новее побеждает),
    /// обновляет AppSettings.LastSyncUtc.
    /// Возвращает true при успехе, false — если сервер недоступен или нет доступа.
    /// </summary>
    Task<bool> SyncDataAsync();

    /// <summary>Дата/время последней успешной синхронизации (UTC), либо null.</summary>
    DateTime? LastSyncUtc { get; }
}
