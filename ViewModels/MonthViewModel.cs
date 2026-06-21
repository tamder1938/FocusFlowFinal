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
    private readonly IDatabaseService  _db;
    private readonly INoteRepository?  _notes;

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private DateTime _currentMonthDate;
    [ObservableProperty] private string _monthYearTitle = string.Empty;
    [ObservableProperty] private ObservableCollection<MonthDayItem> _monthDays = new();
    [ObservableProperty] private ObservableCollection<string> _dayNamesHeaders = new();

    /// <summary>Пользователь кликнул на число месяца — переход на DayView.</summary>
    public event Action<DateTime>? DaySelected;

    public MonthViewModel(IDatabaseService db, INoteRepository notes)
    {
        _db    = db;
        _notes = notes;
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

    public void RefreshMonth() => _ = RefreshMonthAsync();

    public async Task RefreshMonthAsync()
    {
        var firstDayOfMonth = new DateTime(CurrentMonthDate.Year, CurrentMonthDate.Month, 1);
        int offset          = (int)(firstDayOfMonth.DayOfWeek - DayOfWeek.Monday + 7) % 7;
        DateTime startDate  = firstDayOfMonth.AddDays(-offset);
        int currentMonth    = CurrentMonthDate.Month;
        int currentYear     = CurrentMonthDate.Year;
        DateTime endDate    = startDate.AddDays(41);

        var items = await Task.Run(() =>
        {
            HashSet<DateTime> noteDates = _notes?.GetDatesWithNotes(startDate, endDate) ?? new();

            var list = new List<MonthDayItem>();
            for (int i = 0; i < 42; i++)
            {
                DateTime day = startDate.AddDays(i);
                var displayEvents = _db.GetEventsForDisplay(day).ToList();
                list.Add(new MonthDayItem
                {
                    Date           = day,
                    IsCurrentMonth = day.Month == currentMonth && day.Year == currentYear,
                    DayNumber      = day.Day.ToString(),
                    Events         = new ObservableCollection<CalendarEvent>(displayEvents),
                    HasNotes       = noteDates.Contains(day.Date),
                });
            }
            return list;
        });

        MonthDays.Clear();
        foreach (var item in items)
            MonthDays.Add(item);
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
            var mode    = dialogVm.SelectedDeleteMode;
            var dates   = dialogVm.DatesToRemove;
            var eventId = original.Id;
            await Task.Run(() =>
            {
                if (mode == "DeleteAll")
                    _db.DeleteEvent(eventId);
                else if (mode == "DeleteOnlyThis")
                    _db.ExcludeDateFromEvent(eventId, contextDate);
                else if (mode == "DeleteCustom" && dates.Count > 0)
                    _db.ExcludeDatesFromEvent(eventId, dates);
            });
            await RefreshMonthAsync();
        }
        else if (result && dialogVm.ResultEvent != null)
        {
            var updated = dialogVm.ResultEvent;
            await Task.Run(() => _db.UpsertEvent(updated));
            await RefreshMonthAsync();
        }
    }
}
