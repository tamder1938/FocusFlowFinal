using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace FocusFlowFinal.Services.Supabase.Rows;

[Table("wishlist_shares")]
public class WishlistShareRow : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; } = string.Empty;

    [Column("wishlist_id")]
    public string WishlistId { get; set; } = string.Empty;

    [Column("owner_user_id")]
    public string OwnerUserId { get; set; } = string.Empty;

    [Column("shared_with_email")]
    public string SharedWithEmail { get; set; } = string.Empty;

    [Column("shared_with_user_id")]
    public string? SharedWithUserId { get; set; }

    [Column("permission")]
    public string Permission { get; set; } = "view";

    [Column("is_accepted")]
    public bool IsAccepted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
