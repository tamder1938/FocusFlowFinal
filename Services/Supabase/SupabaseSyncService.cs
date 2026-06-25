using FocusFlowFinal.Models;
using FocusFlowFinal.Services.Supabase.Rows;
using static Supabase.Postgrest.Constants;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services.Supabase;

public class SupabaseSyncService : ISyncService
{
    private readonly IDatabaseService _db;
    private readonly IAuthService _auth;
    private readonly SupabaseClientProvider _provider;

    // Нижняя граница диапазона синхронизации при первом запуске (избегаем DateTime.MinValue)
    private static readonly DateTime SyncEpoch = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public DateTime? LastSyncUtc { get; private set; }
    public string?   LastSyncError { get; private set; }

    public SupabaseSyncService(IDatabaseService db, IAuthService auth, SupabaseClientProvider provider)
    {
        _db = db;
        _auth = auth;
        _provider = provider;
        LastSyncUtc = AppSettings.Load().LastSyncUtc;
    }

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
        LastSyncError = null;

        if (!IsSyncAvailable) return false;

        var client = await _provider.GetClientAsync();
        if (client == null)
        {
            LastSyncError = "Supabase client не инициализирован. " +
                            "Проверьте URL и AnonKey в appsettings.json, а также сетевое соединение.";
            Debug.WriteLine($"[Sync] {LastSyncError}");
            return false;
        }

        var userId   = _auth.CurrentUser!.UserId;
        var since    = AppSettings.Load().LastSyncUtc ?? SyncEpoch;
        // ISO-8601 UTC без дробных секунд — принимается всеми версиями PostgREST
        var sinceStr = since.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

        Debug.WriteLine($"[Sync] userId={userId}, since={sinceStr}");

        try
        {
            // ── Tasks ──────────────────────────────────────────────────────
            await PushTasksAsync(client, userId, since);
            var remoteTasks = await client.From<TaskRow>()
                .Filter("user_id", Operator.Equals, userId)
                .Filter("updated_at", Operator.GreaterThan, sinceStr)
                .Get();
            MergeTasks(remoteTasks.Models ?? new List<TaskRow>());

            // ── Calendar events ────────────────────────────────────────────
            await PushEventsAsync(client, userId, since);
            var remoteEvents = await client.From<CalendarEventRow>()
                .Filter("user_id", Operator.Equals, userId)
                .Filter("updated_at", Operator.GreaterThan, sinceStr)
                .Get();
            MergeEvents(remoteEvents.Models ?? new List<CalendarEventRow>());

            // ── Projects ───────────────────────────────────────────────────
            await PushProjectsAsync(client, userId, since);
            var remoteProjects = await client.From<ProjectRow>()
                .Filter("user_id", Operator.Equals, userId)
                .Filter("updated_at", Operator.GreaterThan, sinceStr)
                .Get();
            MergeProjects(remoteProjects.Models ?? new List<ProjectRow>());

            var now      = DateTime.UtcNow;
            LastSyncUtc  = now;
            var settings = AppSettings.Load();
            settings.LastSyncUtc = now;
            settings.Save();

            Debug.WriteLine($"[Sync] OK at {now:o}");
            return true;
        }
        catch (Exception ex)
        {
            LastSyncError = BuildErrorMessage(ex);
            Debug.WriteLine($"[Sync] ОШИБКА: {LastSyncError}");
            Debug.WriteLine($"[Sync] StackTrace: {ex.StackTrace}");
            return false;
        }
    }

    private static string BuildErrorMessage(Exception ex)
    {
        var sb = new StringBuilder();
        sb.Append($"[{ex.GetType().Name}] {ex.Message}");
        if (ex.InnerException is { } inner)
            sb.Append($" → {inner.GetType().Name}: {inner.Message}");
        return sb.ToString();
    }

    // ── Push local changes ─────────────────────────────────────────────────

    private async Task PushTasksAsync(global::Supabase.Client client, string userId, DateTime since)
    {
        var changed = _db.GetAllTasks()
            .Where(t => t.LastModified > since)
            .Select(t => ToRow(t, userId))
            .ToList();
        if (changed.Count == 0) return;
        await client.From<TaskRow>().Upsert(changed);
    }

    private async Task PushEventsAsync(global::Supabase.Client client, string userId, DateTime since)
    {
        var changed = _db.GetEventsForPeriod(DateTime.MinValue.AddYears(1), DateTime.MaxValue.AddYears(-1))
            .GroupBy(e => e.SyncId).Select(g => g.First())
            .Where(e => e.LastModified > since)
            .Select(e => ToRow(e, userId))
            .ToList();
        if (changed.Count == 0) return;
        await client.From<CalendarEventRow>().Upsert(changed);
    }

    private async Task PushProjectsAsync(global::Supabase.Client client, string userId, DateTime since)
    {
        var changed = _db.GetAllProjects()
            .Where(p => p.LastModified > since)
            .Select(p => ToRow(p, userId))
            .ToList();
        if (changed.Count == 0) return;
        await client.From<ProjectRow>().Upsert(changed);
    }

    // ── Merge remote → local ───────────────────────────────────────────────

    private void MergeTasks(IEnumerable<TaskRow> remoteRows)
    {
        var localBySync = _db.GetAllTasks().ToDictionary(t => t.SyncId.ToString());

        foreach (var row in remoteRows)
        {
            if (localBySync.TryGetValue(row.Id, out var local))
            {
                if (row.UpdatedAt <= local.LastModified) continue;
                var updated = FromRow(row, local.Id);
                _db.UpsertTask(updated);
            }
            else
            {
                _db.UpsertTask(FromRow(row, 0));
            }
        }
    }

    private void MergeEvents(IEnumerable<CalendarEventRow> remoteRows)
    {
        var localBySync = _db.GetEventsForPeriod(DateTime.MinValue.AddYears(1), DateTime.MaxValue.AddYears(-1))
            .GroupBy(e => e.SyncId.ToString())
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var row in remoteRows)
        {
            if (localBySync.TryGetValue(row.Id, out var local))
            {
                if (row.UpdatedAt <= local.LastModified) continue;
                var updated = FromRow(row, local.Id);
                _db.UpsertEvent(updated);
            }
            else
            {
                _db.UpsertEvent(FromRow(row, 0));
            }
        }
    }

    private void MergeProjects(IEnumerable<ProjectRow> remoteRows)
    {
        var localBySync = _db.GetAllProjects().ToDictionary(p => p.SyncId.ToString());

        foreach (var row in remoteRows)
        {
            if (localBySync.TryGetValue(row.Id, out var local))
            {
                if (row.UpdatedAt <= local.LastModified) continue;
                var updated = FromRow(row, local.Id);
                _db.UpsertProject(updated);
            }
            else
            {
                _db.UpsertProject(FromRow(row, 0));
            }
        }
    }

    // ── Mappers ────────────────────────────────────────────────────────────

    private static TaskRow ToRow(TaskItem t, string userId) => new()
    {
        Id = t.SyncId.ToString(),
        UserId = userId,
        Title = t.Title,
        Description = t.Description,
        DueDate = t.DueDate,
        Priority = t.Priority,
        IsCompleted = t.IsCompleted,
        IsDeleted = t.IsDeleted,
        UpdatedAt = t.LastModified
    };

    private static TaskItem FromRow(TaskRow r, int localId) => new()
    {
        Id = localId,
        SyncId = Guid.Parse(r.Id),
        UserId = r.UserId,
        Title = r.Title,
        Description = r.Description,
        DueDate = r.DueDate,
        Priority = r.Priority,
        IsCompleted = r.IsCompleted,
        IsDeleted = r.IsDeleted,
        LastModified = r.UpdatedAt
    };

    private static CalendarEventRow ToRow(CalendarEvent e, string userId) => new()
    {
        Id = e.SyncId.ToString(),
        UserId = userId,
        Title = e.Title,
        StartAt = e.Start,
        EndAt = e.End,
        Color = e.Color,
        IsAllDay = e.IsAllDay,
        Recurrence = (int)e.Recurrence,
        IsDeleted = e.IsDeleted,
        UpdatedAt = e.LastModified
    };

    private static CalendarEvent FromRow(CalendarEventRow r, int localId) => new()
    {
        Id = localId,
        SyncId = Guid.Parse(r.Id),
        UserId = r.UserId,
        Title = r.Title,
        Start = r.StartAt,
        End = r.EndAt,
        Color = r.Color,
        IsAllDay = r.IsAllDay,
        Recurrence = (RecurrenceType)r.Recurrence,
        IsDeleted = r.IsDeleted,
        LastModified = r.UpdatedAt
    };

    private static ProjectRow ToRow(ProjectItem p, string userId) => new()
    {
        Id = p.SyncId.ToString(),
        UserId = userId,
        Name = p.Name,
        Color = p.Color,
        IsDeleted = p.IsDeleted,
        UpdatedAt = p.LastModified
    };

    private static ProjectItem FromRow(ProjectRow r, int localId) => new()
    {
        Id = localId,
        SyncId = Guid.Parse(r.Id),
        UserId = r.UserId,
        Name = r.Name,
        Color = r.Color,
        IsDeleted = r.IsDeleted,
        LastModified = r.UpdatedAt
    };
}
