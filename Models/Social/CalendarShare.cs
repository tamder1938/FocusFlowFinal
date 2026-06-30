using System;

namespace FocusFlowFinal.Models.Social;

public class CalendarShare
{
    public string Id { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public string SharedWithUserId { get; set; } = string.Empty;
    public FriendProfile? SharedWithProfile { get; set; }
    public string Permission { get; set; } = "view"; // "view" | "sync"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
