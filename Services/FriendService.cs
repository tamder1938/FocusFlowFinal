using FocusFlowFinal.Models.Social;
using FocusFlowFinal.Services.Supabase;
using FocusFlowFinal.Services.Supabase.Rows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Supabase.Postgrest.Constants;

namespace FocusFlowFinal.Services;

public class FriendService : IFriendService
{
    private readonly SupabaseClientProvider _provider;
    private readonly IAuthService _auth;

    public FriendService(SupabaseClientProvider provider, IAuthService auth)
    {
        _provider = provider;
        _auth = auth;
    }

    private string? MyUserId => _auth.CurrentUser?.UserId;

    private async Task<global::Supabase.Client?> GetClient()
        => await _provider.GetClientAsync();

    public async Task<(List<FriendProfile> results, string? error)> SearchUsersAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return (new List<FriendProfile>(), null);

        var client = await GetClient();
        if (client == null) return (new List<FriendProfile>(), "Нет подключения к серверу");

        try
        {
            var result = await client.From<UserProfileRow>()
                .Filter("email", Operator.Like, $"%{query.ToLower()}%")
                .Limit(20)
                .Get();

            var profiles = (result.Models ?? new List<UserProfileRow>())
                .Where(p => p.Id != MyUserId) // exclude self
                .Select(p => new FriendProfile
                {
                    UserId = p.Id,
                    Username = p.Username,
                    Email = p.Email,
                    AvatarUrl = p.AvatarUrl
                })
                .ToList();

            return (profiles, null);
        }
        catch (Exception ex)
        {
            return (new List<FriendProfile>(), ex.Message);
        }
    }

    public async Task<string?> SendRequestAsync(string targetUserId)
    {
        var client = await GetClient();
        if (client == null) return "Нет подключения к серверу";
        if (MyUserId == null) return "Необходима авторизация";

        try
        {
            var row = new FriendshipRow
            {
                Id = Guid.NewGuid().ToString(),
                RequesterId = MyUserId,
                AddresseeId = targetUserId,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await client.From<FriendshipRow>().Upsert(row);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public async Task<string?> AcceptRequestAsync(string friendshipId)
    {
        var client = await GetClient();
        if (client == null) return "Нет подключения к серверу";

        try
        {
            await client.From<FriendshipRow>()
                .Filter("id", Operator.Equals, friendshipId)
                .Set(f => f.Status, "accepted")
                .Set(f => f.UpdatedAt, DateTime.UtcNow)
                .Update();
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public async Task<string?> DeclineRequestAsync(string friendshipId)
    {
        var client = await GetClient();
        if (client == null) return "Нет подключения к серверу";

        try
        {
            await client.From<FriendshipRow>()
                .Filter("id", Operator.Equals, friendshipId)
                .Delete();
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public async Task<(List<Friendship> friends, string? error)> GetFriendsAsync()
    {
        var client = await GetClient();
        if (client == null) return (new List<Friendship>(), "Нет подключения к серверу");
        if (MyUserId == null) return (new List<Friendship>(), "Необходима авторизация");

        try
        {
            var sent = await client.From<FriendshipRow>()
                .Filter("requester_id", Operator.Equals, MyUserId)
                .Filter("status", Operator.Equals, "accepted")
                .Get();
            var received = await client.From<FriendshipRow>()
                .Filter("addressee_id", Operator.Equals, MyUserId)
                .Filter("status", Operator.Equals, "accepted")
                .Get();

            var all = (sent.Models ?? new List<FriendshipRow>())
                .Concat(received.Models ?? new List<FriendshipRow>())
                .ToList();

            var friends = await EnrichFriendshipsAsync(client, all);
            return (friends, null);
        }
        catch (Exception ex)
        {
            return (new List<Friendship>(), ex.Message);
        }
    }

    public async Task<(List<Friendship> requests, string? error)> GetIncomingRequestsAsync()
    {
        var client = await GetClient();
        if (client == null) return (new List<Friendship>(), "Нет подключения к серверу");
        if (MyUserId == null) return (new List<Friendship>(), "Необходима авторизация");

        try
        {
            var result = await client.From<FriendshipRow>()
                .Filter("addressee_id", Operator.Equals, MyUserId)
                .Filter("status", Operator.Equals, "pending")
                .Get();

            var requests = await EnrichFriendshipsAsync(client, result.Models ?? new List<FriendshipRow>());
            return (requests, null);
        }
        catch (Exception ex)
        {
            return (new List<Friendship>(), ex.Message);
        }
    }

    public async Task<(List<Friendship> requests, string? error)> GetOutgoingRequestsAsync()
    {
        var client = await GetClient();
        if (client == null) return (new List<Friendship>(), "Нет подключения к серверу");
        if (MyUserId == null) return (new List<Friendship>(), "Необходима авторизация");

        try
        {
            var result = await client.From<FriendshipRow>()
                .Filter("requester_id", Operator.Equals, MyUserId)
                .Filter("status", Operator.Equals, "pending")
                .Get();

            var requests = await EnrichFriendshipsAsync(client, result.Models ?? new List<FriendshipRow>());
            return (requests, null);
        }
        catch (Exception ex)
        {
            return (new List<Friendship>(), ex.Message);
        }
    }

    private async Task<List<Friendship>> EnrichFriendshipsAsync(
        global::Supabase.Client client, List<FriendshipRow> rows)
    {
        if (rows.Count == 0) return new List<Friendship>();

        // Collect all unique user IDs to look up
        var userIds = rows.SelectMany(r => new[] { r.RequesterId, r.AddresseeId })
            .Distinct()
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        var profiles = new Dictionary<string, FriendProfile>();
        foreach (var uid in userIds)
        {
            try
            {
                var p = await client.From<UserProfileRow>()
                    .Filter("id", Operator.Equals, uid)
                    .Single();
                if (p != null)
                    profiles[uid] = new FriendProfile
                    {
                        UserId = p.Id,
                        Username = p.Username,
                        Email = p.Email,
                        AvatarUrl = p.AvatarUrl
                    };
            }
            catch { /* profile might not exist yet */ }
        }

        return rows.Select(r => new Friendship
        {
            Id = r.Id,
            RequesterId = r.RequesterId,
            AddresseeId = r.AddresseeId,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            RequesterProfile = profiles.GetValueOrDefault(r.RequesterId),
            AddresseeProfile = profiles.GetValueOrDefault(r.AddresseeId)
        }).ToList();
    }
}
