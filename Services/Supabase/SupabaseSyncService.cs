using FocusFlowFinal.Models;
using FocusFlowFinal.Models.Wishlist;
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
    private readonly IWishlistRepository _wishlistRepo;

    // Нижняя граница диапазона синхронизации при первом запуске (избегаем DateTime.MinValue)
    private static readonly DateTime SyncEpoch = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public DateTime? LastSyncUtc { get; private set; }
    public string?   LastSyncError { get; private set; }

    public SupabaseSyncService(IDatabaseService db, IAuthService auth, SupabaseClientProvider provider,
        IWishlistRepository wishlistRepo)
    {
        _db = db;
        _auth = auth;
        _provider = provider;
        _wishlistRepo = wishlistRepo;
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

            // ── Wishlists ──────────────────────────────────────────────────
            await PushWishlistsAsync(client, userId, since);
            var remoteWishlists = await client.From<WishlistItemRow>()
                .Filter("user_id", Operator.Equals, userId)
                .Filter("updated_at", Operator.GreaterThan, sinceStr)
                .Get();
            MergeWishlists(remoteWishlists.Models ?? new List<WishlistItemRow>());

            await PushWishlistColumnsAsync(client, since);
            await PushWishlistRowsAsync(client, since);
            await PushWishlistCellsAsync(client, since);
            await PushWishlistSharesAsync(client, userId, since);

            // Pull shared wishlists (where this user is shared_with)
            var sharedWishlists = await client.From<WishlistShareRow>()
                .Filter("shared_with_user_id", Operator.Equals, userId)
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();
            await PullSharedWishlistDataAsync(client, sharedWishlists.Models ?? new List<WishlistShareRow>(), since);

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
        // Include soft-deleted tasks so Supabase also marks them deleted
        var changed = _db.GetAllTasksIncludingDeleted()
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
        // Include soft-deleted tasks so we recognize them and don't re-insert
        var localBySync = _db.GetAllTasksIncludingDeleted().ToDictionary(t => t.SyncId.ToString());

        foreach (var row in remoteRows)
        {
            if (localBySync.TryGetValue(row.Id, out var local))
            {
                // Local is as new or newer — local wins
                if (row.UpdatedAt <= local.LastModified) continue;
                // Task was deleted locally — never resurrect it from an older server copy
                if (local.IsDeleted) continue;
                _db.UpsertTask(FromRow(row, local.Id));
            }
            else
            {
                // Unknown locally — only insert if the server copy is not deleted
                if (!row.IsDeleted)
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

    // ── Wishlist push ──────────────────────────────────────────────────────

    private async Task PushWishlistsAsync(global::Supabase.Client client, string userId, DateTime since)
    {
        var changed = _wishlistRepo.GetAllForSync()
            .Where(w => w.UpdatedAt > since)
            .Select(w => ToWishlistRow(w, userId))
            .ToList();
        if (changed.Count == 0) return;
        await client.From<WishlistItemRow>().Upsert(changed);
    }

    private async Task PushWishlistColumnsAsync(global::Supabase.Client client, DateTime since)
    {
        var allWishlists = _wishlistRepo.GetAllForSync().ToList();
        var rows = new List<WishlistColumnRow>();
        foreach (var w in allWishlists)
            rows.AddRange(_wishlistRepo.GetColumnsForSync(w.Id)
                .Where(c => c.UpdatedAt > since)
                .Select(c => ToColumnRow(c, w.SyncId)));
        if (rows.Count == 0) return;
        await client.From<WishlistColumnRow>().Upsert(rows);
    }

    private async Task PushWishlistRowsAsync(global::Supabase.Client client, DateTime since)
    {
        var allWishlists = _wishlistRepo.GetAllForSync().ToList();
        var rows = new List<WishlistRowRow>();
        foreach (var w in allWishlists)
            rows.AddRange(_wishlistRepo.GetRowsForSync(w.Id)
                .Where(r => r.UpdatedAt > since)
                .Select(r => ToRowRow(r, w.SyncId)));
        if (rows.Count == 0) return;
        await client.From<WishlistRowRow>().Upsert(rows);
    }

    private async Task PushWishlistCellsAsync(global::Supabase.Client client, DateTime since)
    {
        var changed = _wishlistRepo.GetAllCellsForSync()
            .Where(c => c.UpdatedAt > since)
            .Select(c => ToCellRow(c))
            .ToList();
        if (changed.Count == 0) return;
        await client.From<WishlistCellRow>().Upsert(changed);
    }

    private async Task PushWishlistSharesAsync(global::Supabase.Client client, string userId, DateTime since)
    {
        var allWishlists = _wishlistRepo.GetAllForSync().ToList();
        var rows = _wishlistRepo.GetAllSharesForSync()
            .Select(s =>
            {
                var w = allWishlists.FirstOrDefault(x => x.Id == s.WishlistId);
                return ToShareRow(s, userId, w?.SyncId ?? Guid.Empty);
            })
            .Where(r => r != null)
            .Cast<WishlistShareRow>()
            .ToList();
        if (rows.Count == 0) return;
        await client.From<WishlistShareRow>().Upsert(rows);
    }

    private void MergeWishlists(IEnumerable<WishlistItemRow> remoteRows)
    {
        foreach (var row in remoteRows)
        {
            var syncId = Guid.Parse(row.Id);
            var local = _wishlistRepo.GetBySyncId(syncId);
            var model = FromWishlistRow(row, local?.Id ?? 0);
            _wishlistRepo.UpsertFromSync(model);
        }
    }

    private async Task PullSharedWishlistDataAsync(global::Supabase.Client client,
        IEnumerable<WishlistShareRow> shares, DateTime since)
    {
        foreach (var share in shares)
        {
            _wishlistRepo.UpsertShareFromSync(FromShareRow(share));

            // Pull the shared wishlist itself
            var wResult = await client.From<WishlistItemRow>()
                .Filter("id", Operator.Equals, share.WishlistId)
                .Get();
            MergeWishlists(wResult.Models ?? new List<WishlistItemRow>());

            // Pull columns/rows/cells for this wishlist since last sync
            var sinceStr = since.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var colResult = await client.From<WishlistColumnRow>()
                .Filter("wishlist_id", Operator.Equals, share.WishlistId)
                .Filter("updated_at", Operator.GreaterThan, sinceStr)
                .Get();
            foreach (var c in colResult.Models ?? new List<WishlistColumnRow>())
                _wishlistRepo.UpsertColumnFromSync(FromColumnRow(c));

            var rowResult = await client.From<WishlistRowRow>()
                .Filter("wishlist_id", Operator.Equals, share.WishlistId)
                .Filter("updated_at", Operator.GreaterThan, sinceStr)
                .Get();
            foreach (var r in rowResult.Models ?? new List<WishlistRowRow>())
            {
                _wishlistRepo.UpsertRowFromSync(FromRowRow(r));
                var cellResult = await client.From<WishlistCellRow>()
                    .Filter("row_id", Operator.Equals, r.Id)
                    .Filter("updated_at", Operator.GreaterThan, sinceStr)
                    .Get();
                foreach (var cell in cellResult.Models ?? new List<WishlistCellRow>())
                    _wishlistRepo.UpsertCellFromSync(FromCellRow(cell));
            }
        }
    }

    // ── Wishlist mappers ───────────────────────────────────────────────────

    private static WishlistItemRow ToWishlistRow(WishlistItem w, string userId) => new()
    {
        Id = w.SyncId.ToString(),
        UserId = userId,
        Name = w.Name,
        Description = w.Description,
        CreatedAt = w.CreatedAt,
        UpdatedAt = w.UpdatedAt,
        IsDeleted = w.IsDeleted
    };

    private static WishlistItem FromWishlistRow(WishlistItemRow r, int localId) => new()
    {
        Id = localId,
        SyncId = Guid.Parse(r.Id),
        UserId = r.UserId,
        Name = r.Name,
        Description = r.Description,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        IsDeleted = r.IsDeleted
    };

    private static WishlistColumnRow ToColumnRow(WishlistColumn c, Guid wishlistSyncId) => new()
    {
        Id = c.SyncId.ToString(),
        WishlistId = wishlistSyncId.ToString(),
        Name = c.Name,
        ColType = (int)c.Type,
        ColOrder = c.Order,
        OptionsJson = c.OptionsJson,
        IsHidden = c.IsHidden,
        UpdatedAt = c.UpdatedAt,
        IsDeleted = c.IsDeleted
    };

    private static WishlistColumn FromColumnRow(WishlistColumnRow r) => new()
    {
        SyncId = Guid.Parse(r.Id),
        Name = r.Name,
        Type = (WishlistColumnType)r.ColType,
        Order = r.ColOrder,
        OptionsJson = r.OptionsJson,
        IsHidden = r.IsHidden,
        UpdatedAt = r.UpdatedAt,
        IsDeleted = r.IsDeleted
    };

    private static WishlistRowRow ToRowRow(WishlistRow row, Guid wishlistSyncId) => new()
    {
        Id = row.SyncId.ToString(),
        WishlistId = wishlistSyncId.ToString(),
        RowOrder = row.Order,
        UpdatedAt = row.UpdatedAt,
        IsDeleted = row.IsDeleted
    };

    private static WishlistRow FromRowRow(WishlistRowRow r) => new()
    {
        SyncId = Guid.Parse(r.Id),
        Order = r.RowOrder,
        UpdatedAt = r.UpdatedAt,
        IsDeleted = r.IsDeleted
    };

    private static WishlistCellRow ToCellRow(WishlistCell c) => new()
    {
        Id = c.SyncId.ToString(),
        RowId = c.RowId.ToString(),
        ColumnId = c.ColumnId.ToString(),
        Value = c.Value,
        Extra = c.Extra,
        UpdatedAt = c.UpdatedAt
    };

    private static WishlistCell FromCellRow(WishlistCellRow r) => new()
    {
        SyncId = Guid.Parse(r.Id),
        Value = r.Value,
        Extra = r.Extra,
        UpdatedAt = r.UpdatedAt
    };

    private static WishlistShareRow? ToShareRow(WishlistShare s, string ownerUserId, Guid wishlistSyncId)
    {
        if (wishlistSyncId == Guid.Empty) return null;
        return new WishlistShareRow
        {
            Id = s.SyncId.ToString(),
            WishlistId = wishlistSyncId.ToString(),
            OwnerUserId = ownerUserId,
            SharedWithEmail = s.SharedWithEmail,
            SharedWithUserId = s.SharedWithUserId,
            Permission = s.Permission,
            IsAccepted = s.IsAccepted,
            CreatedAt = s.CreatedAt,
            IsDeleted = s.IsDeleted
        };
    }

    private static WishlistShare FromShareRow(WishlistShareRow r) => new()
    {
        SyncId = Guid.Parse(r.Id),
        WishlistSyncId = Guid.TryParse(r.WishlistId, out var wid) ? wid : Guid.Empty,
        SharedWithEmail = r.SharedWithEmail,
        SharedWithUserId = r.SharedWithUserId,
        Permission = r.Permission,
        IsAccepted = r.IsAccepted,
        CreatedAt = r.CreatedAt,
        IsDeleted = r.IsDeleted
    };
}
