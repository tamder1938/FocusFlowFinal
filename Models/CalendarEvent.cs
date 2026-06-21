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

public class CalendarEvent : ISyncableEntity
{
    [BsonId]
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    // ИСПРАВЛЕНО (Часть 2-3, п.3): поля синхронизации
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
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

    // Дата окончания повторений. Если задана — события после этой даты не показываются.
    public DateTime? RecurrenceEndDate { get; set; }

    // День месяца для ежемесячного повторения (1-31).
    // Если null — используется день из Start.
    public int? RecurrenceStartDay { get; set; }

    // Месяц для ежегодного повторения (1-12).
    // Если null — используется месяц из Start.
    public int? RecurrenceStartMonth { get; set; }

    // Сюда сохраняются даты, в которые серийное событие было отменено (исключения)
    public List<DateTime> ExceptionDates { get; set; } = new();

    /// <summary>
    /// Время (мин) до начала события, за которое показывать уведомление.
    /// 0 = не уведомлять.
    /// </summary>
    public int NotificationOffsetMinutes { get; set; } = 0;

    /// <summary>
    /// Год окончания ежегодного повторения (включительно).
    /// null = повторять бессрочно.
    /// </summary>
    public int? RecurrenceEndYear { get; set; }

    public PlaceLocation? Location { get; set; }
}
