using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class WeekViewModel : ObservableObject
{
    private readonly IDatabaseService _db;

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private DateTime _weekStart;
    [ObservableProperty] private string _weekRangeTitle = string.Empty;
    [ObservableProperty] private ObservableCollection<WeekDayItem> _weekDaysCustomCollection = new();
    [ObservableProperty] private ObservableCollection<string> _hourStrings = new();

    [ObservableProperty] private bool _isWeekReady   = false;
    [ObservableProperty] private bool _isWeekLoading = true;

    public event Action<DateTime>? DaySelected;

    public WeekViewModel(IDatabaseService db)
    {
        _db = db;

        for (int i = 0; i < 24; i++)
            HourStrings.Add($"{i:D2}:00");

        Loc.PropertyChanged += (s, e) => RefreshWeek();
        GoToWeek(DateTime.Today);
    }

    public void GoToWeek(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        WeekStart = date.AddDays(-diff).Date;

        DateTime weekEnd = WeekStart.AddDays(6);
        WeekRangeTitle = $"{WeekStart:dd.MM.yyyy} — {weekEnd:dd.MM.yyyy}";

        RefreshWeek();
    }

    public void RefreshWeek() => _ = RefreshWeekAsync();

    public async Task RefreshWeekAsync()
    {
        IsWeekReady   = false;
        IsWeekLoading = true;
        try
        {
            var weekStart = WeekStart;
            var newItems  = await Task.Run(() =>
            {
                var list = new List<WeekDayItem>();
                for (int i = 0; i < 7; i++)
                {
                    var targetDay = weekStart.AddDays(i);
                    var dayItem   = new WeekDayItem(targetDay);
                    foreach (var ev in _db.GetEventsForDisplay(targetDay))
                        dayItem.Events.Add(ev);
                    list.Add(dayItem);
                }
                return list;
            });

            WeekDaysCustomCollection.Clear();
            foreach (var item in newItems)
                WeekDaysCustomCollection.Add(item);

            IsWeekReady   = true;
            IsWeekLoading = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WeekViewModel] RefreshWeek error: {ex}");
            IsWeekLoading = false;
        }
    }

    // ── Навигация по неделям ────────────────────────────────────────
    [RelayCommand] private void NextWeek()     => GoToWeek(WeekStart.AddDays(7));
    [RelayCommand] private void PreviousWeek() => GoToWeek(WeekStart.AddDays(-7));

    // ── Клик на заголовок дня → переход на DayView ─────────────────
    [RelayCommand]
    private void SelectDay(DateTime date) => DaySelected?.Invoke(date);

    // ── Редактирование события или задачи по клику ──────────────────
    [RelayCommand]
    public async Task EditEvent(CalendarEvent ev)
    {
        if (ev == null) return;

        // Виртуальное событие из задачи (Id=0, TaskId установлен) → открываем диалог задачи
        if (ev.Id == 0 && ev.TaskId.HasValue)
        {
            var task = _db.GetTask(ev.TaskId.Value);
            if (task != null)
            {
                await EditTaskFromCalendar(task);
                return;
            }
        }

        // Для серийных событий ищем оригинал в БД
        CalendarEvent? original = null;
        if (ev.Id != 0 && ev.Recurrence != RecurrenceType.None)
            original = _db.GetEventById(ev.Id);

        if (original == null && ev.Recurrence != RecurrenceType.None)
            original = _db.FindOriginalSeries(ev);

        if (original == null) original = ev;

        var contextDate = ev.Start.Date;

        var dialogVm = new EventDialogViewModel(original, contextDate);
        var dialog   = new EventDialog { DataContext = dialogVm };
        var desktop  = App.Current?.ApplicationLifetime as
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
            await RefreshWeekAsync();
        }
        else if (result && dialogVm.ResultEvent != null)
        {
            var updated = dialogVm.ResultEvent;
            await Task.Run(() => _db.UpsertEvent(updated));
            await RefreshWeekAsync();
        }
    }

    private async Task EditTaskFromCalendar(TaskItem task)
    {
        var dialogVm = new TaskDialogViewModel(task);
        var dialog   = new TaskDialog { DataContext = dialogVm };
        var desktop  = App.Current?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;

        var result = await dialog.ShowDialog<bool?>(desktop?.MainWindow);
        if (result == true)
        {
            var t = task;
            await Task.Run(() => _db.UpsertTask(t));
            await RefreshWeekAsync();
        }
    }
}
