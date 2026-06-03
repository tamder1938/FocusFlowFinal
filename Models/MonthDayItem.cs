using System;
using System.Collections.ObjectModel;
using LiteDB;

namespace FocusFlowFinal.Models;

public class MonthDayItem
{
    public DateTime Date { get; set; }
    public bool IsCurrentMonth { get; set; }
    public string DayNumber { get; set; } = string.Empty;
    public ObservableCollection<CalendarEvent> Events { get; set; } = new();
}
