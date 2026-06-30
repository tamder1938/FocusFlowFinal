using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace FocusFlowFinal.Services.Supabase.Rows;

[Table("wishlist_columns")]
public class WishlistColumnRow : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; } = string.Empty;

    [Column("wishlist_id")]
    public string WishlistId { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("col_type")]
    public int ColType { get; set; }

    [Column("col_order")]
    public int ColOrder { get; set; }

    [Column("options_json")]
    public string? OptionsJson { get; set; }

    [Column("is_hidden")]
    public bool IsHidden { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
