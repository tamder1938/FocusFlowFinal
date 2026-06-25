using FocusFlowFinal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

/// <summary>
/// ИСПРАВЛЕНО (Часть 2, п.3): реализация <see cref="ISyncService"/> для прототипа.
///
/// Алгоритм SyncDataAsync:
///   1. Проверяем доступ (подписка / dev-режим) и включена ли синхронизация.
///   2. Собираем локальные записи, изменённые после AppSettings.LastSyncUtc.
///   3. Отправляем их на сервер через ICloudDatabaseService.PushAndPullChangesAsync,
///      получаем изменения от других устройств.
///   4. Применяем полученные изменения локально: для каждой записи ищем
///      локальный аналог по SyncId; если remote.LastModified новее —
///      перезаписываем локальные поля (SyncConflictResolver.RemoteWins);
///      если локальной записи нет — создаём новую.
///   5. Обновляем AppSettings.LastSyncUtc = response.ServerTimeUtc.
///
/// ПРИМЕЧАНИЕ О ПРОИЗВОДИТЕЛЬНОСТИ: для прототипа "изменено после X" вычисляется
/// в памяти через LINQ по всем записям (LiteDB). Для больших БД на сервере
/// это должен быть индексированный SQL-запрос
/// (`WHERE last_modified > @since AND user_id = @userId`) — см. инструкцию по серверу.
/// </summary>
public class CloudSyncService : ISyncService
{
    private readonly IDatabaseService _db;
    private readonly IAuthService _auth;
    private readonly ICloudDatabaseService _cloudDb;

    public DateTime? LastSyncUtc  { get; private set; }
    public string?   LastSyncError { get; private set; }

    public CloudSyncService(IDatabaseService db, IAuthService auth, ICloudDatabaseService cloudDb)
    {
        _db = db;
        _auth = auth;
        _cloudDb = cloudDb;

        LastSyncUtc = AppSettings.Load().LastSyncUtc;
    }

    // ИСПРАВЛЕНО (Часть 3, п.11): доступ есть, если пользователь авторизован,
    // сервер подтвердил HasSyncAccess (подписка / dev / free) И синхронизация включена в настройках.
    public bool IsSyncAvailable
    {
        get
        {
            var settings = AppSettings.Load();
            return settings.SyncEnabled
                   && _auth.IsAuthenticated
                   && (_auth.CurrentUser?.HasSyncAccess ?? false);
        }
    }

    public async Task<bool> SyncDataAsync()
    {
        if (!IsSyncAvailable) return false;

        var settings = AppSettings.Load();
        if (string.IsNullOrEmpty(settings.AuthToken)) return false;

        var since = settings.LastSyncUtc ?? DateTime.MinValue;

        // ── Шаг 1: собираем локальные изменения ────────────────────────
        var localChanges = new SyncData
        {
            SinceUtc = since,
            Tasks    = _db.GetAllTasks()
                          .Where(t => t.LastModified > since)
                          .ToList(),
            Events   = _db.GetEventsForPeriod(DateTime.MinValue.AddYears(1), DateTime.MaxValue.AddYears(-1))
                          .Where(e => e.LastModified > since)
                          .ToList(),
            Projects = _db.GetAllProjects()
                          .Where(p => p.LastModified > since)
                          .ToList()
        };

        // ── Шаг 2: отправляем на сервер, получаем чужие изменения ───────
        var response = await _cloudDb.PushAndPullChangesAsync(localChanges, settings.AuthToken);

        if (response == null)
        {
            // Сервер недоступен (или 401 — токен недействителен).
            // ИСПРАВЛЕНО (Часть 3, п.10): если причина — недействительный токен,
            // в реальной реализации здесь нужно проверить код ответа и вызвать
            // _auth.LogoutAsync(), чтобы пользователь увидел экран входа снова.
            return false;
        }

        // ── Шаг 3: применяем входящие изменения локально ────────────────
        MergeTasks(response.Tasks);
        MergeEvents(response.Events);
        MergeProjects(response.Projects);

        // ── Шаг 4: фиксируем время последней синхронизации ──────────────
        LastSyncUtc = response.ServerTimeUtc;
        settings.LastSyncUtc = response.ServerTimeUtc;
        settings.Save();

        return true;
    }

    private void MergeTasks(List<TaskItem> remoteItems)
    {
        if (remoteItems.Count == 0) return;

        var localBySync = _db.GetAllTasks().ToDictionary(t => t.SyncId);

        foreach (var remote in remoteItems)
        {
            if (localBySync.TryGetValue(remote.SyncId, out var local))
            {
                if (!SyncConflictResolver.RemoteWins(local, remote)) continue;

                // Более новая версия с сервера — обновляем локальную запись,
                // сохраняя локальный int Id (первичный ключ LiteDB).
                remote.Id = local.Id;
                _db.UpsertTask(remote);
            }
            else
            {
                // Новая запись с другого устройства — вставляем (Id=0 → автоинкремент).
                remote.Id = 0;
                _db.UpsertTask(remote);
            }
        }
    }

    private void MergeEvents(List<CalendarEvent> remoteItems)
    {
        if (remoteItems.Count == 0) return;

        // ИСПРАВЛЕНО: GetEventsForPeriod может возвращать несколько "виртуальных"
        // экземпляров повторяющегося события с одинаковым SyncId — берём по одному.
        var localBySync = _db.GetEventsForPeriod(DateTime.MinValue.AddYears(1), DateTime.MaxValue.AddYears(-1))
                              .GroupBy(e => e.SyncId)
                              .ToDictionary(g => g.Key, g => g.First());

        foreach (var remote in remoteItems)
        {
            if (localBySync.TryGetValue(remote.SyncId, out var local))
            {
                if (!SyncConflictResolver.RemoteWins(local, remote)) continue;

                remote.Id = local.Id;
                _db.UpsertEvent(remote);
            }
            else
            {
                remote.Id = 0;
                _db.UpsertEvent(remote);
            }
        }
    }

    private void MergeProjects(List<ProjectItem> remoteItems)
    {
        if (remoteItems.Count == 0) return;

        var localBySync = _db.GetAllProjects().ToDictionary(p => p.SyncId);

        foreach (var remote in remoteItems)
        {
            if (localBySync.TryGetValue(remote.SyncId, out var local))
            {
                if (!SyncConflictResolver.RemoteWins(local, remote)) continue;

                remote.Id = local.Id;
                _db.UpsertProject(remote);
            }
            else
            {
                remote.Id = 0;
                _db.UpsertProject(remote);
            }
        }
    }
}
