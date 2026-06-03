using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class EventDialogViewModel : ObservableObject
{
    private readonly int _originalId;
    private readonly int? _originalTaskId;
    private readonly DateTime _selectedDate;
    private readonly DateTime _originalSeriesStartDate; // Храним дату старта самой серии
    private readonly ITemplateService _templateService;

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private bool _isAllDay;
    [ObservableProperty] private int _startHour;
    [ObservableProperty] private int _startMinute;
    [ObservableProperty] private int _endHour;
    [ObservableProperty] private int _endMinute;
    [ObservableProperty] private string _color = "#3498db";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWeeklyFieldsVisible))]
    [NotifyPropertyChangedFor(nameof(IsShiftFieldsVisible))]
    [NotifyPropertyChangedFor(nameof(IsCustomFieldsVisible))]
    private int _recurrenceIndex;

    public bool IsWeeklyFieldsVisible => RecurrenceIndex == 3;
    public bool IsShiftFieldsVisible => RecurrenceIndex == 6;
    public bool IsCustomFieldsVisible => RecurrenceIndex == 7;

    public bool IsEditMode => _originalId != 0;
    public bool IsDeleted { get; private set; }

    [ObservableProperty] private bool _dayMon;
    [ObservableProperty] private bool _dayTue;
    [ObservableProperty] private bool _dayWed;
    [ObservableProperty] private bool _dayThu;
    [ObservableProperty] private bool _dayFri;
    [ObservableProperty] private bool _daySat;
    [ObservableProperty] private bool _daySun;

    [ObservableProperty] private int _workingDays = 2;
    [ObservableProperty] private int _offDays = 2;
    [ObservableProperty] private DateTimeOffset? _cycleStartDate = DateTimeOffset.Now;

    [ObservableProperty] private int _intervalValue = 1;
    [ObservableProperty] private int _intervalUnitIndex = 0;

    // Свойства сохранения и вывода шаблонов событий
    [ObservableProperty] private bool _saveAsTemplate;
    [ObservableProperty] private string _templateName = string.Empty;
    [ObservableProperty] private ObservableCollection<EventTemplate> _eventTemplates = new();
    [ObservableProperty] private EventTemplate? _selectedEventTemplate;

    [ObservableProperty] private CalendarEvent? _resultEvent;

    public string SelectedDeleteMode { get; private set; } = "Cancel";

    // ИСПРАВЛЕНИЕ: Явно указан аргумент типа <DateTime>
    public List<DateTime> DatesToRemove { get; private set; } = new List<DateTime>();

    public EventDialogViewModel(CalendarEvent evt, DateTime selectedDate, ITemplateService? templateService = null)
    {
        _originalId = evt.Id;
        _originalTaskId = evt.TaskId;
        _selectedDate = selectedDate;

        var services = ((App)Avalonia.Application.Current!).Services!;
        var db = services.GetRequiredService<IDatabaseService>();
        _templateService = templateService ?? services.GetRequiredService<ITemplateService>();

        // Извлекаем настоящую дату старта серии из базы, чтобы не брать виртуальную
        var dbEvent = _originalId != 0 ? db.GetEventById(_originalId) : null;
        _originalSeriesStartDate = dbEvent != null ? dbEvent.Start : evt.Start;

        _title = evt.Title;
        _isAllDay = evt.IsAllDay;
        _startHour = evt.Start.Hour;
        _startMinute = evt.Start.Minute;
        _endHour = evt.End.Hour;
        _endMinute = evt.End.Minute;
        _color = evt.Color ?? "#3498db";
        _recurrenceIndex = (int)evt.Recurrence;

        if (evt.DaysOfWeek != null)
        {
            DayMon = evt.DaysOfWeek.Contains(DayOfWeek.Monday);
            DayTue = evt.DaysOfWeek.Contains(DayOfWeek.Tuesday);
            DayWed = evt.DaysOfWeek.Contains(DayOfWeek.Wednesday);
            DayThu = evt.DaysOfWeek.Contains(DayOfWeek.Thursday);
            DayFri = evt.DaysOfWeek.Contains(DayOfWeek.Friday);
            DaySat = evt.DaysOfWeek.Contains(DayOfWeek.Saturday);
            DaySun = evt.DaysOfWeek.Contains(DayOfWeek.Sunday);
        }

        if (evt.WorkingDays.HasValue) WorkingDays = evt.WorkingDays.Value;
        if (evt.OffDays.HasValue) OffDays = evt.OffDays.Value;

        CycleStartDate = new DateTimeOffset(evt.CycleStartDate ?? _selectedDate.Date);

        if (evt.IntervalValue.HasValue) IntervalValue = evt.IntervalValue.Value;
        if (evt.IntervalUnit.HasValue) IntervalUnitIndex = (int)evt.IntervalUnit.Value;

        LoadEventTemplates();
    }

    private void LoadEventTemplates()
    {
        EventTemplates.Clear();
        var templates = _templateService.GetEventTemplates();
        foreach (var t in templates)
            EventTemplates.Add(t);
    }

    partial void OnSelectedEventTemplateChanged(EventTemplate? value)
    {
        if (value != null)
        {
            Title = value.Title;
            IsAllDay = value.IsAllDay;
            StartHour = value.StartHour;
            StartMinute = value.StartMinute;
            EndHour = value.EndHour;
            EndMinute = value.EndMinute;
            Color = value.Color ?? "#3498db";
            RecurrenceIndex = (int)value.Recurrence;

            if (value.DaysOfWeek != null)
            {
                DayMon = value.DaysOfWeek.Contains(DayOfWeek.Monday);
                DayTue = value.DaysOfWeek.Contains(DayOfWeek.Tuesday);
                DayWed = value.DaysOfWeek.Contains(DayOfWeek.Wednesday);
                DayThu = value.DaysOfWeek.Contains(DayOfWeek.Thursday);
                DayFri = value.DaysOfWeek.Contains(DayOfWeek.Friday);
                DaySat = value.DaysOfWeek.Contains(DayOfWeek.Saturday);
                DaySun = value.DaysOfWeek.Contains(DayOfWeek.Sunday);
            }

            if (value.WorkingDays.HasValue) WorkingDays = value.WorkingDays.Value;
            if (value.OffDays.HasValue) OffDays = value.OffDays.Value;
            if (value.IntervalValue.HasValue) IntervalValue = value.IntervalValue.Value;
            if (value.IntervalUnit.HasValue) IntervalUnitIndex = (int)value.IntervalUnit.Value;
        }
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (RecurrenceIndex == 0)
        {
            SelectedDeleteMode = "DeleteAll";
            IsDeleted = true;
            CloseWindow(true);
            return;
        }

        var desktop = App.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var currentWindow = desktop?.Windows.FirstOrDefault(w => w.DataContext == this);
        if (currentWindow == null) return;

        var modeWindow = new DeleteModeWindow();
        await modeWindow.ShowDialog(currentWindow);

        if (modeWindow.DeleteResult == "Cancel") return;

        if (modeWindow.DeleteResult == "All")
        {
            SelectedDeleteMode = "DeleteAll";
            IsDeleted = true;
            CloseWindow(true);
        }
        else if (modeWindow.DeleteResult == "OnlyThis")
        {
            SelectedDeleteMode = "DeleteOnlyThis";
            DatesToRemove.Add(_selectedDate.Date);
            IsDeleted = true;
            CloseWindow(true);
        }
        else if (modeWindow.DeleteResult == "Custom")
        {
            // ИСПРАВЛЕНИЕ: Явно указан аргумент типа <DateTime>
            var upcomingDates = new List<DateTime>();
            DateTime startPoint = _selectedDate.Date;

            for (int i = 0; i < 35; i++)
            {
                DateTime targetDate = startPoint.AddDays(i);
                bool match = RecurrenceIndex switch
                {
                    1 => true,
                    2 => targetDate.DayOfWeek != DayOfWeek.Saturday && targetDate.DayOfWeek != DayOfWeek.Sunday,
                    3 => (targetDate.DayOfWeek == DayOfWeek.Monday && DayMon) ||
                         (targetDate.DayOfWeek == DayOfWeek.Tuesday && DayTue) ||
                         (targetDate.DayOfWeek == DayOfWeek.Wednesday && DayWed) ||
                         (targetDate.DayOfWeek == DayOfWeek.Thursday && DayThu) ||
                         (targetDate.DayOfWeek == DayOfWeek.Friday && DayFri) ||
                         (targetDate.DayOfWeek == DayOfWeek.Saturday && DaySat) ||
                         (targetDate.DayOfWeek == DayOfWeek.Sunday && DaySun),
                    _ => targetDate.DayOfWeek == _selectedDate.DayOfWeek
                };

                if (match) upcomingDates.Add(targetDate);
            }

            var customDaysWindow = new DeleteCustomDaysWindow(upcomingDates);
            await customDaysWindow.ShowDialog(currentWindow);

            if (customDaysWindow.SelectedDates != null && customDaysWindow.SelectedDates.Count > 0)
            {
                SelectedDeleteMode = "DeleteCustom";
                DatesToRemove = customDaysWindow.SelectedDates;
                IsDeleted = true;
                CloseWindow(true);
            }
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Title))
            return;

        DateTime start;
        DateTime end;

        // Фиксируем дату на дне создания серии, чтобы сохранение не сдвигало начало цепочки повторений
        DateTime baseDate = (RecurrenceIndex != 0 && _originalId != 0) ? _originalSeriesStartDate.Date : _selectedDate.Date;

        if (IsAllDay)
        {
            start = baseDate;
            end = baseDate.AddDays(1).AddSeconds(-1);
        }
        else
        {
            start = baseDate.AddHours(StartHour).AddMinutes(StartMinute);
            end = baseDate.AddHours(EndHour).AddMinutes(EndMinute);
            if (end <= start)
                return;
        }

        ResultEvent = new CalendarEvent
        {
            Id = _originalId,
            Title = Title.Trim(),
            Start = start,
            End = end,
            Color = Color,
            TaskId = _originalTaskId,
            IsAllDay = IsAllDay,
            Recurrence = (RecurrenceType)RecurrenceIndex,
            DaysOfWeek = new List<DayOfWeek>()
        };

        // БЛОК ИСПРАВЛЕН: Теперь эти строки находятся строго внутри метода Save()
        if (DayMon) ResultEvent.DaysOfWeek.Add(DayOfWeek.Monday);
        if (DayTue) ResultEvent.DaysOfWeek.Add(DayOfWeek.Tuesday);
        if (DayWed) ResultEvent.DaysOfWeek.Add(DayOfWeek.Wednesday);
        if (DayThu) ResultEvent.DaysOfWeek.Add(DayOfWeek.Thursday);
        if (DayFri) ResultEvent.DaysOfWeek.Add(DayOfWeek.Friday);
        if (DaySat) ResultEvent.DaysOfWeek.Add(DayOfWeek.Saturday);
        if (DaySun) ResultEvent.DaysOfWeek.Add(DayOfWeek.Sunday);

        if (RecurrenceIndex == 6)
        {
            ResultEvent.WorkingDays = WorkingDays;
            ResultEvent.OffDays = OffDays;
            ResultEvent.CycleStartDate = CycleStartDate?.DateTime ?? baseDate;
        }

        if (RecurrenceIndex == 7)
        {
            ResultEvent.IntervalValue = IntervalValue;
            ResultEvent.IntervalUnit = (IntervalUnit)IntervalUnitIndex;
        }

        if (SaveAsTemplate && !string.IsNullOrWhiteSpace(TemplateName))
        {
            var eventTemplate = new EventTemplate
            {
                Name = TemplateName.Trim(),
                Title = ResultEvent.Title,
                IsAllDay = ResultEvent.IsAllDay,
                StartHour = StartHour,
                StartMinute = StartMinute,
                EndHour = EndHour,
                EndMinute = EndMinute,
                Color = ResultEvent.Color,
                Recurrence = ResultEvent.Recurrence,
                DaysOfWeek = new List<DayOfWeek>(ResultEvent.DaysOfWeek),
                WorkingDays = ResultEvent.WorkingDays,
                OffDays = ResultEvent.OffDays,
                CycleStartDate = ResultEvent.CycleStartDate,
                IntervalValue = ResultEvent.IntervalValue,
                IntervalUnit = ResultEvent.IntervalUnit,
            };
            _templateService.SaveEventTemplate(eventTemplate);
            LoadEventTemplates();
        }

        CloseWindow(true);
    }

    [RelayCommand]
    private void Cancel() => CloseWindow(false);
    private void CloseWindow(bool result)
    {
        if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }
}