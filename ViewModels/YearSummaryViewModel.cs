using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.YearStats;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

// ── Ячейка героограммы ──────────────────────────────────────────────────────
public class HeatCellVm
{
    public DateTime? Date    { get; set; }
    public bool      HasDate { get; set; }
    public int       Level   { get; set; }
    public string    Color   { get; set; } = "Transparent";
    public string    Tooltip { get; set; } = string.Empty;
}

// ── Мини-календарь месяца ───────────────────────────────────────────────────
public class MonthHeatmapVm
{
    public string                          MonthName { get; set; } = string.Empty;
    public ObservableCollection<HeatCellVm> Cells   { get; set; } = new();
}

// ── Элемент топ-списка ──────────────────────────────────────────────────────
public class TopItem
{
    public string Name    { get; set; } = string.Empty;
    public string Color   { get; set; } = "#6B7280";
    public string ValueLabel { get; set; } = string.Empty;
    public double Percent { get; set; }
}

// ── Столбик месячного бар-графика ───────────────────────────────────────────
public class MonthBarItem
{
    public string Label      { get; set; } = string.Empty;
    public double Hours      { get; set; }
    public double HeightRatio { get; set; }
    public string Color      { get; set; } = "#3B82F6";
}

// ── Полоска настроения (stacked bar) ────────────────────────────────────────
public class MoodBarItem
{
    public string LevelLabel { get; set; } = string.Empty;
    public string Color      { get; set; } = "#9CA3AF";
    public double WidthPct   { get; set; }
    public int    Count      { get; set; }
}

// ── Главная ViewModel ────────────────────────────────────────────────────────
public partial class YearSummaryViewModel : ObservableObject
{
    private readonly IYearStatisticsService _svc;
    private readonly LocalizationService    _loc = LocalizationService.Instance;

    private static readonly string[] HeatColors =
    {
        "#D1D5DB", // Level 0 — нет активности
        "#BBF7D0", // Level 1
        "#4ADE80", // Level 2
        "#16A34A", // Level 3
        "#15803D"  // Level 4
    };

    private static readonly string[] MonthShort =
    {
        "Янв","Фев","Мар","Апр","Май","Июн",
        "Июл","Авг","Сен","Окт","Ноя","Дек"
    };

    private static readonly string[] WeekDayShort = { "Пн","Вт","Ср","Чт","Пт","Сб","Вс" };
    private static readonly string[] MoodColors    = { "#EF4444","#F97316","#EAB308","#22C55E","#10B981" };
    private static readonly string[] MoodLabels    = { "Ужасно","Плохо","Нейтр.","Хорошо","Супер" };

    // ── Состояние ────────────────────────────────────────────────────────────

    [ObservableProperty] private int    _selectedYear = DateTime.Today.Year;
    [ObservableProperty] private string _yearTitle    = string.Empty;

    // Карточка "Задачи"
    [ObservableProperty] private int    _tasksCreated;
    [ObservableProperty] private int    _tasksCompleted;
    [ObservableProperty] private double _tasksCompletionPct;
    [ObservableProperty] private ObservableCollection<TopItem> _topProjects = new();

    // Карточка "Фокус"
    [ObservableProperty] private string _focusTotalHours   = "—";
    [ObservableProperty] private string _focusAvgPerDay    = "—";
    [ObservableProperty] private string _focusBestDay      = "—";
    [ObservableProperty] private string _focusBestMonth    = "—";
    [ObservableProperty] private ObservableCollection<MonthBarItem> _focusMonthBars = new();

    // Карточка "События"
    [ObservableProperty] private int    _eventsTotal;
    [ObservableProperty] private string _eventsHours = "—";
    [ObservableProperty] private ObservableCollection<TopItem> _topEventCategories = new();

    // Карточка "Привычки"
    [ObservableProperty] private int    _habitsActive;
    [ObservableProperty] private string _habitsLongestStreak = "—";
    [ObservableProperty] private string _habitsAvgPct        = "—";
    [ObservableProperty] private ObservableCollection<TopItem> _topHabits = new();

    // Карточка "Тренировки"
    [ObservableProperty] private int    _workoutSessions;
    [ObservableProperty] private string _workoutTonnage = "—";
    [ObservableProperty] private string _workoutHours   = "—";
    [ObservableProperty] private string _workoutFav     = "—";

    // Карточка "Настроение"
    [ObservableProperty] private int    _moodTotal;
    [ObservableProperty] private string _moodBestMonth   = "—";
    [ObservableProperty] private string _moodBestPct     = "—";
    [ObservableProperty] private ObservableCollection<MoodBarItem> _moodBars = new();

    // Карточка "Заметки"
    [ObservableProperty] private int    _notesTotal;
    [ObservableProperty] private string _notesDaysWithNotes = "—";
    [ObservableProperty] private ObservableCollection<TopItem> _topNoteTags = new();

    // Карточка "Медиа"
    [ObservableProperty] private int    _mediaMovies;
    [ObservableProperty] private int    _mediaSeries;
    [ObservableProperty] private int    _mediaAnime;
    [ObservableProperty] private int    _mediaBooks;
    [ObservableProperty] private int    _mediaManga;
    [ObservableProperty] private string _mediaAvgScore = "—";
    [ObservableProperty] private ObservableCollection<TopItem> _topMedia = new();

    // Героограмма
    [ObservableProperty] private ObservableCollection<HeatCellVm>     _heatLegendCells = new();
    [ObservableProperty] private ObservableCollection<MonthHeatmapVm> _monthHeatmaps   = new();
    public ObservableCollection<string> WeekDayHeaders { get; } = new(WeekDayShort);

    // ── Конструктор ──────────────────────────────────────────────────────────

    public YearSummaryViewModel(IYearStatisticsService svc)
    {
        _svc = svc;
        Reload();
    }

    // Для открытия из Settings с конкретной датой (диалог "Сегодня")
    public YearSummaryViewModel(IYearStatisticsService svc, int year)
    {
        _svc         = svc;
        _selectedYear = year;
        Reload();
    }

    // ── Навигация по годам ───────────────────────────────────────────────────

    [RelayCommand] private void PreviousYear() { SelectedYear--; Reload(); }
    [RelayCommand] private void NextYear()     { SelectedYear++; Reload(); }

    [RelayCommand] private void CloseWindow(Window? win) => win?.Close();

    // ── Загрузка данных ───────────────────────────────────────────────────────

    private void Reload()
    {
        YearTitle = string.Format(_loc["YearSummary_Title"], SelectedYear);

        var data   = _svc.GetYearSummary(SelectedYear);
        var heatmap = _svc.GetHeatmap(SelectedYear);

        ApplyTasks(data.Tasks);
        ApplyFocus(data.Focus);
        ApplyEvents(data.Events);
        ApplyHabits(data.Habits);
        ApplyWorkouts(data.Workouts);
        ApplyMood(data.Mood);
        ApplyNotes(data.Notes);
        ApplyMedia(data.Media);
        BuildHeroHeatmap(heatmap);
        BuildHeatLegend();
    }

    private void ApplyTasks(TaskYearStats t)
    {
        TasksCreated       = t.TotalCreated;
        TasksCompleted     = t.TotalCompleted;
        TasksCompletionPct = Math.Round(t.CompletionPct, 1);

        TopProjects.Clear();
        foreach (var (name, color, count) in t.Top3Projects)
            TopProjects.Add(new TopItem { Name = name, Color = color, ValueLabel = count.ToString(), Percent = t.TotalCreated > 0 ? count * 100.0 / t.TotalCreated : 0 });
    }

    private void ApplyFocus(FocusYearStats f)
    {
        FocusTotalHours = $"{f.TotalHours} {_loc["YearSummary_Hours"]}";
        FocusAvgPerDay  = $"{f.AvgHoursPerDay} {_loc["YearSummary_Hph"]}";
        FocusBestDay    = f.MostProductiveDay.HasValue
            ? $"{f.MostProductiveDay.Value:dd.MM.yyyy} · {f.MostProductiveDayHours} {_loc["YearSummary_Hours"]}"
            : "—";
        FocusBestMonth  = string.IsNullOrEmpty(f.MostProductiveMonth)
            ? "—"
            : $"{f.MostProductiveMonth} · {f.MostProductiveMonthHours} {_loc["YearSummary_Hours"]}";

        FocusMonthBars.Clear();
        double maxH = f.MonthlyHours.Max();
        for (int i = 0; i < 12; i++)
        {
            FocusMonthBars.Add(new MonthBarItem
            {
                Label      = MonthShort[i],
                Hours      = f.MonthlyHours[i],
                HeightRatio = maxH > 0 ? f.MonthlyHours[i] / maxH * 60 : 4,
                Color      = "#3B82F6"
            });
        }
    }

    private void ApplyEvents(EventYearStats e)
    {
        EventsTotal = e.TotalEvents;
        EventsHours = $"{e.TotalHours} {_loc["YearSummary_Hours"]}";

        TopEventCategories.Clear();
        foreach (var (name, count) in e.Top3Categories)
            TopEventCategories.Add(new TopItem { Name = name, Color = name, ValueLabel = count.ToString() });
    }

    private void ApplyHabits(HabitYearStats h)
    {
        HabitsActive       = h.ActiveHabits;
        HabitsLongestStreak = h.LongestStreak > 0
            ? $"{h.LongestStreak} {_loc["YearSummary_Days"]} — {h.LongestStreakHabit}"
            : "—";
        HabitsAvgPct = $"{h.AvgCompletionPercent}%";

        TopHabits.Clear();
        foreach (var (name, pct) in h.Top3Stable)
            TopHabits.Add(new TopItem { Name = name, ValueLabel = $"{Math.Round(pct, 1)}%", Percent = pct, Color = "#22C55E" });
    }

    private void ApplyWorkouts(WorkoutYearStats w)
    {
        WorkoutSessions = w.TotalSessions;
        WorkoutTonnage  = w.TotalTonnageTons > 0 ? $"{w.TotalTonnageTons} {_loc["YearSummary_Tons"]}" : "—";
        WorkoutHours    = w.TotalHours > 0        ? $"{w.TotalHours} {_loc["YearSummary_Hours"]}" : "—";
        WorkoutFav      = string.IsNullOrEmpty(w.FavoriteExercise) ? "—" : w.FavoriteExercise;
    }

    private void ApplyMood(MoodYearStats m)
    {
        MoodTotal     = m.TotalEntries;
        MoodBestMonth = string.IsNullOrEmpty(m.BestMonth) ? "—" : m.BestMonth;
        MoodBestPct   = m.TotalEntries > 0 ? $"{m.BestMonthGoodPct}%" : "—";

        MoodBars.Clear();
        int total = m.Distribution.Values.Sum();
        for (int lvl = 5; lvl >= 1; lvl--)
        {
            int cnt = m.Distribution.GetValueOrDefault(lvl, 0);
            MoodBars.Add(new MoodBarItem
            {
                LevelLabel = MoodLabels[lvl - 1],
                Color      = MoodColors[lvl - 1],
                Count      = cnt,
                WidthPct   = total > 0 ? cnt * 100.0 / total : 0
            });
        }
    }

    private void ApplyNotes(NotesYearStats n)
    {
        NotesTotal         = n.TotalNotes;
        int daysInYear     = DateTime.IsLeapYear(SelectedYear) ? 366 : 365;
        NotesDaysWithNotes = $"{n.DaysWithNotes} {string.Format(_loc["YearSummary_Of365"], daysInYear)}";

        TopNoteTags.Clear();
        foreach (var (tag, count) in n.Top3Tags)
            TopNoteTags.Add(new TopItem { Name = $"#{tag}", ValueLabel = count.ToString(), Color = "#8B5CF6" });
    }

    private void ApplyMedia(MediaYearStats m)
    {
        MediaMovies  = m.CompletedMovies;
        MediaSeries  = m.CompletedSeries;
        MediaAnime   = m.CompletedAnime;
        MediaBooks   = m.CompletedBooks;
        MediaManga   = m.CompletedManga;
        MediaAvgScore = m.AvgScore > 0 ? m.AvgScore.ToString("F1") : "—";

        TopMedia.Clear();
        foreach (var (title, score, _) in m.Top3)
            TopMedia.Add(new TopItem { Name = title, ValueLabel = score.ToString("F1"), Color = "#F59E0B" });
    }

    // ── Героограмма ───────────────────────────────────────────────────────────

    private void BuildHeroHeatmap(IReadOnlyList<HeatCell> heatmap)
    {
        var cellsByDate = heatmap.ToDictionary(c => c.Date);
        MonthHeatmaps.Clear();

        for (int month = 1; month <= 12; month++)
        {
            var mv = new MonthHeatmapVm { MonthName = MonthShort[month - 1] };
            int daysInMonth = DateTime.DaysInMonth(SelectedYear, month);
            var firstDay    = new DateTime(SelectedYear, month, 1);
            int startOffset = ((int)firstDay.DayOfWeek + 6) % 7; // Mon=0

            // 42 cells: leading empty + days + trailing empty
            for (int i = 0; i < 42; i++)
            {
                int dayNum = i - startOffset + 1;
                if (dayNum < 1 || dayNum > daysInMonth)
                {
                    mv.Cells.Add(new HeatCellVm { HasDate = false, Color = "Transparent" });
                }
                else
                {
                    var date = new DateTime(SelectedYear, month, dayNum);
                    int level = cellsByDate.TryGetValue(date, out var c) ? c.Level : 0;
                    double score = cellsByDate.TryGetValue(date, out var c2) ? c2.ActivityScore : 0;
                    mv.Cells.Add(new HeatCellVm
                    {
                        Date    = date,
                        HasDate = true,
                        Level   = level,
                        Color   = HeatColors[level],
                        Tooltip = $"{date:dd.MM.yyyy} · активность: {score:F1}"
                    });
                }
            }

            MonthHeatmaps.Add(mv);
        }
    }

    private void BuildHeatLegend()
    {
        HeatLegendCells.Clear();
        for (int i = 0; i < 5; i++)
            HeatLegendCells.Add(new HeatCellVm { HasDate = false, Level = i, Color = HeatColors[i] });
    }

    // ── Команда открытия детального попапа для дня ────────────────────────────

    [RelayCommand]
    private async Task ShowDaySummary(object? param)
    {
        if (param is not DateTime date) return;

        var activity = _svc.GetDayActivity(date);
        var dlg      = new DayStatsDialog { DataContext = new DayStatsViewModel(activity) };

        var owner = GetMainWindow();
        if (owner != null) await dlg.ShowDialog(owner);
        else dlg.Show();
    }

    // ── Экспорт PNG ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ExportPng(object? param)
    {
        if (param is not Avalonia.Visual visual) return;

        var owner = GetMainWindow();
        if (owner == null) return;

        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title           = "Сохранить PNG",
            SuggestedFileName = $"FocusFlow_{SelectedYear}.png",
            FileTypeChoices = new[] { new FilePickerFileType("PNG") { Patterns = new[] { "*.png" } } }
        });

        if (file == null) return;

        try
        {
            var bounds = visual.Bounds;
            var size   = new Avalonia.PixelSize((int)bounds.Width, (int)bounds.Height);
            var rtb    = new RenderTargetBitmap(size);
            rtb.Render(visual);

            await using var stream = await file.OpenWriteAsync();
            rtb.Save(stream);
        }
        catch { /* silent */ }
    }

    // ── Экспорт PDF ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ExportPdf()
    {
        var owner = GetMainWindow();
        if (owner == null) return;

        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title           = "Сохранить PDF",
            SuggestedFileName = $"FocusFlow_{SelectedYear}.pdf",
            FileTypeChoices = new[] { new FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } } }
        });

        if (file == null) return;

        try
        {
            var data  = _svc.GetYearSummary(SelectedYear);
            await using var stream = await file.OpenWriteAsync();
            YearPdfExporter.Export(stream, data);
        }
        catch { /* silent */ }
    }

    private static Window? GetMainWindow() =>
        (Avalonia.Application.Current?.ApplicationLifetime
            as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow;
}
