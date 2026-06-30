using LiteDB;
using System;

namespace FocusFlowFinal.Models.Wishlist;

public class WishlistColumn
{
    [BsonId]
    public int Id { get; set; }
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public int WishlistId { get; set; }
    public string Name { get; set; } = string.Empty;
    public WishlistColumnType Type { get; set; } = WishlistColumnType.Text;
    public int Order { get; set; }
    public string? OptionsJson { get; set; }
    public bool IsHidden { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}
