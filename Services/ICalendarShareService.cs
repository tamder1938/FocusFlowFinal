using FocusFlowFinal.Models.Social;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

public interface ICalendarShareService
{
    Task<(List<CalendarShare> shares, string? error)> GetMySharesAsync();
    Task<(List<CalendarShare> shares, string? error)> GetSharedWithMeAsync();
    Task<string?> ShareCalendarAsync(string withUserId, string permission);
    Task<string?> RevokeShareAsync(string shareId);
    Task<string?> UpdatePermissionAsync(string shareId, string permission);
}
