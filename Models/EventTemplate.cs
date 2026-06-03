using System;
using System.Collections.Generic;
using LiteDB;

namespace FocusFlowFinal.Models;

public class EventTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsAllDay { get; set; }
    public int StartHour { get; set; }
    public int StartMinute { get; set; }
    public int EndHour { get; set; }
    public int EndMinute { get; set; }
    public string? Color { get; set; }
    public RecurrenceType Recurrence { get; set; }

    // Поля структуры повторения (синхронизировано со свойствами формы)
    public List<DayOfWeek>? DaysOfWeek { get; set; }
    public int? WorkingDays { get; set; }
    public int? OffDays { get; set; }
    public DateTime? CycleStartDate { get; set; }
    public int? IntervalValue { get; set; }
    public IntervalUnit? IntervalUnit { get; set; }
}
