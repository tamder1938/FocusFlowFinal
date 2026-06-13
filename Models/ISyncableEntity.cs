using System;

namespace FocusFlowFinal.Models;

/// <summary>
/// ИСПРАВЛЕНО (Часть 2-3, п.3): общий контракт полей синхронизации.
/// Каждая синхронизируемая сущность (TaskItem, CalendarEvent, ProjectItem)
/// реализует этот интерфейс, что позволяет <see cref="FocusFlowFinal.Services.ISyncService"/>
/// работать с ними единообразно через рефлексию/обобщённые методы.
/// </summary>
public interface ISyncableEntity
{
    /// <summary>Глобальный уникальный идентификатор записи (для сопоставления
    /// между устройствами — локальный int Id уникален только в рамках одной БД).</summary>
    Guid SyncId { get; set; }

    /// <summary>Время последнего изменения записи (UTC). Используется для
    /// разрешения конфликтов синхронизации: более позднее изменение побеждает.</summary>
    DateTime LastModified { get; set; }

    /// <summary>Мягкое удаление: true — запись считается удалённой и должна
    /// быть скрыта из UI, но остаётся в БД до подтверждения синхронизации
    /// со всеми устройствами.</summary>
    bool IsDeleted { get; set; }
}
