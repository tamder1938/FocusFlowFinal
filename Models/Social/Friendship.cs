using System;

namespace FocusFlowFinal.Models.Social;

public class Friendship
{
    public string Id { get; set; } = string.Empty;
    public string RequesterId { get; set; } = string.Empty;
    public string AddresseeId { get; set; } = string.Empty;
    public FriendProfile? RequesterProfile { get; set; }
    public FriendProfile? AddresseeProfile { get; set; }
    public string Status { get; set; } = "pending"; // "pending" | "accepted" | "declined"
    public DateTime CreatedAt { get; set; }

    public FriendProfile? OtherProfile(string myUserId) =>
        RequesterId == myUserId ? AddresseeProfile : RequesterProfile;
}
