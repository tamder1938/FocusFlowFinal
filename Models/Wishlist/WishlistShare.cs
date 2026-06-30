using LiteDB;
using System;

namespace FocusFlowFinal.Models.Wishlist;

public class WishlistShare
{
    [BsonId]
    public int Id { get; set; }
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public int WishlistId { get; set; }
    public Guid WishlistSyncId { get; set; }
    public string SharedWithEmail { get; set; } = string.Empty;
    public string? SharedWithUserId { get; set; }
    public string Permission { get; set; } = "view"; // "view" | "edit"
    public bool IsAccepted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}
