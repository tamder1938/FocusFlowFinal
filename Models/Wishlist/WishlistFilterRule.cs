using System.Collections.Generic;

namespace FocusFlowFinal.Models.Wishlist;

public class WishlistFilterRule
{
    public int ColumnId { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public WishlistColumnType ColumnType { get; set; }
    public WishlistFilterCondition Condition { get; set; }
    public string? Value { get; set; }
    public string? Value2 { get; set; }
    public List<string> SelectedOptions { get; set; } = new();
}
