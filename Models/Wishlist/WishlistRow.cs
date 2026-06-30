using LiteDB;
using System;

namespace FocusFlowFinal.Models.Wishlist;

public class WishlistRow
{
    [BsonId]
    public int Id { get; set; }
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public int WishlistId { get; set; }
    public int Order { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}
