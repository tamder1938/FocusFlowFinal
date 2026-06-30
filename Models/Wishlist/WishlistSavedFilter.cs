using LiteDB;
using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Models.Wishlist;

public class WishlistSavedFilter
{
    [BsonId] public int Id { get; set; }
    public int WishlistId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<WishlistFilterRule> Rules { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
