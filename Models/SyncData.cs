using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Models;

/// <summary>
/// ИСПРАВЛЕНО (Часть 2-3, п.3, 9): полезная нагрузка для одного "цикла"
/// синхронизации — пакет изменений, отправляемых на сервер, и пакет
/// изменений, получаемых обратно.
///
/// Формат соответствует эндпоинту POST /api/sync (см. инструкцию по серверу).
/// </summary>
public class SyncData
{
    /// <summary>Метка времени последней успешной синхронизации этого устройства (UTC).
    /// Сервер возвращает только записи, изменённые ПОСЛЕ этой метки.</summary>
    public DateTime SinceUtc { get; set; }

    /// <summary>Локальные изменения задач с момента последней синхронизации.</summary>
    public List<TaskItem> Tasks { get; set; } = new();

    /// <summary>Локальные изменения событий календаря.</summary>
    public List<CalendarEvent> Events { get; set; } = new();

    /// <summary>Локальные изменения проектов.</summary>
    public List<ProjectItem> Projects { get; set; } = new();
}

/// <summary>
/// ИСПРАВЛЕНО (Часть 2-3, п.3): ответ сервера на запрос синхронизации —
/// содержит изменения от других устройств пользователя, которые нужно
/// слить с локальной БД (конфликты — по LastModified, новее побеждает).
/// </summary>
public class SyncResponse
{
    /// <summary>Метка времени сервера на момент обработки запроса (UTC).
    /// Клиент сохраняет её как новое значение SinceUtc для следующего цикла.</summary>
    public DateTime ServerTimeUtc { get; set; }

    public List<TaskItem> Tasks { get; set; } = new();
    public List<CalendarEvent> Events { get; set; } = new();
    public List<ProjectItem> Projects { get; set; } = new();
}

/// <summary>
/// ИСПРАВЛЕНО (Часть 2-3, п.3): вспомогательная структура для разрешения
/// конфликтов — сравнивает LastModified двух версий одной записи (по SyncId)
/// и определяет победителя ("более новая дата побеждает").
/// </summary>
public static class SyncConflictResolver
{
    /// <summary>
    /// Возвращает true, если <paramref name="remote"/> новее <paramref name="local"/>
    /// и должна заменить локальную версию.
    /// </summary>
    public static bool RemoteWins<T>(T local, T remote) where T : ISyncableEntity
        => remote.LastModified > local.LastModified;
}
