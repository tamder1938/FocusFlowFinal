using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace FocusFlowFinal.Services.Supabase.Rows;

[Table("projects")]
public class ProjectRow : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; } = string.Empty;

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("color")]
    public string Color { get; set; } = "#3B82F6";

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
