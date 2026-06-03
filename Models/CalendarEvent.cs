using LiteDB;
using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Models;

public enum RecurrenceType
{
    None,
    Daily,
    Weekdays,
    Weekly,
    Monthly,
    Yearly,
    Shift,
    Custom
}

public enum IntervalUnit
{
    Days,
    Weeks,
    Months
}

public class CalendarEvent
{
    [BsonId]
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Color { get; set; } = "#3498db";
    public int? TaskId { get; set; }

    public bool IsAllDay { get; set; }
    public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;

    public List<DayOfWeek> DaysOfWeek { get; set; } = new();

    public int? WorkingDays { get; set; }
    public int? OffDays { get; set; }
    public DateTime? CycleStartDate { get; set; }

    public int? IntervalValue { get; set; }
    public IntervalUnit? IntervalUnit { get; set; }

    // ПОЛЕ ИСКЛЮЧЕНИЙ: Сюда сохраняются даты, в которые серийное событие было отменено
    public List<DateTime> ExceptionDates { get; set; } = new();
}
