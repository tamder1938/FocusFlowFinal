using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Models.Habits;
using FocusFlowFinal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

// ══════════════════════════════════════════════════════════════════════════════
// Вспомогательные классы
// ══════════════════════════════════════════════════════════════════════════════

public class HabitDisplayItem : ObservableObject
{
    public Habit Habit { get; }
    private bool _isCompletedToday;
    private bool _isSelected;

    public HabitDisplayItem(Habit habit, bool isCompletedToday)
    {
        Habit = habit;
        _isCompletedToday = isCompletedToday;
    }

    public bool IsCompletedToday
    {
        get => _isCompletedToday;
        set { _isCompletedToday = value; OnPropertyChanged(); }
    }
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public string Name           => Habit.Name;
    public string Icon           => Habit.Icon;
    public string Category       => Habit.Category;
    public string Color          => Habit.Color;
    public string Description    => Habit.Description;
    public int    CurrentStreak  => Habit.CurrentStreak;
    public int    BestStreak     => Habit.BestStreak;
    public int    TotalCompletions => Habit.TotalCompletions;

    public string StreakText => CurrentStreak > 0 ? $"🔥 {CurrentStreak} дн." : "";
    public string RepeatText => Habit.RepetitionType switch
    {
        HabitRepetitionType.Daily         => "Ежедневно",
        HabitRepetitionType.WeekDays      => string.Join(", ", Habit.WeekDaysList
            .Select(d => new[] { "Пн","Вт","Ср","Чт","Пт","Сб","Вс" }[d])),
        HabitRepetitionType.TimesPerWeek  => $"{Habit.TimesPerWeek}× в неделю",
        HabitRepetitionType.TimesPerMonth => $"{Habit.TimesPerMonth}× в месяц",
        _ => ""
    };

    public void Refresh()
    {
        OnPropertyChanged(nameof(CurrentStreak));
        OnPropertyChanged(nameof(BestStreak));
        OnPropertyChanged(nameof(TotalCompletions));
        OnPropertyChanged(nameof(StreakText));
    }
}

// ── HeatMap cell (GitHub-стиль, 12 недель) ────────────────────────────────
public class HabitHeatCell
{
    public DateTime Date    { get; set; }
    public int      Status  { get; set; } // 0=None, 1=Partial, 2=Done
    public bool IsFuture    { get; set; }
    public bool IsToday     { get; set; }

    public string CellColor => IsFuture  ? "Transparent"
                             : Status == 2 ? "#22C55E"
                             : Status == 1 ? "#86EFAC"
                             : IsToday    ? "#DBEAFE"
                             : "#E5E7EB";

    public string Tooltip => $"{Date:dd.MM.yyyy}" + (Status == 2 ? " ✓ Выполнено"
                                                  : Status == 1 ? " ~ Частично" : "");
}

// ── Ячейка месячной сетки (3 состояния) ──────────────────────────────────
public class MonthGridCell : ObservableObject
{
    public DateTime Date          { get; set; }
    public bool     IsCurrentMonth { get; set; }
    public bool     IsToday        { get; set; }
    public bool     IsFuture       { get; set; }

    private int _status;
    public int Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(CellColor)); OnPropertyChanged(nameof(TextColor)); OnPropertyChanged(nameof(BorderColor)); }
    }

    public string DayNumber => IsCurrentMonth ? Date.Day.ToString() : "";

    public string CellColor => !IsCurrentMonth ? "Transparent"
                             : IsFuture        ? "#F9FAFB"
                             : Status == 2     ? "#22C55E"
                             : Status == 1     ? "#F59E0B"
                             : IsToday         ? "#DBEAFE"
                             : "#F3F4F6";

    public string TextColor => Status >= 1 ? "White"
                             : IsToday     ? "#1D4ED8"
                             : "#374151";

    public string BorderColor => IsToday && IsCurrentMonth ? "#3B82F6" : "Transparent";

    public string Tooltip => !IsCurrentMonth ? "" :
        $"{Date:dd.MM.yyyy}" + (Status == 2 ? " ✓" : Status == 1 ? " ~" : "");
}

// ── Достижение ────────────────────────────────────────────────────────────
public class AchievementItem
{
    public string Title    { get; set; } = string.Empty;
    public bool   Unlocked { get; set; }
    public string Color    => Unlocked ? "#22C55E" : "#9CA3AF";
}

// ══════════════════════════════════════════════════════════════════════════════
// Основной ViewModel
// ══════════════════════════════════════════════════════════════════════════════

public partial class HabitViewModel : ObservableObject
{
    private readonly IDatabaseService  _db;
    private readonly HabitExportService _exporter;
    public LocalizationService Loc => LocalizationService.Instance;

    // ── Список привычек ───────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<HabitDisplayItem> _habits = new();
    [ObservableProperty] private HabitDisplayItem? _selectedHabitItem;
    [ObservableProperty] private string _searchText      = string.Empty;
    [ObservableProperty] private string _filterCategory  = string.Empty;
    [ObservableProperty] private bool   _showArchived;

    // ── Детали: вкладки ────────────────────────────────────────────────
    [ObservableProperty] private int _detailTabIndex = 0; // 0=Activity 1=Month 2=Achiev
    public bool ShowActivity  => DetailTabIndex == 0;
    public bool ShowMonthGrid => DetailTabIndex == 1;
    public bool ShowAchievements => DetailTabIndex == 2;

    partial void OnDetailTabIndexChanged(int v)
    {
        OnPropertyChanged(nameof(ShowActivity));
        OnPropertyChanged(nameof(ShowMonthGrid));
        OnPropertyChanged(nameof(ShowAchievements));
    }

    // ── HeatMap ───────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<HabitHeatCell>   _selectedHabitHeatMap  = new();

    // ── Месячная сетка ────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<MonthGridCell>   _monthGridCells = new();
    [ObservableProperty] private int _currentGridYear  = DateTime.Today.Year;
    [ObservableProperty] private int _currentGridMonth = DateTime.Today.Month;
    public string CurrentMonthLabel
    {
        get
        {
            var d = new DateTime(CurrentGridYear, CurrentGridMonth, 1);
            return d.ToString("MMMM yyyy", new CultureInfo("ru-RU"));
        }
    }

    // ── История ───────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<HabitCompletion> _selectedHabitHistory = new();
    [ObservableProperty] private ObservableCollection<AchievementItem> _achievements = new();

    // ── Статистика дня ────────────────────────────────────────────────
    [ObservableProperty] private int    _totalHabits;
    [ObservableProperty] private int    _todayDone;
    [ObservableProperty] private int    _todayTotal;
    [ObservableProperty] private string _todayDoneText = "0/0";

    // ── Категории ─────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<string>        _categoryNames      = new();
    [ObservableProperty] private ObservableCollection<string>        _filterCategoryList = new();
    [ObservableProperty] private ObservableCollection<HabitCategory> _allCategories       = new();
    [ObservableProperty] private string? _selectedCategoryFilter;
    [ObservableProperty] private bool    _isAddingCategory;
    [ObservableProperty] private string  _newCategoryName = string.Empty;

    partial void OnSelectedCategoryFilterChanged(string? v)
    {
        FilterCategory = string.IsNullOrEmpty(v) || v == Loc["Habit_AllCats"] ? "" : v;
    }

    // ── Форма создания/редактирования ─────────────────────────────────
    [ObservableProperty] private bool   _isAddingHabit;
    private Habit? _editingHabit;

    [ObservableProperty] private string _formName     = string.Empty;
    [ObservableProperty] private string _formDesc     = string.Empty;
    [ObservableProperty] private string _formCategory = string.Empty;
    [ObservableProperty] private string _formIcon     = "⭐";
    [ObservableProperty] private string _formColor    = "#3B82F6";
    [ObservableProperty] private int    _formRepeatIndex  = 0;
    [ObservableProperty] private int    _formTimesPerWeek  = 3;
    [ObservableProperty] private int    _formTimesPerMonth = 10;
    [ObservableProperty] private bool   _formMonday    = true;
    [ObservableProperty] private bool   _formTuesday   = true;
    [ObservableProperty] private bool   _formWednesday = true;
    [ObservableProperty] private bool   _formThursday  = true;
    [ObservableProperty] private bool   _formFriday    = true;
    [ObservableProperty] private bool   _formSaturday;
    [ObservableProperty] private bool   _formSunday;
    [ObservableProperty] private string _formError = string.Empty;

    // ── Интеграция с задачами ─────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<TaskItem> _availableTasks = new();
    [ObservableProperty] private int? _formLinkedTaskId;

    // ── Шаблоны ───────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<HabitTemplate> _systemTemplates = new();
    [ObservableProperty] private ObservableCollection<HabitTemplate> _userTemplates   = new();
    [ObservableProperty] private bool   _isChoosingTemplate;
    [ObservableProperty] private bool   _isSavingTemplate;
    [ObservableProperty] private string _templateSaveName = string.Empty;

    // ── Экспорт ───────────────────────────────────────────────────────
    [ObservableProperty] private string _exportStatus = string.Empty;

    // ── Иконки / повторение ───────────────────────────────────────────
    public string[] IconPresets => new[]
    {
        "⭐","🔥","💪","📚","💊","🏃","🧘","💧","🥗","🎯",
        "🎵","✍️","🌿","🚀","💰","🏠","😴","🎮","🌍","❤️"
    };

    public string[] RepeatItems => new[]
    {
        Loc["Habit_Repeat_Daily"],
        Loc["Habit_Repeat_Weekdays"],
        Loc["Habit_Repeat_PerWeek"],
        Loc["Habit_Repeat_PerMonth"]
    };

    public bool FormShowWeekDays      => FormRepeatIndex == 1;
    public bool FormShowTimesPerWeek  => FormRepeatIndex == 2;
    public bool FormShowTimesPerMonth => FormRepeatIndex == 3;

    partial void OnFormRepeatIndexChanged(int v)
    {
        OnPropertyChanged(nameof(FormShowWeekDays));
        OnPropertyChanged(nameof(FormShowTimesPerWeek));
        OnPropertyChanged(nameof(FormShowTimesPerMonth));
    }

    public bool IsHabitsEmpty         => Habits.Count == 0;
    public bool IsHistoryEmpty        => SelectedHabitHistory.Count == 0;
    public bool IsUserTemplatesEmpty  => UserTemplates.Count == 0;
    public bool IsCategoriesEmpty     => AllCategories.Count == 0;

    // ══════════════════════════════════════════════════════════════════
    public HabitViewModel(IDatabaseService db)
    {
        _db       = db;
        _exporter = new HabitExportService(db);
        LoadAll();
    }

    // ── Загрузка ──────────────────────────────────────────────────────

    private void LoadAll()
    {
        LoadCategories();
        LoadHabits();
        LoadTasks();
        LoadTemplates();
        RefreshStats();
    }

    private void LoadCategories()
    {
        var prevFilter = SelectedCategoryFilter;
        AllCategories.Clear();
        CategoryNames.Clear();
        FilterCategoryList.Clear();
        FilterCategoryList.Add(Loc["Habit_AllCats"]);

        foreach (var c in _db.GetAllHabitCategories())
        {
            AllCategories.Add(c);
            CategoryNames.Add(c.Name);
            FilterCategoryList.Add(c.Name);
        }

        // Восстанавливаем выбранный фильтр или сбрасываем если категория удалена
        SelectedCategoryFilter = CategoryNames.Contains(prevFilter)
            ? prevFilter
            : Loc["Habit_AllCats"];

        OnPropertyChanged(nameof(IsCategoriesEmpty));
    }

    private void LoadTasks()
    {
        AvailableTasks.Clear();
        foreach (var t in _db.GetAllTasks().Where(t => !t.IsCompleted))
            AvailableTasks.Add(t);
    }

    private void LoadTemplates()
    {
        SystemTemplates.Clear();
        UserTemplates.Clear();
        foreach (var t in _db.GetAllHabitTemplates())
        {
            if (t.IsSystem) SystemTemplates.Add(t);
            else            UserTemplates.Add(t);
        }
    }

    private void LoadHabits()
    {
        var today      = DateTime.Today;
        var all        = _db.GetAllHabits().ToList();
        var todayComps = _db.GetCompletionsForPeriod(today, today.AddDays(1)).ToList();

        Habits.Clear();
        foreach (var h in all)
        {
            if (h.IsArchived != ShowArchived) continue;
            if (!string.IsNullOrEmpty(FilterCategory) && FilterCategory != h.Category) continue;
            if (!string.IsNullOrEmpty(SearchText) &&
                !h.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) continue;

            CheckStreakExpiry(h);
            bool done = todayComps.Any(c => c.HabitId == h.Id && c.Status >= 1);
            Habits.Add(new HabitDisplayItem(h, done));
        }

        OnPropertyChanged(nameof(IsHabitsEmpty));
    }

    private void CheckStreakExpiry(Habit h)
    {
        if (h.LastCompletedDate == null || h.CurrentStreak == 0) return;
        var last  = h.LastCompletedDate.Value.Date;
        var today = DateTime.Today;
        if (last == today) return;

        bool expired = h.RepetitionType switch
        {
            HabitRepetitionType.Daily    => today > last.AddDays(1),
            HabitRepetitionType.WeekDays => HasMissedScheduledDay(h, last.AddDays(1), today.AddDays(-1)),
            _                            => false
        };

        if (expired) { h.CurrentStreak = 0; _db.UpsertHabit(h); }
    }

    private bool HasMissedScheduledDay(Habit h, DateTime from, DateTime to)
    {
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            int dow = ((int)d.DayOfWeek + 6) % 7;
            if (h.WeekDaysList.Contains(dow) && !_db.HasCompletionForDate(h.Id, d))
                return true;
        }
        return false;
    }

    private void RefreshStats()
    {
        var active = _db.GetAllHabits().Where(h => !h.IsArchived).ToList();
        TotalHabits = active.Count;
        var today = DateTime.Today;
        var comps = _db.GetCompletionsForPeriod(today, today.AddDays(1))
                       .Where(c => c.Status >= 1).ToList();
        TodayDone     = comps.Select(c => c.HabitId).Distinct().Count();
        TodayTotal    = active.Count;
        TodayDoneText = $"{TodayDone}/{TodayTotal}";
    }

    // ── Выбор привычки ────────────────────────────────────────────────

    [RelayCommand]
    private void SelectHabit(HabitDisplayItem? item)
    {
        foreach (var h in Habits) h.IsSelected = false;
        SelectedHabitItem = item;
        if (item == null) return;
        item.IsSelected = true;
        LoadHabitDetail(item);
    }

    private void LoadHabitDetail(HabitDisplayItem item)
    {
        LoadHeatMap(item);
        LoadMonthGrid(item.Habit.Id);
        LoadHistory(item);
        RefreshAchievements(item.Habit);
    }

    private void LoadHeatMap(HabitDisplayItem item)
    {
        SelectedHabitHeatMap.Clear();
        var today       = DateTime.Today;
        int dow         = ((int)today.DayOfWeek + 6) % 7;
        var startMonday = today.AddDays(-dow - 11 * 7);

        var compsByDate = _db.GetCompletionsForPeriod(startMonday, today.AddDays(1))
                             .Where(c => c.HabitId == item.Habit.Id)
                             .ToDictionary(c => c.Date.Date, c => c.Status);

        // Генерируем в порядке день-недели x неделя — для UniformGrid Columns="12"
        // Row 0 = все понедельники, Row 1 = все вторники, ... Row 6 = все воскресенья
        for (int dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++)
        {
            for (int week = 0; week < 12; week++)
            {
                var d = startMonday.AddDays(dayOfWeek + week * 7);
                SelectedHabitHeatMap.Add(new HabitHeatCell
                {
                    Date     = d,
                    Status   = compsByDate.GetValueOrDefault(d, 0),
                    IsFuture = d > today,
                    IsToday  = d == today
                });
            }
        }
    }

    private void LoadHistory(HabitDisplayItem item)
    {
        SelectedHabitHistory.Clear();
        foreach (var c in _db.GetHabitCompletions(item.Habit.Id).Take(30))
            SelectedHabitHistory.Add(c);
        OnPropertyChanged(nameof(IsHistoryEmpty));
    }

    // ── Месячная сетка ────────────────────────────────────────────────

    private void LoadMonthGrid(int habitId)
    {
        MonthGridCells.Clear();
        var today    = DateTime.Today;
        var first    = new DateTime(CurrentGridYear, CurrentGridMonth, 1);
        var lastDay  = first.AddMonths(1).AddDays(-1);
        var comps    = _db.GetCompletionsForPeriod(first, lastDay.AddDays(1))
                         .Where(c => c.HabitId == habitId)
                         .ToDictionary(c => c.Date.Date, c => c);

        // Пустые ячейки до первого числа (0=Пн)
        int startDow = ((int)first.DayOfWeek + 6) % 7;
        for (int i = 0; i < startDow; i++)
            MonthGridCells.Add(new MonthGridCell { IsCurrentMonth = false, Date = first.AddDays(i - startDow) });

        for (var d = first; d <= lastDay; d = d.AddDays(1))
        {
            comps.TryGetValue(d, out var comp);
            MonthGridCells.Add(new MonthGridCell
            {
                Date           = d,
                IsCurrentMonth = true,
                IsToday        = d == today,
                IsFuture       = d > today,
                Status         = comp?.Status ?? 0
            });
        }

        // Выравнивание до кратного 7
        int rem = (7 - MonthGridCells.Count % 7) % 7;
        for (int i = 0; i < rem; i++)
            MonthGridCells.Add(new MonthGridCell { IsCurrentMonth = false });

        OnPropertyChanged(nameof(CurrentMonthLabel));
    }

    [RelayCommand]
    private void PrevMonth()
    {
        if (CurrentGridMonth == 1) { CurrentGridMonth = 12; CurrentGridYear--; }
        else CurrentGridMonth--;
        if (SelectedHabitItem != null) LoadMonthGrid(SelectedHabitItem.Habit.Id);
        OnPropertyChanged(nameof(CurrentMonthLabel));
    }

    [RelayCommand]
    private void NextMonth()
    {
        if (CurrentGridMonth == 12) { CurrentGridMonth = 1; CurrentGridYear++; }
        else CurrentGridMonth++;
        if (SelectedHabitItem != null) LoadMonthGrid(SelectedHabitItem.Habit.Id);
        OnPropertyChanged(nameof(CurrentMonthLabel));
    }

    [RelayCommand]
    private void ToggleCellStatus(MonthGridCell? cell)
    {
        if (cell == null || !cell.IsCurrentMonth || cell.IsFuture || SelectedHabitItem == null) return;

        // Цикл: 0 → 2 (Выполнено) → 1 (Частично) → 0 (Нет)
        int next = cell.Status switch { 0 => 2, 2 => 1, _ => 0 };

        if (next == 0)
        {
            var existing = _db.GetCompletionForDate(SelectedHabitItem.Habit.Id, cell.Date);
            if (existing != null)
            {
                _db.DeleteHabitCompletion(existing.Id);
                SelectedHabitItem.Habit.TotalCompletions = Math.Max(0, SelectedHabitItem.Habit.TotalCompletions - 1);
            }
        }
        else
        {
            var comp = _db.GetCompletionForDate(SelectedHabitItem.Habit.Id, cell.Date)
                       ?? new HabitCompletion { HabitId = SelectedHabitItem.Habit.Id, Date = cell.Date };
            bool wasNew = comp.Id == 0;
            comp.Status = next;
            _db.UpsertHabitCompletion(comp);
            if (wasNew) SelectedHabitItem.Habit.TotalCompletions++;
        }

        _db.UpsertHabit(SelectedHabitItem.Habit);
        cell.Status = next;

        if (cell.Date == DateTime.Today)
        {
            SelectedHabitItem.IsCompletedToday = next >= 1;
            LoadHeatMap(SelectedHabitItem);
        }

        SelectedHabitItem.Refresh();
        RefreshStats();
    }

    [RelayCommand]
    private void SetDetailTab(object? tab)
    {
        DetailTabIndex = tab switch
        {
            int i    => i,
            string s when int.TryParse(s, out int idx) => idx,
            _ => 0
        };
    }

    // ── Отметить выполненным ─────────────────────────────────────────

    [RelayCommand]
    private void ToggleComplete(HabitDisplayItem? item)
    {
        if (item == null) return;
        var today = DateTime.Today;

        if (item.IsCompletedToday)
        {
            var comps = _db.GetHabitCompletions(item.Habit.Id)
                           .Where(c => c.Date.Date == today).ToList();
            foreach (var c in comps) _db.DeleteHabitCompletion(c.Id);
            item.Habit.TotalCompletions = Math.Max(0, item.Habit.TotalCompletions - 1);
            RecalcStreak(item.Habit);
            item.IsCompletedToday = false;
        }
        else
        {
            var comp = new HabitCompletion { HabitId = item.Habit.Id, Date = today, Status = 2 };
            _db.UpsertHabitCompletion(comp);
            item.Habit.TotalCompletions++;
            UpdateStreak(item.Habit, today);
            item.IsCompletedToday = true;
        }

        _db.UpsertHabit(item.Habit);
        item.Refresh();

        if (SelectedHabitItem?.Habit.Id == item.Habit.Id)
            LoadHabitDetail(item);

        RefreshStats();
    }

    private void UpdateStreak(Habit h, DateTime today)
    {
        var last = h.LastCompletedDate?.Date;
        if (last == null || last < today.AddDays(-1)) h.CurrentStreak = 1;
        else if (last == today.AddDays(-1))           h.CurrentStreak++;
        if (h.CurrentStreak > h.BestStreak) h.BestStreak = h.CurrentStreak;
        h.LastCompletedDate = today;
    }

    private void RecalcStreak(Habit h)
    {
        var comps = _db.GetHabitCompletions(h.Id)
                       .Where(c => c.Date.Date < DateTime.Today && c.Status >= 1)
                       .OrderByDescending(c => c.Date).ToList();

        h.CurrentStreak = 0;
        h.LastCompletedDate = null;
        if (!comps.Any()) return;

        h.LastCompletedDate = comps.First().Date.Date;
        int streak = 0;
        var expected = comps.First().Date.Date;
        foreach (var c in comps)
        {
            if (c.Date.Date == expected) { streak++; expected = expected.AddDays(-1); }
            else break;
        }
        h.CurrentStreak = streak;
    }

    // ── Поиск и фильтр ────────────────────────────────────────────────

    partial void OnSearchTextChanged(string v)     => LoadHabits();
    partial void OnFilterCategoryChanged(string v) => LoadHabits();
    partial void OnShowArchivedChanged(bool v)     => LoadHabits();

    [RelayCommand]
    private void SetFilterCategory(string cat) => FilterCategory = cat;

    // ── CRUD привычек ─────────────────────────────────────────────────

    [RelayCommand]
    private void BeginAddHabit()
    {
        _editingHabit     = null;
        FormName          = string.Empty;
        FormDesc          = string.Empty;
        FormCategory      = CategoryNames.FirstOrDefault() ?? string.Empty;
        FormIcon          = "⭐";
        FormColor         = "#3B82F6";
        FormRepeatIndex   = 0;
        FormTimesPerWeek  = 3;
        FormTimesPerMonth = 10;
        FormMonday = FormTuesday = FormWednesday = FormThursday = FormFriday = true;
        FormSaturday = FormSunday = false;
        FormLinkedTaskId  = null;
        FormError         = string.Empty;
        IsChoosingTemplate = false;
        IsAddingHabit     = true;
    }

    [RelayCommand]
    private void BeginEditHabit(HabitDisplayItem? item)
    {
        if (item == null) return;
        var h = item.Habit;
        _editingHabit     = h;
        FormName          = h.Name;
        FormDesc          = h.Description;
        FormCategory      = h.Category;
        FormIcon          = h.Icon;
        FormColor         = h.Color;
        FormRepeatIndex   = (int)h.RepetitionType;
        FormTimesPerWeek  = h.TimesPerWeek;
        FormTimesPerMonth = h.TimesPerMonth;
        FormMonday    = h.WeekDaysList.Contains(0);
        FormTuesday   = h.WeekDaysList.Contains(1);
        FormWednesday = h.WeekDaysList.Contains(2);
        FormThursday  = h.WeekDaysList.Contains(3);
        FormFriday    = h.WeekDaysList.Contains(4);
        FormSaturday  = h.WeekDaysList.Contains(5);
        FormSunday    = h.WeekDaysList.Contains(6);
        FormLinkedTaskId   = h.LinkedTaskId;
        FormError          = string.Empty;
        IsChoosingTemplate = false;
        IsAddingHabit      = true;
    }

    [RelayCommand]
    private void SaveHabit()
    {
        if (string.IsNullOrWhiteSpace(FormName)) { FormError = Loc["Habit_NameRequired"]; return; }

        var h       = _editingHabit ?? new Habit { StartDate = DateTime.Today };
        h.Name      = FormName.Trim();
        h.Description    = FormDesc;
        h.Category       = FormCategory;
        h.Icon           = FormIcon;
        h.Color          = FormColor;
        h.RepetitionType = (HabitRepetitionType)FormRepeatIndex;
        h.TimesPerWeek   = FormTimesPerWeek;
        h.TimesPerMonth  = FormTimesPerMonth;
        h.LinkedTaskId   = FormLinkedTaskId;

        h.WeekDaysList.Clear();
        if (FormMonday)    h.WeekDaysList.Add(0);
        if (FormTuesday)   h.WeekDaysList.Add(1);
        if (FormWednesday) h.WeekDaysList.Add(2);
        if (FormThursday)  h.WeekDaysList.Add(3);
        if (FormFriday)    h.WeekDaysList.Add(4);
        if (FormSaturday)  h.WeekDaysList.Add(5);
        if (FormSunday)    h.WeekDaysList.Add(6);

        _db.UpsertHabit(h);
        IsAddingHabit = false;
        FormError = string.Empty;
        LoadHabits();
        RefreshStats();
    }

    [RelayCommand]
    private void CancelHabit() { IsAddingHabit = false; FormError = string.Empty; IsChoosingTemplate = false; }

    [RelayCommand]
    private void DeleteHabit(HabitDisplayItem? item)
    {
        if (item == null) return;
        _db.DeleteHabit(item.Habit.Id);
        if (SelectedHabitItem?.Habit.Id == item.Habit.Id) SelectedHabitItem = null;
        LoadHabits();
        RefreshStats();
    }

    [RelayCommand]
    private void ArchiveHabit(HabitDisplayItem? item)
    {
        if (item == null) return;
        item.Habit.IsArchived = true;
        _db.UpsertHabit(item.Habit);
        if (SelectedHabitItem?.Habit.Id == item.Habit.Id) SelectedHabitItem = null;
        LoadHabits();
        RefreshStats();
    }

    [RelayCommand]
    private void RestoreHabit(HabitDisplayItem? item)
    {
        if (item == null) return;
        item.Habit.IsArchived = false;
        _db.UpsertHabit(item.Habit);
        LoadHabits();
        RefreshStats();
    }

    // ── Иконка ────────────────────────────────────────────────────────
    [RelayCommand]
    private void SelectIcon(string icon) => FormIcon = icon;

    // ── Категории ────────────────────────────────────────────────────

    [RelayCommand]
    private void BeginAddCategory() { NewCategoryName = string.Empty; IsAddingCategory = true; }

    [RelayCommand]
    private void SaveCategory()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName)) { IsAddingCategory = false; return; }
        var cat = new HabitCategory { Name = NewCategoryName.Trim() };
        _db.UpsertHabitCategory(cat);
        IsAddingCategory = false;
        LoadCategories();
        FormCategory = NewCategoryName.Trim();
    }

    [RelayCommand]
    private void CancelCategory() => IsAddingCategory = false;

    [RelayCommand]
    private void DeleteHabitCategory(HabitCategory? cat)
    {
        if (cat == null) return;
        _db.DeleteHabitCategory(cat.Id);
        if (FilterCategory == cat.Name) FilterCategory = "";
        LoadCategories();
        LoadHabits();
    }

    [RelayCommand]
    private void DeleteCompletion(HabitCompletion? comp)
    {
        if (comp == null || SelectedHabitItem == null) return;
        _db.DeleteHabitCompletion(comp.Id);
        SelectedHabitItem.Habit.TotalCompletions = Math.Max(0, SelectedHabitItem.Habit.TotalCompletions - 1);
        if (comp.Date.Date == DateTime.Today) SelectedHabitItem.IsCompletedToday = false;
        RecalcStreak(SelectedHabitItem.Habit);
        _db.UpsertHabit(SelectedHabitItem.Habit);
        SelectedHabitItem.Refresh();
        LoadHabitDetail(SelectedHabitItem);
        RefreshStats();
    }

    // ── Достижения ────────────────────────────────────────────────────

    private void RefreshAchievements(Habit h)
    {
        Achievements.Clear();
        Achievements.Add(new AchievementItem { Title = Loc["Habit_Achiev_7days"],   Unlocked = h.BestStreak >= 7 });
        Achievements.Add(new AchievementItem { Title = Loc["Habit_Achiev_30days"],  Unlocked = h.BestStreak >= 30 });
        Achievements.Add(new AchievementItem { Title = Loc["Habit_Achiev_90days"],  Unlocked = h.BestStreak >= 90 });
        Achievements.Add(new AchievementItem { Title = Loc["Habit_Achiev_100done"], Unlocked = h.TotalCompletions >= 100 });
    }

    // ── Шаблоны ───────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleTemplatePicker()
    {
        IsChoosingTemplate = !IsChoosingTemplate;
        IsSavingTemplate   = false;
    }

    [RelayCommand]
    private void LoadTemplate(HabitTemplate? t)
    {
        if (t == null) return;
        FormName          = t.Name;
        FormDesc          = t.Description;
        FormCategory      = t.Category;
        FormIcon          = t.Icon;
        FormColor         = t.Color;
        FormRepeatIndex   = (int)t.RepetitionType;
        FormTimesPerWeek  = t.TimesPerWeek;
        FormTimesPerMonth = t.TimesPerMonth;
        FormMonday    = t.WeekDaysList.Contains(0);
        FormTuesday   = t.WeekDaysList.Contains(1);
        FormWednesday = t.WeekDaysList.Contains(2);
        FormThursday  = t.WeekDaysList.Contains(3);
        FormFriday    = t.WeekDaysList.Contains(4);
        FormSaturday  = t.WeekDaysList.Contains(5);
        FormSunday    = t.WeekDaysList.Contains(6);
        IsChoosingTemplate = false;
    }

    [RelayCommand]
    private void BeginSaveTemplate()
    {
        TemplateSaveName   = FormName;
        IsSavingTemplate   = true;
        IsChoosingTemplate = false;
    }

    [RelayCommand]
    private void ConfirmSaveTemplate()
    {
        if (string.IsNullOrWhiteSpace(TemplateSaveName)) { IsSavingTemplate = false; return; }
        var t = new HabitTemplate
        {
            Name           = TemplateSaveName.Trim(),
            Description    = FormDesc,
            Category       = FormCategory,
            Icon           = FormIcon,
            Color          = FormColor,
            RepetitionType = (HabitRepetitionType)FormRepeatIndex,
            TimesPerWeek   = FormTimesPerWeek,
            TimesPerMonth  = FormTimesPerMonth,
        };
        t.WeekDaysList.Clear();
        if (FormMonday)    t.WeekDaysList.Add(0);
        if (FormTuesday)   t.WeekDaysList.Add(1);
        if (FormWednesday) t.WeekDaysList.Add(2);
        if (FormThursday)  t.WeekDaysList.Add(3);
        if (FormFriday)    t.WeekDaysList.Add(4);
        if (FormSaturday)  t.WeekDaysList.Add(5);
        if (FormSunday)    t.WeekDaysList.Add(6);

        _db.UpsertHabitTemplate(t);
        IsSavingTemplate = false;
        LoadTemplates();
        ExportStatus = Loc["Habit_TplSaved"];
    }

    [RelayCommand]
    private void DeleteTemplate(HabitTemplate? t)
    {
        if (t == null || t.IsSystem) return;
        _db.DeleteHabitTemplate(t.Id);
        LoadTemplates();
    }

    // ── Экспорт ───────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ExportCsv()
    {
        try
        {
            var path = await Task.Run(() => _exporter.ExportToCsv());
            ExportStatus = $"{Loc["Habit_ExportDone"]} {Path.GetFileName(path)}";
            OpenFolder(path);
        }
        catch (Exception ex)
        {
            ExportStatus = $"{Loc["Habit_ExportError"]}: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        try
        {
            var path = await Task.Run(() => _exporter.ExportToExcel());
            ExportStatus = $"{Loc["Habit_ExportDone"]} {Path.GetFileName(path)}";
            OpenFolder(path);
        }
        catch (Exception ex)
        {
            ExportStatus = $"{Loc["Habit_ExportError"]}: {ex.Message}";
        }
    }

    private static void OpenFolder(string filePath)
    {
        try { Process.Start("explorer.exe", $"/select,\"{filePath}\""); }
        catch { /* ignore */ }
    }
}
