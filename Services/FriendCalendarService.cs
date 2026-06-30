using FocusFlowFinal.Models;
using FocusFlowFinal.Services.Supabase;
using FocusFlowFinal.Services.Supabase.Rows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Supabase.Postgrest.Constants;

namespace FocusFlowFinal.Services;

public class FriendCalendarService : IFriendCalendarService
{
    private readonly SupabaseClientProvider _provider;
    private readonly IAuthService _auth;

    // Muted palette for friend events — rotates per friend userId
    private static readonly string[] FriendColors =
    {
        "#94A3B8", "#A78BFA", "#34D399", "#F472B6",
        "#FB923C", "#38BDF8", "#A3E635", "#F87171"
    };

    public FriendCalendarService(SupabaseClientProvider provider, IAuthService auth)
    {
        _provider = provider;
        _auth = auth;
    }

    private string? MyUserId => _auth.CurrentUser?.UserId;

    private async Task<global::Supabase.Client?> GetClient()
        => await _provider.GetClientAsync();

    private string ColorFor(string userId)
    {
        var hash = Math.Abs(userId.GetHashCode());
        return FriendColors[hash % FriendColors.Length];
    }

    public async Task<List<FriendEventDisplayItem>> GetFriendEventsForDayAsync(DateTime date)
    {
        var client = await GetClient();
        if (client == null || MyUserId == null) return new List<FriendEventDisplayItem>();

        var result = new List<FriendEventDisplayItem>();
        try
        {
            var shares = await client.From<CalendarShareRow>()
                .Filter("shared_with_user_id", Operator.Equals, MyUserId)
                .Filter("is_active", Operator.Equals, "true")
                .Get();

            if (shares.Models == null || shares.Models.Count == 0) return result;

            var dayStart = date.Date.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var dayEnd   = date.Date.AddDays(1).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

            foreach (var share in shares.Models)
            {
                // Get friend name
                string friendName = share.OwnerUserId;
                try
                {
                    var profile = await client.From<UserProfileRow>()
                        .Filter("id", Operator.Equals, share.OwnerUserId)
                        .Single();
                    if (profile != null)
                        friendName = !string.IsNullOrEmpty(profile.Username)
                            ? profile.Username : profile.Email;
                }
                catch { }

                var color = ColorFor(share.OwnerUserId);

                var eventsResult = await client.From<CalendarEventRow>()
                    .Filter("user_id", Operator.Equals, share.OwnerUserId)
                    .Filter("is_deleted", Operator.Equals, "false")
                    .Filter("start_at", Operator.GreaterThanOrEqual, dayStart)
                    .Filter("start_at", Operator.LessThan, dayEnd)
                    .Get();

                foreach (var ev in eventsResult.Models ?? new List<CalendarEventRow>())
                {
                    if (ev.IsDeleted) continue;
                    var startMin = ev.StartAt.Hour * 60 + ev.StartAt.Minute;
                    var endMin   = ev.EndAt.Hour   * 60 + ev.EndAt.Minute;
                    if (endMin <= startMin) endMin = startMin + 30;

                    result.Add(new FriendEventDisplayItem
                    {
                        EventId   = 0,
                        Title     = ev.Title,
                        Color     = color,
                        Top       = startMin,
                        Height    = Math.Max(endMin - startMin, 20),
                        Left      = 4,
                        Width     = 200,
                        TimeLabel = $"{ev.StartAt:HH:mm}–{ev.EndAt:HH:mm}",
                        FriendName   = friendName,
                        FriendUserId = share.OwnerUserId
                    });
                }
            }
        }
        catch { /* offline — return empty */ }

        return result;
    }

    public async Task<List<(string friendName, string friendUserId, List<CalendarEvent> events)>> GetFriendEventsForWeekAsync(
        DateTime weekStart, DateTime weekEnd)
    {
        var client = await GetClient();
        if (client == null || MyUserId == null)
            return new List<(string, string, List<CalendarEvent>)>();

        var result = new List<(string, string, List<CalendarEvent>)>();
        try
        {
            var shares = await client.From<CalendarShareRow>()
                .Filter("shared_with_user_id", Operator.Equals, MyUserId)
                .Filter("is_active", Operator.Equals, "true")
                .Get();

            if (shares.Models == null || shares.Models.Count == 0) return result;

            var fromStr = weekStart.Date.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var toStr   = weekEnd.Date.AddDays(1).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

            foreach (var share in shares.Models)
            {
                string friendName = share.OwnerUserId;
                try
                {
                    var profile = await client.From<UserProfileRow>()
                        .Filter("id", Operator.Equals, share.OwnerUserId)
                        .Single();
                    if (profile != null)
                        friendName = !string.IsNullOrEmpty(profile.Username)
                            ? profile.Username : profile.Email;
                }
                catch { }

                var color = ColorFor(share.OwnerUserId);

                var eventsResult = await client.From<CalendarEventRow>()
                    .Filter("user_id", Operator.Equals, share.OwnerUserId)
                    .Filter("is_deleted", Operator.Equals, "false")
                    .Filter("start_at", Operator.GreaterThanOrEqual, fromStr)
                    .Filter("start_at", Operator.LessThan, toStr)
                    .Get();

                var calEvents = (eventsResult.Models ?? new List<CalendarEventRow>())
                    .Where(e => !e.IsDeleted)
                    .Select(e => new CalendarEvent
                    {
                        Id    = 0,
                        Title = $"[{friendName}] {e.Title}",
                        Color = color,
                        Start = e.StartAt,
                        End   = e.EndAt,
                        IsAllDay   = e.IsAllDay,
                        Recurrence = (RecurrenceType)e.Recurrence
                    })
                    .ToList();

                if (calEvents.Count > 0)
                    result.Add((friendName, share.OwnerUserId, calEvents));
            }
        }
        catch { /* offline */ }

        return result;
    }
}
