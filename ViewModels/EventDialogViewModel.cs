using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
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
    private readonly DateTime _originalSeriesStartDate;
    private readonly ITemplateService _templateService;

    public LocalizationService Loc => LocalizationService.Instance;

    // === Предустановленные цвета ===
    public static readonly string[] PresetColors = new[]
    {
        "#EF4444", // Красный
        "#F97316", // Оранжевый
        "#EAB308", // Жёлтый
        "#22C55E", // Зелёный
        "#3B82F6", // Синий
        "#8B5CF6", // Фиолетовый
        "#EC4899", // Розовый
        "#6B7280"  // Серый
    };

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private bool _isAllDay;

    // ИСПРАВЛЕНО (Часть 1, п.2): дата события теперь редактируема через DatePicker
    // в самом диалоге — пользователь может выбрать произвольный день,
    // не закрывая окно и не возвращаясь в месячный календарь.
    [ObservableProperty] private DateTimeOffset _eventDate;
    [ObservableProperty] private int _startHour;
    [ObservableProperty] private int _startMinute;
    [ObservableProperty] private int _endHour;
    [ObservableProperty] private int _endMinute;

    // Цвет события: основное свойство — HEX строка
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPresetSelected))]
    private string _color = "#3498db";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWeeklyFieldsVisible))]
    [NotifyPropertyChangedFor(nameof(IsShiftFieldsVisible))]
    [NotifyPropertyChangedFor(nameof(IsCustomFieldsVisible))]
    [NotifyPropertyChangedFor(nameof(IsMonthlyFieldsVisible))]
    [NotifyPropertyChangedFor(nameof(HasRecurrence))]
    private int _recurrenceIndex;

    public bool IsWeeklyFieldsVisible  => RecurrenceIndex == 3;
    public bool IsShiftFieldsVisible   => RecurrenceIndex == 6;
    public bool IsCustomFieldsVisible  => RecurrenceIndex == 7;
    public bool IsMonthlyFieldsVisible => RecurrenceIndex == 4;

    // Показываем поле даты окончания только если есть повторение
    public bool HasRecurrence => RecurrenceIndex != 0;

    public bool IsEditMode => _originalId != 0;
    public bool IsDeleted { get; private set; }

    // === Выбор дня начала для Monthly ===
    [ObservableProperty] private int _monthlyDay = 1;

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

    // Дата окончания серии повторений
    [ObservableProperty] private DateTimeOffset? _recurrenceEndDate = null;

    // === Гибкое уведомление: количество + единица (минуты/часы/дни/недели) ===
    [ObservableProperty] private int _notificationQuantity = 0;
    [ObservableProperty] private int _notificationUnitIndex = 2; // 0=Мин,1=Часы,2=Дни,3=Недели

    public IReadOnlyList<string> NotificationUnitItems => new[]
    {
        Loc["NotifUnit_Minutes"],
        Loc["NotifUnit_Hours"],
        Loc["NotifUnit_Days"],
        Loc["NotifUnit_Weeks"]
    };

    private static readonly int[] UnitMultipliers = { 1, 60, 60 * 24, 60 * 24 * 7 };

    private static (int qty, int unitIdx) MinutesToQtyUnit(int minutes)
    {
        if (minutes <= 0) return (0, 2);
        if (minutes % (60 * 24 * 7) == 0) return (minutes / (60 * 24 * 7), 3);
        if (minutes % (60 * 24) == 0)     return (minutes / (60 * 24), 2);
        if (minutes % 60 == 0)             return (minutes / 60, 1);
        return (minutes, 0);
    }

    [ObservableProperty] private bool _saveAsTemplate;
    [ObservableProperty] private string _templateName = string.Empty;
    [ObservableProperty] private ObservableCollection<EventTemplate> _eventTemplates = new();
    [ObservableProperty] private EventTemplate? _selectedEventTemplate;
    [ObservableProperty] private CalendarEvent? _resultEvent;

    // ── Место ───────────────────────────────────────────────────────────
    [ObservableProperty] private bool _hasLocation;
    [ObservableProperty] private string _locationText = string.Empty;

    public string SelectedDeleteMode { get; private set; } = "Cancel";
    public List<DateTime> DatesToRemove { get; private set; } = new List<DateTime>();

    /// <summary>
    /// Возвращает true, если данный hex совпадает с текущим выбранным цветом.
    /// Используется в XAML для подсветки активного кружка.
    /// </summary>
    public bool IsPresetSelected => PresetColors.Any(c =>
        string.Equals(c, Color, StringComparison.OrdinalIgnoreCase));

    /// <summary>Команда выбора предустановленного цвета из кружка.</summary>
    [RelayCommand]
    private void SelectColor(string hex)
    {
        Color = hex;
    }

    public EventDialogViewModel(CalendarEvent evt, DateTime selectedDate, ITemplateService? templateService = null)
    {
        _originalId = evt.Id;
        _originalTaskId = evt.TaskId;
        _selectedDate = selectedDate;

        // ИСПРАВЛЕНО (Часть 1, п.2): для нового события — дата клика в календаре,
        // для существующего — дата самого события (evt.Start).
        EventDate = new DateTimeOffset(evt.Id != 0 ? evt.Start.Date : selectedDate.Date);

        var services = ((App)Avalonia.Application.Current!).Services!;
        var db = services.GetRequiredService<IDatabaseService>();
        _templateService = templateService ?? services.GetRequiredService<ITemplateService>();

        var dbEvent = _originalId != 0 ? db.GetEventById(_originalId) : null;
        _originalSeriesStartDate = dbEvent != null ? dbEvent.Start : evt.Start;

        _title        = evt.Title;
        _isAllDay     = evt.IsAllDay;
        _startHour    = evt.Start.Hour;
        _startMinute  = evt.Start.Minute;
        _endHour      = evt.End.Hour;
        _endMinute    = evt.End.Minute;
        _color        = evt.Color ?? "#3498db";
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
        if (evt.OffDays.HasValue)     OffDays     = evt.OffDays.Value;

        CycleStartDate = new DateTimeOffset(evt.CycleStartDate ?? _selectedDate.Date);

        if (evt.IntervalValue.HasValue) IntervalValue    = evt.IntervalValue.Value;
        if (evt.IntervalUnit.HasValue)  IntervalUnitIndex = (int)evt.IntervalUnit.Value;

        // Загружаем дату окончания из модели
        RecurrenceEndDate = evt.RecurrenceEndDate.HasValue
            ? new DateTimeOffset(evt.RecurrenceEndDate.Value)
            : null;

        // Загружаем день начала повторения для Monthly
        _monthlyDay = evt.RecurrenceStartDay ?? evt.Start.Day;

        // Восстанавливаем гибкое уведомление
        var (qty, unitIdx) = MinutesToQtyUnit(evt.NotificationOffsetMinutes);
        _notificationQuantity   = qty;
        _notificationUnitIndex  = unitIdx;

        // Загружаем место
        HasLocation  = evt.Location != null;
        LocationText = evt.Location?.DisplayName ?? string.Empty;

        LoadEventTemplates();
    }

    private void LoadEventTemplates()
    {
        EventTemplates.Clear();
        foreach (var t in _templateService.GetEventTemplates())
            EventTemplates.Add(t);
    }

    partial void OnSelectedEventTemplateChanged(EventTemplate? value)
    {
        if (value == null) return;

        Title          = value.Title;
        IsAllDay       = value.IsAllDay;
        StartHour      = value.StartHour;
        StartMinute    = value.StartMinute;
        EndHour        = value.EndHour;
        EndMinute      = value.EndMinute;
        Color          = value.Color ?? "#3498db";
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

        if (value.WorkingDays.HasValue) WorkingDays    = value.WorkingDays.Value;
        if (value.OffDays.HasValue)     OffDays        = value.OffDays.Value;
        if (value.IntervalValue.HasValue) IntervalValue = value.IntervalValue.Value;
        if (value.IntervalUnit.HasValue)  IntervalUnitIndex = (int)value.IntervalUnit.Value;
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

        var desktop = App.Current?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
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
            int lookAheadDays = RecurrenceIndex switch
            {
                1 => 60,   // ежедневно
                2 => 60,   // рабочие дни
                3 => 180,  // еженедельно
                4 => 730,  // ежемесячно (~24 мес.)
                5 => 3650, // ежегодно (~10 лет)
                6 => 180,  // сменный график
                7 => 730,  // пользовательский интервал
                _ => 60
            };
            if (RecurrenceEndDate.HasValue)
            {
                int daysToEnd = (int)(RecurrenceEndDate.Value.DateTime.Date - _selectedDate.Date).TotalDays + 1;
                lookAheadDays = Math.Min(lookAheadDays, Math.Max(daysToEnd, 1));
            }
            var upcomingDates = BuildUpcomingDates(lookAheadDays);
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

    private List<DateTime> BuildUpcomingDates(int daysAhead)
    {
        var result = new List<DateTime>();
        var startPoint = _selectedDate.Date;

        for (int i = 0; i < daysAhead; i++)
        {
            var target = startPoint.AddDays(i);
            bool match = RecurrenceIndex switch
            {
                1 => true,
                2 => target.DayOfWeek != DayOfWeek.Saturday && target.DayOfWeek != DayOfWeek.Sunday,
                3 =>
                    (target.DayOfWeek == DayOfWeek.Monday    && DayMon) ||
                    (target.DayOfWeek == DayOfWeek.Tuesday   && DayTue) ||
                    (target.DayOfWeek == DayOfWeek.Wednesday && DayWed) ||
                    (target.DayOfWeek == DayOfWeek.Thursday  && DayThu) ||
                    (target.DayOfWeek == DayOfWeek.Friday    && DayFri) ||
                    (target.DayOfWeek == DayOfWeek.Saturday  && DaySat) ||
                    (target.DayOfWeek == DayOfWeek.Sunday    && DaySun),
                4 => target.Day == MonthlyDay,
                5 => target.Day == EventDate.Day && target.Month == EventDate.Month,
                6 => IsShiftWorkingDay(target),
                7 => IsCustomIntervalDay(target),
                _ => false
            };

            if (match) result.Add(target);
        }

        return result;
    }

    private bool IsShiftWorkingDay(DateTime date)
    {
        var cycleStart = CycleStartDate?.DateTime.Date ?? _originalSeriesStartDate.Date;
        if (date < cycleStart) return false;

        int working = WorkingDays > 0 ? WorkingDays : 2;
        int off     = OffDays > 0     ? OffDays     : 2;
        int total   = working + off;
        int passed  = (date - cycleStart).Days;
        int pos     = passed % total;
        return pos < working;
    }

    private bool IsCustomIntervalDay(DateTime date)
    {
        if (IntervalValue <= 0) return false;
        var origin = _originalSeriesStartDate.Date;
        int daysDiff = (date - origin).Days;
        if (daysDiff < 0) return false;

        return IntervalUnitIndex switch
        {
            0 => daysDiff % IntervalValue == 0,
            1 => daysDiff % (IntervalValue * 7) == 0,
            2 => ((date.Year - origin.Year) * 12 + date.Month - origin.Month) % IntervalValue == 0 &&
                 date.Day == origin.Day,
            _ => false
        };
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Title)) return;

        // ИСПРАВЛЕНО (Часть 1, п.2): для серий повторений сохраняем исходную дату начала серии,
        // для нового или неповторяющегося события — берём дату, выбранную пользователем в DatePicker.
        DateTime baseDate = (RecurrenceIndex != 0 && _originalId != 0)
            ? _originalSeriesStartDate.Date
            : EventDate.Date;

        DateTime start, end;
        if (IsAllDay)
        {
            start = baseDate;
            end   = baseDate.AddDays(1).AddSeconds(-1);
        }
        else
        {
            start = baseDate.AddHours(StartHour).AddMinutes(StartMinute);
            end   = baseDate.AddHours(EndHour).AddMinutes(EndMinute);
            if (end <= start) return;
        }

        int multiplier = (NotificationUnitIndex >= 0 && NotificationUnitIndex < UnitMultipliers.Length)
            ? UnitMultipliers[NotificationUnitIndex] : 1;
        int offsetMins = NotificationQuantity > 0 ? NotificationQuantity * multiplier : 0;

        ResultEvent = new CalendarEvent
        {
            Id          = _originalId,
            Title       = Title.Trim(),
            Start       = start,
            End         = end,
            Color       = Color,
            TaskId      = _originalTaskId,
            IsAllDay    = IsAllDay,
            Recurrence  = (RecurrenceType)RecurrenceIndex,
            DaysOfWeek  = new List<DayOfWeek>(),
            RecurrenceEndDate = RecurrenceEndDate?.DateTime.Date,
            NotificationOffsetMinutes = offsetMins,
            Location    = HasLocation && !string.IsNullOrWhiteSpace(LocationText)
                ? new PlaceLocation { DisplayName = LocationText.Trim() }
                : null
        };

        // Сохраняем день начала повторения для Monthly/Yearly
        if (RecurrenceIndex == 4) // Monthly
        {
            ResultEvent.RecurrenceStartDay = MonthlyDay;
        }
        else if (RecurrenceIndex == 5) // Yearly — день/месяц берём из даты события
        {
            ResultEvent.RecurrenceStartDay   = EventDate.Day;
            ResultEvent.RecurrenceStartMonth = EventDate.Month;
            ResultEvent.RecurrenceEndYear    = DateTime.Now.Year + 100;
        }

        if (DayMon) ResultEvent.DaysOfWeek.Add(DayOfWeek.Monday);
        if (DayTue) ResultEvent.DaysOfWeek.Add(DayOfWeek.Tuesday);
        if (DayWed) ResultEvent.DaysOfWeek.Add(DayOfWeek.Wednesday);
        if (DayThu) ResultEvent.DaysOfWeek.Add(DayOfWeek.Thursday);
        if (DayFri) ResultEvent.DaysOfWeek.Add(DayOfWeek.Friday);
        if (DaySat) ResultEvent.DaysOfWeek.Add(DayOfWeek.Saturday);
        if (DaySun) ResultEvent.DaysOfWeek.Add(DayOfWeek.Sunday);

        if (RecurrenceIndex == 6)
        {
            ResultEvent.WorkingDays    = WorkingDays;
            ResultEvent.OffDays        = OffDays;
            ResultEvent.CycleStartDate = CycleStartDate?.DateTime ?? baseDate;
        }

        if (RecurrenceIndex == 7)
        {
            ResultEvent.IntervalValue = IntervalValue;
            ResultEvent.IntervalUnit  = (IntervalUnit)IntervalUnitIndex;
        }

        if (SaveAsTemplate && !string.IsNullOrWhiteSpace(TemplateName))
        {
            var eventTemplate = new EventTemplate
            {
                Name         = TemplateName.Trim(),
                Title        = ResultEvent.Title,
                IsAllDay     = ResultEvent.IsAllDay,
                StartHour    = StartHour,
                StartMinute  = StartMinute,
                EndHour      = EndHour,
                EndMinute    = EndMinute,
                Color        = ResultEvent.Color,
                Recurrence   = ResultEvent.Recurrence,
                DaysOfWeek   = new List<DayOfWeek>(ResultEvent.DaysOfWeek),
                WorkingDays  = ResultEvent.WorkingDays,
                OffDays      = ResultEvent.OffDays,
                CycleStartDate = ResultEvent.CycleStartDate,
                IntervalValue  = ResultEvent.IntervalValue,
                IntervalUnit   = ResultEvent.IntervalUnit
            };
            _templateService.SaveEventTemplate(eventTemplate);
            LoadEventTemplates();
        }

        CloseWindow(true);
    }

    [RelayCommand]
    private async Task CopyToSelectedDays()
    {
        if (_originalId == 0) return; // только для существующих событий

        var desktop = App.Current?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var currentWindow = desktop?.Windows.FirstOrDefault(w => w.DataContext == this);
        if (currentWindow == null) return;

        var services = ((App)Avalonia.Application.Current!).Services!;
        var db = services.GetRequiredService<IDatabaseService>();

        // Получаем оригинальное событие из БД
        var originalEvent = db.GetEventById(_originalId);
        if (originalEvent == null) return;

        var copyVm = new EventCopyViewModel(originalEvent);
        var copyDialog = new EventCopyDialog { DataContext = copyVm };
        await copyDialog.ShowDialog(currentWindow);

        if (copyVm.SelectedDates.Count == 0) return;

        // Создаём копии для каждой выбранной даты
        var copies = copyVm.SelectedDates.Select(date => new CalendarEvent
        {
            Title                    = originalEvent.Title,
            Start                    = date.Date.Add(originalEvent.Start.TimeOfDay),
            End                      = date.Date.Add(originalEvent.End.TimeOfDay),
            Color                    = originalEvent.Color,
            IsAllDay                 = originalEvent.IsAllDay,
            Recurrence               = RecurrenceType.None,
            NotificationOffsetMinutes = originalEvent.NotificationOffsetMinutes,
            SyncId                   = Guid.NewGuid(),
            LastModified             = DateTime.UtcNow
        }).ToList();

        db.InsertEvents(copies);

        var notif = services.GetRequiredService<INotificationService>();
        notif.Show(
            Loc["CopyEventTitle"],
            $"{Loc["CopySelectedCount"]} {copies.Count}",
            NotificationLevel.Success);
    }

    [RelayCommand]
    private void Cancel() => CloseWindow(false);

    private void CloseWindow(bool result)
    {
        if (App.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Windows.FirstOrDefault(w => w.DataContext == this)?.Close(result);
        }
    }

    public IReadOnlyList<string> RecurrenceItems => new[]
    {
        Loc["Recurrence_None"],
        Loc["Recurrence_Daily"],
        Loc["Recurrence_Weekdays"],
        Loc["Recurrence_Weekly"],
        Loc["Recurrence_Monthly"],
        Loc["Recurrence_Yearly"],
        Loc["Recurrence_Shift"],
        Loc["Recurrence_Custom"]
    };

    public IReadOnlyList<string> IntervalUnitItems => new[]
    {
        Loc["IntervalUnit_Days"],
        Loc["IntervalUnit_Weeks"],
        Loc["IntervalUnit_Months"]
    };
}
