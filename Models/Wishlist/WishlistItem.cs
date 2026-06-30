using LiteDB;
using System;

namespace FocusFlowFinal.Models.Wishlist;

public class WishlistItem
{
    [BsonId]
    public int Id { get; set; }
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}
