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
    [ObservableProperty] private ObservableCollection<TaskItem> _dayTasks = new();
    [ObservableProperty] private ObservableCollection<EventDisplayItem> _eventDisplayItems = new();

    // Локализация
    public LocalizationService Loc => LocalizationService.Instance;

    // Форматированная дата
    public string SelectedDateFormatted =>
        SelectedDate.ToString("dd MMMM yyyy", new CultureInfo(Loc.CurrentLanguage == "English" ? "en-US" : "ru-RU"));

    // Строки часов для сетки дня
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
        var events = _db.GetEvents(SelectedDate);
        Events.Clear();
        foreach (var ev in events)
            Events.Add(ev);
        UpdateEventDisplayItems();
    }

    private void LoadTasks()
    {
        var tasks = _db.GetTasksByDate(SelectedDate);
        DayTasks.Clear();
        foreach (var t in tasks)
            DayTasks.Add(t);
    }

    private void UpdateEventDisplayItems()
    {
        EventDisplayItems.Clear();
        // Предполагается, что Canvas имеет ширину около 400px, левый отступ 4px, ширина события - 250px
        double canvasWidth = 400;
        double defaultWidth = 250;
        double leftMargin = 4;
        double rightMargin = canvasWidth - leftMargin - defaultWidth;

        foreach (var ev in Events)
        {
            var startMinutes = ev.Start.Hour * 60 + ev.Start.Minute;
            var endMinutes = ev.End.Hour * 60 + ev.End.Minute;
            if (endMinutes <= startMinutes) endMinutes = startMinutes + 30; // fallback

            double top = startMinutes;
            double height = endMinutes - startMinutes;

            var displayItem = new EventDisplayItem
            {
                EventId = ev.Id,
                Title = ev.Title,
                Color = ev.Color,
                Top = top,
                Height = height,
                Left = leftMargin,
                Width = defaultWidth,
                OriginalEvent = ev
            };
            EventDisplayItems.Add(displayItem);
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
            // Обновить связанные события, если нужно
            LoadEvents();
        }
    }

    [RelayCommand]
    public async Task EditEvent(CalendarEvent ev)
    {
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
            if (dialogVm.SelectedDeleteMode == "DeleteAll")
                _db.DeleteEvent(originalEvent.Id);
            else if (dialogVm.SelectedDeleteMode == "DeleteOnlyThis")
                _db.ExcludeDateFromEvent(originalEvent.Id, SelectedDate);
            else if (dialogVm.SelectedDeleteMode == "DeleteCustom" && dialogVm.DatesToRemove.Count > 0)
                foreach (var date in dialogVm.DatesToRemove)
                    _db.ExcludeDateFromEvent(originalEvent.Id, date);
            LoadEvents();
        }
        else if (result && dialogVm.ResultEvent != null)
        {
            _db.UpsertEvent(dialogVm.ResultEvent);
            LoadEvents();
        }
    }
}