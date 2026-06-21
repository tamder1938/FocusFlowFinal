using System;
using System.Collections.ObjectModel;

namespace FocusFlowFinal.Models;

public class MonthDayItem
{
    public DateTime Date { get; set; }
    public bool IsCurrentMonth { get; set; }
    public string DayNumber { get; set; } = string.Empty;
    public ObservableCollection<CalendarEvent> Events { get; set; } = new();

    /// <summary>true, если эта ячейка соответствует сегодняшней дате (для синего бейджа в MonthView).</summary>
    public bool IsToday => Date.Date == DateTime.Today;

    public bool HasNotes { get; set; }
    public int  NoteCount { get; set; }
}
