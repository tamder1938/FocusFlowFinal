using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace FocusFlowFinal.Services.Supabase.Rows;

[Table("wishlist_cells")]
public class WishlistCellRow : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; } = string.Empty;

    [Column("row_id")]
    public string RowId { get; set; } = string.Empty;

    [Column("column_id")]
    public string ColumnId { get; set; } = string.Empty;

    [Column("value")]
    public string? Value { get; set; }

    [Column("extra")]
    public string? Extra { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
