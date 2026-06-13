using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public partial class YearMonthItem : ObservableObject
{
    [ObservableProperty] private string _monthName = string.Empty;
    [ObservableProperty] private ObservableCollection<YearDayItem> _days = new();

    /// <summary>true, если это текущий календарный месяц (для подсветки в YearView).</summary>
    [ObservableProperty] private bool _isCurrentMonth;

    /// <summary>Кол-во выполненных задач в этом месяце.</summary>
    [ObservableProperty] private int _completedTasksCount;

    /// <summary>Общее кол-во задач с дедлайном в этом месяце.</summary>
    [ObservableProperty] private int _totalTasksCount;

    /// <summary>Текст вида "Выполнено: 2/7" для текущего месяца.</summary>
    public string ProgressText => $"{LocalizationService.Instance["Done"]}: {CompletedTasksCount}/{TotalTasksCount}";

    /// <summary>Доля выполненных задач (0-100) для прогресс-бара.</summary>
    public double ProgressPercent => TotalTasksCount > 0
        ? (double)CompletedTasksCount / TotalTasksCount * 100
        : 0;

    /// <summary>Показывать блок прогресса только если есть хотя бы одна задача.</summary>
    public bool HasProgress => TotalTasksCount > 0;
}

public partial class YearDayItem : ObservableObject
{
    [ObservableProperty] private DateTime _date;
    [ObservableProperty] private bool _isCurrentMonth;
    [ObservableProperty] private bool _hasTasks;

    public int DayNumber => Date.Day;
    public bool IsToday => Date.Date == DateTime.Today;
}

public partial class YearViewModel : ObservableObject
{
    private readonly IDatabaseService _db;

    [ObservableProperty] private int _currentYear;
    [ObservableProperty] private string _yearTitle = string.Empty;
    [ObservableProperty] private ObservableCollection<YearMonthItem> _months = new();

    public event Action<DateTime>? DaySelected;

    public LocalizationService Loc => LocalizationService.Instance;

    public string MonShort => Loc["MonShort"];
    public string TueShort => Loc["TueShort"];
    public string WedShort => Loc["WedShort"];
    public string ThuShort => Loc["ThuShort"];
    public string FriShort => Loc["FriShort"];
    public string SatShort => Loc["SatShort"];
    public string SunShort => Loc["SunShort"];

    public YearViewModel(IDatabaseService db)
    {
        _db = db;
        LocalizationService.Instance.PropertyChanged += (s, e) => RefreshYear();
        GoToYear(DateTime.Today.Year);
    }

    private void RefreshYear() => GoToYear(CurrentYear);

    public void GoToYear(int year)
    {
        CurrentYear = year;
        YearTitle = $"{year} {LocalizationService.Instance["YearWord"]}";
        Reload();
    }

    [RelayCommand] private void PreviousYear() => GoToYear(CurrentYear - 1);
    [RelayCommand] private void NextYear() => GoToYear(CurrentYear + 1);

    [RelayCommand]
    private void SelectDay(YearDayItem? item)
    {
        if (item != null && item.IsCurrentMonth)
            DaySelected?.Invoke(item.Date);
    }

    private void Reload()
    {
        Months.Clear();

        var allTasksThisYear = _db.GetAllTasks()
            .Where(t => t.DueDate.HasValue && t.DueDate.Value.Year == CurrentYear)
            .ToList();

        var tasksByDate = allTasksThisYear
            .Where(t => t.DueDate.HasValue)
            .Select(t => t.DueDate!.Value.Date)
            .ToHashSet();

        string[] monthKeys = { "January", "February", "March", "April", "May", "June",
                               "July", "August", "September", "October", "November", "December" };

        var today = DateTime.Today;

        for (int m = 0; m < 12; m++)
        {
            int monthNumber = m + 1;
            var firstDayOfMonth = new DateTime(CurrentYear, monthNumber, 1);
            string monthName = LocalizationService.Instance[monthKeys[m]];

            var monthItem = new YearMonthItem
            {
                MonthName       = monthName,
                IsCurrentMonth  = (CurrentYear == today.Year && monthNumber == today.Month)
            };

            // Прогресс выполнения задач за месяц
            var monthTasks = allTasksThisYear
                .Where(t => t.DueDate!.Value.Month == monthNumber)
                .ToList();
            monthItem.TotalTasksCount     = monthTasks.Count;
            monthItem.CompletedTasksCount = monthTasks.Count(t => t.IsCompleted);

            int diff = (7 + (firstDayOfMonth.DayOfWeek - DayOfWeek.Monday)) % 7;
            var gridStart = firstDayOfMonth.AddDays(-diff);

            for (int i = 0; i < 42; i++)
            {
                var date = gridStart.AddDays(i);

                var dayEvents = _db.GetEventsForDisplay(date.Date);
                bool dayHasActivity = tasksByDate.Contains(date.Date) || dayEvents.Any();

                var dayItem = new YearDayItem
                {
                    Date = date,
                    IsCurrentMonth = date.Month == monthNumber && date.Year == CurrentYear,
                    HasTasks = dayHasActivity
                };
                monthItem.Days.Add(dayItem);
            }

            Months.Add(monthItem);
        }
    }
}
