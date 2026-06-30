using System;

namespace FocusFlowFinal.Models.Social;

public class SharedCalendarEvent
{
    public string Id { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;      // friend (owner of the calendar slot)
    public string CreatedByUserId { get; set; } = string.Empty;  // who created this event
    public string Title { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Color { get; set; } = "#6366F1";
    public bool IsAllDay { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime UpdatedAt { get; set; }
}
