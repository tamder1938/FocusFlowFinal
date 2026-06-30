using LiteDB;

namespace FocusFlowFinal.Models.Wishlist;

public class WishlistConditionalFormat
{
    [BsonId] public int Id { get; set; }
    public int WishlistId { get; set; }
    public string Name { get; set; } = string.Empty;

    public int ColumnId { get; set; }
    public WishlistFilterCondition Condition { get; set; }
    public string? Value { get; set; }
    public string? Value2 { get; set; }

    // Style
    public string? BackgroundColor { get; set; } // HEX or null
    public string? TextColor { get; set; }        // HEX or null
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }

    public int Priority { get; set; } // lower = higher priority
}
