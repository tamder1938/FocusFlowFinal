using FocusFlowFinal.Models;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

/// <summary>
/// ИСПРАВЛЕНО (Часть 3, п.9): абстракция доступа к серверной БД (PostgreSQL
/// через ASP.NET Core Web API). Реальная реализация — <see cref="CloudDatabaseService"/>,
/// использующая HttpClient. См. инструкцию по серверу для описания эндпоинтов.
/// </summary>
public interface ICloudDatabaseService
{
    /// <summary>
    /// Один цикл синхронизации: отправляет локальные изменения (<paramref name="localChanges"/>)
    /// на сервер (POST /api/sync) и получает изменения от других устройств пользователя.
    /// Конфликты разрешаются на сервере по LastModified (новее побеждает) —
    /// см. <see cref="SyncConflictResolver"/>.
    /// </summary>
    Task<SyncResponse?> PushAndPullChangesAsync(SyncData localChanges, string authToken);

    /// <summary>Проверка доступности серверного API (для индикатора "онлайн/офлайн").</summary>
    Task<bool> PingAsync();
}
