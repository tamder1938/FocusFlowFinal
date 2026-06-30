using LiteDB;
using System;

namespace FocusFlowFinal.Models.Wishlist;

public class WishlistCell
{
    [BsonId]
    public int Id { get; set; }
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public int RowId { get; set; }
    public int ColumnId { get; set; }
    public string? Value { get; set; }
    public string? Extra { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
