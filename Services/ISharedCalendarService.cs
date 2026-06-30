using FocusFlowFinal.Models.Social;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

public interface ISharedCalendarService
{
    // Events I created in friends' calendars
    Task<(List<SharedCalendarEvent> events, string? error)> GetMyCreatedEventsAsync();

    // Events others created in my calendar
    Task<(List<SharedCalendarEvent> events, string? error)> GetEventsInMyCalendarAsync();

    // Events in a specific friend's calendar (for viewing)
    Task<(List<SharedCalendarEvent> events, string? error)> GetEventsForFriendAsync(string ownerUserId, DateTime from, DateTime to);

    Task<string?> AddEventAsync(SharedCalendarEvent ev);
    Task<string?> UpdateEventAsync(SharedCalendarEvent ev);
    Task<string?> DeleteEventAsync(string eventId);

    // Returns friends who have given me 'sync' permission
    Task<(List<(string userId, string name)> friends, string? error)> GetSyncFriendsAsync();
}
