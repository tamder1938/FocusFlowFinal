using FocusFlowFinal.Models.Social;
using FocusFlowFinal.Services.Supabase;
using FocusFlowFinal.Services.Supabase.Rows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static Supabase.Postgrest.Constants;

namespace FocusFlowFinal.Services;

public class SharedCalendarService : ISharedCalendarService
{
    private readonly SupabaseClientProvider _provider;
    private readonly IAuthService _auth;

    public SharedCalendarService(SupabaseClientProvider provider, IAuthService auth)
    {
        _provider = provider;
        _auth = auth;
    }

    private string? MyUserId => _auth.CurrentUser?.UserId;

    private async Task<global::Supabase.Client?> GetClient()
        => await _provider.GetClientAsync();

    public async Task<(List<SharedCalendarEvent> events, string? error)> GetMyCreatedEventsAsync()
    {
        var client = await GetClient();
        if (client == null) return (new List<SharedCalendarEvent>(), "Нет подключения");
        if (MyUserId == null) return (new List<SharedCalendarEvent>(), "Необходима авторизация");

        try
        {
            var result = await client.From<SharedCalendarEventRow>()
                .Filter("created_by_user_id", Operator.Equals, MyUserId)
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();
            return (Map(result.Models ?? new List<SharedCalendarEventRow>()), null);
        }
        catch (Exception ex)
        {
            return (new List<SharedCalendarEvent>(), FormatError(ex));
        }
    }

    public async Task<(List<SharedCalendarEvent> events, string? error)> GetEventsInMyCalendarAsync()
    {
        var client = await GetClient();
        if (client == null) return (new List<SharedCalendarEvent>(), "Нет подключения");
        if (MyUserId == null) return (new List<SharedCalendarEvent>(), "Необходима авторизация");

        try
        {
            var result = await client.From<SharedCalendarEventRow>()
                .Filter("owner_user_id", Operator.Equals, MyUserId)
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();
            return (Map(result.Models ?? new List<SharedCalendarEventRow>()), null);
        }
        catch (Exception ex)
        {
            return (new List<SharedCalendarEvent>(), FormatError(ex));
        }
    }

    public async Task<(List<SharedCalendarEvent> events, string? error)> GetEventsForFriendAsync(
        string ownerUserId, DateTime from, DateTime to)
    {
        var client = await GetClient();
        if (client == null) return (new List<SharedCalendarEvent>(), "Нет подключения");

        try
        {
            var fromStr = from.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var toStr   = to.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

            var result = await client.From<SharedCalendarEventRow>()
                .Filter("owner_user_id", Operator.Equals, ownerUserId)
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("start_at", Operator.GreaterThanOrEqual, fromStr)
                .Filter("start_at", Operator.LessThan, toStr)
                .Get();
            return (Map(result.Models ?? new List<SharedCalendarEventRow>()), null);
        }
        catch (Exception ex)
        {
            return (new List<SharedCalendarEvent>(), FormatError(ex));
        }
    }

    public async Task<string?> AddEventAsync(SharedCalendarEvent ev)
    {
        var client = await GetClient();
        if (client == null) return "Нет подключения";
        if (MyUserId == null) return "Необходима авторизация";

        try
        {
            var row = ToRow(ev, MyUserId);
            row.Id = Guid.NewGuid().ToString();
            await client.From<SharedCalendarEventRow>().Insert(row);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public async Task<string?> UpdateEventAsync(SharedCalendarEvent ev)
    {
        var client = await GetClient();
        if (client == null) return "Нет подключения";
        if (MyUserId == null) return "Необходима авторизация";

        try
        {
            var row = ToRow(ev, MyUserId);
            await client.From<SharedCalendarEventRow>().Upsert(row);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public async Task<string?> DeleteEventAsync(string eventId)
    {
        var client = await GetClient();
        if (client == null) return "Нет подключения";

        try
        {
            await client.From<SharedCalendarEventRow>()
                .Filter("id", Operator.Equals, eventId)
                .Set(r => r.IsDeleted, true)
                .Set(r => r.UpdatedAt, DateTime.UtcNow)
                .Update();
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public async Task<(List<(string userId, string name)> friends, string? error)> GetSyncFriendsAsync()
    {
        var client = await GetClient();
        if (client == null) return (new List<(string, string)>(), "Нет подключения");
        if (MyUserId == null) return (new List<(string, string)>(), "Необходима авторизация");

        try
        {
            // Friends who shared their calendar with me with 'sync' permission
            var shares = await client.From<CalendarShareRow>()
                .Filter("shared_with_user_id", Operator.Equals, MyUserId)
                .Filter("permission", Operator.Equals, "sync")
                .Filter("is_active", Operator.Equals, "true")
                .Get();

            var result = new List<(string, string)>();
            foreach (var share in shares.Models ?? new List<CalendarShareRow>())
            {
                string name = share.OwnerUserId;
                try
                {
                    var profile = await client.From<UserProfileRow>()
                        .Filter("id", Operator.Equals, share.OwnerUserId)
                        .Single();
                    if (profile != null)
                        name = !string.IsNullOrEmpty(profile.Username)
                            ? profile.Username : profile.Email;
                }
                catch { }
                result.Add((share.OwnerUserId, name));
            }
            return (result, null);
        }
        catch (Exception ex)
        {
            return (new List<(string, string)>(), FormatError(ex));
        }
    }

    private static string FormatError(Exception ex)
    {
        try
        {
            using var doc = JsonDocument.Parse(ex.Message);
            var root = doc.RootElement;
            if (root.TryGetProperty("code", out var code) && code.GetString() == "42703")
                return "Ошибка 42703: столбец is_active отсутствует в таблице calendar_shares. " +
                       "Выполните в Supabase SQL Editor: " +
                       "ALTER TABLE public.calendar_shares " +
                       "ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;";
            if (root.TryGetProperty("message", out var msg))
                return msg.GetString() ?? ex.Message;
        }
        catch { }
        return ex.Message;
    }

    private static List<SharedCalendarEvent> Map(List<SharedCalendarEventRow> rows) =>
        rows.Select(r => new SharedCalendarEvent
        {
            Id = r.Id,
            OwnerUserId = r.OwnerUserId,
            CreatedByUserId = r.CreatedByUserId,
            Title = r.Title,
            StartAt = r.StartAt,
            EndAt = r.EndAt,
            Color = r.Color,
            IsAllDay = r.IsAllDay,
            Notes = r.Notes,
            IsDeleted = r.IsDeleted,
            UpdatedAt = r.UpdatedAt
        }).ToList();

    private static SharedCalendarEventRow ToRow(SharedCalendarEvent ev, string createdBy) => new()
    {
        Id = ev.Id,
        OwnerUserId = ev.OwnerUserId,
        CreatedByUserId = createdBy,
        Title = ev.Title,
        StartAt = ev.StartAt,
        EndAt = ev.EndAt,
        Color = ev.Color,
        IsAllDay = ev.IsAllDay,
        Notes = ev.Notes,
        IsDeleted = ev.IsDeleted,
        UpdatedAt = DateTime.UtcNow
    };
}
