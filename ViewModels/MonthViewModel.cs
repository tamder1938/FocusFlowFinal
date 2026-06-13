using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class MonthViewModel : ObservableObject
{
    private readonly IDatabaseService _db;

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private DateTime _currentMonthDate;
    [ObservableProperty] private string _monthYearTitle = string.Empty;
    [ObservableProperty] private ObservableCollection<MonthDayItem> _monthDays = new();
    [ObservableProperty] private ObservableCollection<string> _dayNamesHeaders = new();

    /// <summary>Пользователь кликнул на число месяца — переход на DayView.</summary>
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
        var culture = Loc.CurrentLanguage == "English"
            ? new CultureInfo("en-US")
            : new CultureInfo("ru-RU");

        MonthYearTitle = CurrentMonthDate.ToString("MMMM yyyy", culture);

        DayNamesHeaders.Clear();
        var fmt = culture.DateTimeFormat;
        DayNamesHeaders.Add(fmt.GetShortestDayName(DayOfWeek.Monday));
        DayNamesHeaders.Add(fmt.GetShortestDayName(DayOfWeek.Tuesday));
        DayNamesHeaders.Add(fmt.GetShortestDayName(DayOfWeek.Wednesday));
        DayNamesHeaders.Add(fmt.GetShortestDayName(DayOfWeek.Thursday));
        DayNamesHeaders.Add(fmt.GetShortestDayName(DayOfWeek.Friday));
        DayNamesHeaders.Add(fmt.GetShortestDayName(DayOfWeek.Saturday));
        DayNamesHeaders.Add(fmt.GetShortestDayName(DayOfWeek.Sunday));

        RefreshMonth();
    }

    public void RefreshMonth()
    {
        MonthDays.Clear();

        var firstDayOfMonth = new DateTime(CurrentMonthDate.Year, CurrentMonthDate.Month, 1);

        // ИСПРАВЛЕНО (Проблема 3): вычисляем смещение от Понедельника до первого дня месяца.
        // Например, для июля 2026 (1 июля — среда) offset = 2, и сетка начнётся с 29 июня (Пн).
        int offset = (int)(firstDayOfMonth.DayOfWeek - DayOfWeek.Monday + 7) % 7;
        DateTime startDate = firstDayOfMonth.AddDays(-offset);

        // Всегда генерируем 6 недель (42 ячейки), как в стандартной сетке месяца
        for (int i = 0; i < 42; i++)
        {
            DateTime currentDay = startDate.AddDays(i);
            var displayEvents = _db.GetEventsForDisplay(currentDay).ToList();

            MonthDays.Add(new MonthDayItem
            {
                Date           = currentDay,
                IsCurrentMonth = currentDay.Month == CurrentMonthDate.Month && currentDay.Year == CurrentMonthDate.Year,
                DayNumber      = currentDay.Day.ToString(),
                Events         = new ObservableCollection<CalendarEvent>(displayEvents)
            });
        }
    }

    // ── Навигация ───────────────────────────────────────────────────
    [RelayCommand] private void SelectDay(DateTime date) => DaySelected?.Invoke(date);
    [RelayCommand] private void NextMonth()     => GoToMonth(CurrentMonthDate.AddMonths(1));
    [RelayCommand] private void PreviousMonth() => GoToMonth(CurrentMonthDate.AddMonths(-1));

    // ── Редактирование события при клике на него в MonthView ────────
    [RelayCommand]
    public async Task EditEvent(CalendarEvent ev)
    {
        if (ev == null) return;

        CalendarEvent? original = null;
        if (ev.Id != 0 && ev.Recurrence != RecurrenceType.None)
            original = _db.GetEventById(ev.Id);

        if (original == null && ev.Recurrence != RecurrenceType.None)
            original = _db.FindOriginalSeries(ev);

        if (original == null) original = ev;

        var contextDate = ev.Start.Date;
        var dialogVm    = new EventDialogViewModel(original, contextDate);
        var dialog      = new EventDialog { DataContext = dialogVm };
        var desktop     = App.Current?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;

        var result = await dialog.ShowDialog<bool>(desktop?.MainWindow);

        if (result && dialogVm.IsDeleted)
        {
            if (dialogVm.SelectedDeleteMode == "DeleteAll")
                _db.DeleteEvent(original.Id);
            else if (dialogVm.SelectedDeleteMode == "DeleteOnlyThis")
                _db.ExcludeDateFromEvent(original.Id, contextDate);
            else if (dialogVm.SelectedDeleteMode == "DeleteCustom")
                foreach (var d in dialogVm.DatesToRemove)
                    _db.ExcludeDateFromEvent(original.Id, d);
            RefreshMonth();
        }
        else if (result && dialogVm.ResultEvent != null)
        {
            _db.UpsertEvent(dialogVm.ResultEvent);
            RefreshMonth();
        }
    }
}
