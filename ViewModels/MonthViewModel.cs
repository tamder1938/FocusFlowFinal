using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public partial class MonthViewModel : ObservableObject
{
    private readonly IDatabaseService _db;

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private DateTime _currentMonthDate;
    [ObservableProperty] private string _monthYearTitle = string.Empty;
    [ObservableProperty] private ObservableCollection<MonthDayItem> _monthDays = new();

    [ObservableProperty] private ObservableCollection<string> _dayNamesHeaders = new();

    public event Action<DateTime>? DaySelected;

    public MonthViewModel(IDatabaseService db)
    {
        _db = db;
        Loc.PropertyChanged += (s, e) => UpdateLocalization();
        GoToMonth(DateTime.Today);
    }

    public void GoToMonth(DateTime date)
    {
        CurrentMonthDate = new DateTime(date.Year, date.Month, 1);
        UpdateLocalization();
    }

    private void UpdateLocalization()
    {
        var culture = Loc.CurrentLanguage == "English" ? new CultureInfo("en-US") : new CultureInfo("ru-RU");
        MonthYearTitle = CurrentMonthDate.ToString("MMMM yyyy", culture);

        DayNamesHeaders.Clear();
        var dateTimeFormat = culture.DateTimeFormat;
        // Порядок дней: понедельник – воскресенье
        DayNamesHeaders.Add(dateTimeFormat.GetShortestDayName(DayOfWeek.Monday));
        DayNamesHeaders.Add(dateTimeFormat.GetShortestDayName(DayOfWeek.Tuesday));
        DayNamesHeaders.Add(dateTimeFormat.GetShortestDayName(DayOfWeek.Wednesday));
        DayNamesHeaders.Add(dateTimeFormat.GetShortestDayName(DayOfWeek.Thursday));
        DayNamesHeaders.Add(dateTimeFormat.GetShortestDayName(DayOfWeek.Friday));
        DayNamesHeaders.Add(dateTimeFormat.GetShortestDayName(DayOfWeek.Saturday));
        DayNamesHeaders.Add(dateTimeFormat.GetShortestDayName(DayOfWeek.Sunday));

        RefreshMonth();
    }

    public void RefreshMonth()
    {
        MonthDays.Clear();

        int daysInMonth = DateTime.DaysInMonth(CurrentMonthDate.Year, CurrentMonthDate.Month);

        for (int day = 1; day <= daysInMonth; day++)
        {
            DateTime currentDay = new DateTime(CurrentMonthDate.Year, CurrentMonthDate.Month, day);
            var displayEvents = _db.GetEventsForDisplay(currentDay).ToList();

            var monthDay = new MonthDayItem
            {
                Date = currentDay,
                IsCurrentMonth = true, // Все дни текущего месяца
                DayNumber = currentDay.Day.ToString(),
                Events = new ObservableCollection<CalendarEvent>(displayEvents)
            };

            MonthDays.Add(monthDay);
        }
    }

    [RelayCommand] private void SelectDay(DateTime date) => DaySelected?.Invoke(date);
    [RelayCommand] private void NextMonth() => GoToMonth(CurrentMonthDate.AddMonths(1));
    [RelayCommand] private void PreviousMonth() => GoToMonth(CurrentMonthDate.AddMonths(-1));
}