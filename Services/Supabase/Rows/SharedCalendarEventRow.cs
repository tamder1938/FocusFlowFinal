using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace FocusFlowFinal.Services.Supabase.Rows;

[Table("shared_calendar_events")]
public class SharedCalendarEventRow : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; } = string.Empty;

    [Column("owner_user_id")]
    public string OwnerUserId { get; set; } = string.Empty;

    [Column("created_by_user_id")]
    public string CreatedByUserId { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("start_at")]
    public DateTime StartAt { get; set; }

    [Column("end_at")]
    public DateTime EndAt { get; set; }

    [Column("color")]
    public string Color { get; set; } = "#6366F1";

    [Column("is_all_day")]
    public bool IsAllDay { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
