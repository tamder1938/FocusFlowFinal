using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace FocusFlowFinal.Services.Supabase.Rows;

[Table("calendar_events")]
public class CalendarEventRow : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; } = string.Empty;

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("start_at")]
    public DateTime StartAt { get; set; }

    [Column("end_at")]
    public DateTime EndAt { get; set; }

    [Column("color")]
    public string Color { get; set; } = "#3498db";

    [Column("is_all_day")]
    public bool IsAllDay { get; set; }

    [Column("recurrence")]
    public int Recurrence { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
