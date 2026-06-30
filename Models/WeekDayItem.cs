using System;
using System.Collections.ObjectModel;
using System.Globalization;
using FocusFlowFinal.Services;

namespace FocusFlowFinal.Models;

public class WeekDayItem
{
    public DateTime DayDate { get; set; }
    public string DayName { get; set; } = string.Empty;
    public string DayNumber { get; set; } = string.Empty;
    public bool IsToday { get; set; }
    public ObservableCollection<CalendarEvent> Events { get; set; } = new();
    public ObservableCollection<FriendEventDisplayItem> FriendEvents { get; set; } = new();

    public WeekDayItem(DateTime date)
    {
        DayDate = date.Date;
        IsToday = date.Date == DateTime.Today;

        try
        {
            var currentLang = LocalizationService.Instance.CurrentLanguage;
            var culture = currentLang == "English"
                ? new CultureInfo("en-US")
                : new CultureInfo("ru-RU");
            DayName = date.ToString("ddd", culture);
        }
        catch
        {
            DayName = date.ToString("ddd");
        }

        DayNumber = date.Day.ToString();
    }
}
