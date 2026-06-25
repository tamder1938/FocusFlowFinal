using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace FocusFlowFinal.Services.Supabase.Rows;

[Table("tasks")]
public class TaskRow : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; } = string.Empty;

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("priority")]
    public int Priority { get; set; }

    [Column("is_completed")]
    public bool IsCompleted { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
