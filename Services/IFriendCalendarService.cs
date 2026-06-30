using FocusFlowFinal.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

public interface IFriendCalendarService
{
    Task<List<FriendEventDisplayItem>> GetFriendEventsForDayAsync(DateTime date);
    Task<List<(string friendName, string friendUserId, List<CalendarEvent> events)>> GetFriendEventsForWeekAsync(DateTime weekStart, DateTime weekEnd);
}
