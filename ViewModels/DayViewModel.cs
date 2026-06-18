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

public partial class DayViewModel : ObservableObject
{
    private readonly IDatabaseService _db;

    [ObservableProperty] private DateTime _selectedDate;
    [ObservableProperty] private ObservableCollection<CalendarEvent> _events = new();

    // Задачи без времени начала — показываются как чипы вверху
    [ObservableProperty] private ObservableCollection<TaskItem> _dayTasks = new();
    [ObservableProperty] private ObservableCollection<EventDisplayItem> _eventDisplayItems = new();

    public LocalizationService Loc => LocalizationService.Instance;

    public string SelectedDateFormatted =>
        SelectedDate.ToString("dd MMMM yyyy", new CultureInfo(Loc.CurrentLanguage == "English" ? "en-US" : "ru-RU"));

    public List<string> HourStrings { get; } = Enumerable.Range(0, 24).Select(h => $"{h:00}:00").ToList();

    public DayViewModel(IDatabaseService db)
    {
        _db = db;
        LoadEvents();
        LoadTasks();
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        LoadEvents();
        LoadTasks();
        OnPropertyChanged(nameof(SelectedDateFormatted));
    }

    public void LoadEvents()
    {
        // GetEventsForDisplay включает и обычные события, и задачи с StartTime
        var events = _db.GetEventsForDisplay(SelectedDate);
        Events.Clear();
        foreach (var ev in events)
            Events.Add(ev);
        UpdateEventDisplayItems();
    }

    public void Refresh()
    {
        LoadEvents();
        LoadTasks();
    }

    private void LoadTasks()
    {
        var tasks = _db.GetTasksByDate(SelectedDate);
        DayTasks.Clear();
        // Чипами отображаем только незавершённые задачи без привязки ко времени.
        // Задачи с StartTime уже попадают в timeline через GetEventsForDisplay.
        foreach (var t in tasks.Where(t => !t.IsCompleted && !t.StartTime.HasValue))
            DayTasks.Add(t);
    }

    private const double LeftMargin = 4;
    private const double ColumnGap  = 4;
    private double _canvasWidth = 560;

    public void UpdateCanvasWidth(double width)
    {
        if (width > 0 && Math.Abs(width - _canvasWidth) > 1)
        {
            _canvasWidth = width;
            UpdateEventDisplayItems();
        }
    }

    private void UpdateEventDisplayItems()
    {
        EventDisplayItems.Clear();
        if (Events.Count == 0) return;

        var layoutItems = new List<EventLayoutItem>();

        foreach (var ev in Events)
        {
            var startMinutes = ev.Start.Hour * 60 + ev.Start.Minute;
            var endMinutes   = ev.End.Hour   * 60 + ev.End.Minute;
            if (endMinutes <= startMinutes) endMinutes = startMinutes + 30;

            var baseDate = DateTime.Today;
            layoutItems.Add(new EventLayoutItem
            {
                Tag   = ev,
                Start = baseDate.AddMinutes(startMinutes),
                End   = baseDate.AddMinutes(endMinutes)
            });
        }

        EventLayoutCalculator.CalculateLayout(layoutItems, _canvasWidth, LeftMargin, ColumnGap);

        foreach (var item in layoutItems)
        {
            var ev = (CalendarEvent)item.Tag!;

            var startMinutes = ev.Start.Hour * 60 + ev.Start.Minute;
            var endMinutes   = ev.End.Hour   * 60 + ev.End.Minute;
            if (endMinutes <= startMinutes) endMinutes = startMinutes + 30;

            EventDisplayItems.Add(new EventDisplayItem
            {
                EventId       = ev.Id,
                Title         = ev.Title,
                Color         = ev.Color,
                Top           = startMinutes,
                Height        = Math.Max(endMinutes - startMinutes, 20),
                Left          = item.Left,
                Width         = item.Width,
                TimeLabel     = $"{ev.Start:HH:mm}–{ev.End:HH:mm}",
                OriginalEvent = ev
            });
        }
    }

    [RelayCommand]
    private void PreviousDay() => SelectedDate = SelectedDate.AddDays(-1);

    [RelayCommand]
    private void NextDay() => SelectedDate = SelectedDate.AddDays(1);

    [RelayCommand]
    private async Task AddEvent()
    {
        var newEvent = new CalendarEvent
        {
            Start = SelectedDate,
            End = SelectedDate.AddHours(1),
            Recurrence = RecurrenceType.None
        };
        var dialogVm = new EventDialogViewModel(newEvent, SelectedDate);
        var dialog = new EventDialog { DataContext = dialogVm };
        var desktop = App.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        await dialog.ShowDialog<bool>(desktop?.MainWindow);
        if (dialogVm.ResultEvent != null)
        {
            _db.UpsertEvent(dialogVm.ResultEvent);
            LoadEvents();
        }
    }

    [RelayCommand]
    private async Task EditTaskFromCalendar(TaskItem task)
    {
        if (task == null) return;
        var dialogVm = new TaskDialogViewModel(task);
        var dialog = new TaskDialog { DataContext = dialogVm };
        var desktop = App.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var result = await dialog.ShowDialog<bool?>(desktop?.MainWindow);
        if (result == true)
        {
            _db.UpsertTask(task);
            LoadTasks();
            LoadEvents();
        }
    }

    [RelayCommand]
    public async Task EditEvent(CalendarEvent ev)
    {
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

        CalendarEvent? originalEvent = null;

        if (ev.Id != 0 && ev.Recurrence != RecurrenceType.None)
            originalEvent = _db.GetEventById(ev.Id);

        if (originalEvent == null && ev.Recurrence != RecurrenceType.None)
            originalEvent = _db.FindOriginalSeries(ev);

        if (originalEvent == null)
            originalEvent = ev;

        var dialogVm = new EventDialogViewModel(originalEvent, SelectedDate);
        var dialog = new EventDialog { DataContext = dialogVm };
        var desktop = App.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var result = await dialog.ShowDialog<bool>(desktop?.MainWindow);

        if (result && dialogVm.IsDeleted)
        {
            var mode    = dialogVm.SelectedDeleteMode;
            var dates   = dialogVm.DatesToRemove;
            var eventId = originalEvent.Id;
            var anchor  = SelectedDate;
            await Task.Run(() =>
            {
                if (mode == "DeleteAll")
                    _db.DeleteEvent(eventId);
                else if (mode == "DeleteOnlyThis")
                    _db.ExcludeDateFromEvent(eventId, anchor);
                else if (mode == "DeleteCustom" && dates.Count > 0)
                    _db.ExcludeDatesFromEvent(eventId, dates);
            });
            LoadEvents();
        }
        else if (result && dialogVm.ResultEvent != null)
        {
            var updated = dialogVm.ResultEvent;
            await Task.Run(() => _db.UpsertEvent(updated));
            LoadEvents();
        }
    }
}
