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

public class CalendarShareService : ICalendarShareService
{
    private readonly SupabaseClientProvider _provider;
    private readonly IAuthService _auth;

    public CalendarShareService(SupabaseClientProvider provider, IAuthService auth)
    {
        _provider = provider;
        _auth = auth;
    }

    private string? MyUserId => _auth.CurrentUser?.UserId;

    private async Task<global::Supabase.Client?> GetClient()
        => await _provider.GetClientAsync();

    public async Task<(List<CalendarShare> shares, string? error)> GetMySharesAsync()
    {
        var client = await GetClient();
        if (client == null) return (new List<CalendarShare>(), "Нет подключения к серверу");
        if (MyUserId == null) return (new List<CalendarShare>(), "Необходима авторизация");

        try
        {
            var result = await client.From<CalendarShareRow>()
                .Filter("owner_user_id", Operator.Equals, MyUserId)
                .Filter("is_active", Operator.Equals, "true")
                .Get();

            var shares = await EnrichSharesAsync(client, result.Models ?? new List<CalendarShareRow>(), forRecipient: true);
            return (shares, null);
        }
        catch (Exception ex)
        {
            return (new List<CalendarShare>(), FormatError(ex));
        }
    }

    public async Task<(List<CalendarShare> shares, string? error)> GetSharedWithMeAsync()
    {
        var client = await GetClient();
        if (client == null) return (new List<CalendarShare>(), "Нет подключения к серверу");
        if (MyUserId == null) return (new List<CalendarShare>(), "Необходима авторизация");

        try
        {
            var result = await client.From<CalendarShareRow>()
                .Filter("shared_with_user_id", Operator.Equals, MyUserId)
                .Filter("is_active", Operator.Equals, "true")
                .Get();

            var shares = await EnrichSharesAsync(client, result.Models ?? new List<CalendarShareRow>(), forRecipient: false);
            return (shares, null);
        }
        catch (Exception ex)
        {
            return (new List<CalendarShare>(), FormatError(ex));
        }
    }

    public async Task<string?> ShareCalendarAsync(string withUserId, string permission)
    {
        var client = await GetClient();
        if (client == null) return "Нет подключения к серверу";
        if (MyUserId == null) return "Необходима авторизация";

        try
        {
            var row = new CalendarShareRow
            {
                Id = Guid.NewGuid().ToString(),
                OwnerUserId = MyUserId,
                SharedWithUserId = withUserId,
                Permission = permission,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await client.From<CalendarShareRow>().Upsert(row);
            return null;
        }
        catch (Exception ex)
        {
            return FormatError(ex);
        }
    }

    public async Task<string?> RevokeShareAsync(string shareId)
    {
        var client = await GetClient();
        if (client == null) return "Нет подключения к серверу";

        try
        {
            await client.From<CalendarShareRow>()
                .Filter("id", Operator.Equals, shareId)
                .Set(s => s.IsActive, false)
                .Update();
            return null;
        }
        catch (Exception ex)
        {
            return FormatError(ex);
        }
    }

    public async Task<string?> UpdatePermissionAsync(string shareId, string permission)
    {
        var client = await GetClient();
        if (client == null) return "Нет подключения к серверу";

        try
        {
            await client.From<CalendarShareRow>()
                .Filter("id", Operator.Equals, shareId)
                .Set(s => s.Permission, permission)
                .Update();
            return null;
        }
        catch (Exception ex)
        {
            return FormatError(ex);
        }
    }

    // Converts raw PostgREST JSON errors into readable Russian messages.
    // 42703 = column does not exist — signals a missing SQL migration.
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

    private async Task<List<CalendarShare>> EnrichSharesAsync(
        global::Supabase.Client client, List<CalendarShareRow> rows, bool forRecipient)
    {
        var result = new List<CalendarShare>();
        foreach (var row in rows)
        {
            var targetId = forRecipient ? row.SharedWithUserId : row.OwnerUserId;
            FriendProfile? profile = null;
            try
            {
                var p = await client.From<UserProfileRow>()
                    .Filter("id", Operator.Equals, targetId)
                    .Single();
                if (p != null)
                    profile = new FriendProfile
                    {
                        UserId = p.Id,
                        Username = p.Username,
                        Email = p.Email,
                        AvatarUrl = p.AvatarUrl
                    };
            }
            catch { }

            result.Add(new CalendarShare
            {
                Id = row.Id,
                OwnerUserId = row.OwnerUserId,
                SharedWithUserId = row.SharedWithUserId,
                SharedWithProfile = profile,
                Permission = row.Permission,
                IsActive = row.IsActive,
                CreatedAt = row.CreatedAt
            });
        }
        return result;
    }
}
