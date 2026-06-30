using FocusFlowFinal.Models.Social;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

public interface IFriendService
{
    Task<(List<FriendProfile> results, string? error)> SearchUsersAsync(string query);
    Task<string?> SendRequestAsync(string targetUserId);
    Task<string?> AcceptRequestAsync(string friendshipId);
    Task<string?> DeclineRequestAsync(string friendshipId);
    Task<(List<Friendship> friends, string? error)> GetFriendsAsync();
    Task<(List<Friendship> requests, string? error)> GetIncomingRequestsAsync();
    Task<(List<Friendship> requests, string? error)> GetOutgoingRequestsAsync();
}
