
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public class AnalyticsSegment
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string TimeFormatted { get; set; } = "0 h 00 m";
    public double Percentage { get; set; }
    public string Color { get; set; } = "#3498db";
}

public class DayChartItem
{
    public string DayName { get; set; } = string.Empty;
    public double Hours { get; set; }
    public double HeightRatio { get; set; }
    public string Color { get; set; } = "#93C5FD";
}

public partial class AnalyticsViewModel : ObservableObject
{
    private readonly IDatabaseService _db;
    private readonly LocalizationService _localization = LocalizationService.Instance;

    // Физический максимум — 168 часов в неделю
    private const int MaxWeekMinutes = 168 * 60;

    [ObservableProperty]
    private DateTime _weekStart;

    [ObservableProperty]
    private ObservableCollection<AnalyticsSegment> _segments = new();

    [ObservableProperty]
    private ObservableCollection<DayChartItem> _weekDaysChart = new();

    [ObservableProperty]
    private string _totalFocusedTimeStr = "0 h 00 m";

    [ObservableProperty]
    private string _weekComparisonStr = "0% per week";

    [ObservableProperty]
    private string _weekComparisonColor = "#9CA3AF";

    [ObservableProperty]
    private string _completedTasksRatio = "0 / 0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TasksProgressLabel))]
    private double _tasksProgressPercentage;

    public string TasksProgressLabel =>
        $"{LocalizationService.Instance["Done"]}: {(int)Math.Round(TasksProgressPercentage)}%";

    // Карточка "Отклонение" (заменяет "Продуктивность")
    [ObservableProperty]
    private string _deviationStr = "—";

    [ObservableProperty]
    private string _deviationColor = "#9CA3AF";

    [ObservableProperty]
    private string _deviationSubLabel = "—";

    [ObservableProperty]
    private double _totalHours;

    public AnalyticsViewModel(IDatabaseService db)
    {
        _db = db;

        int diff = (7 + (DateTime.Today.DayOfWeek - DayOfWeek.Monday)) % 7;
        WeekStart = DateTime.Today.AddDays(-diff);

        LoadData();
    }

    partial void OnWeekStartChanged(DateTime value)
    {
        OnPropertyChanged(nameof(WeekTitle));
        LoadData();
    }

    public string WeekTitle =>
        $"{_localization["Week"]} {WeekStart:dd.MM.yyyy}";

    [RelayCommand]
    private void PreviousWeek()
    {
        WeekStart = WeekStart.AddDays(-7);
    }

    [RelayCommand]
    private void NextWeek()
    {
        WeekStart = WeekStart.AddDays(7);
    }

    [RelayCommand]
    private void CloseWindow(Avalonia.Controls.Window window)
    {
        window?.Close();
    }

    private void LoadData()
    {
        var weekEnd  = WeekStart.AddDays(7);
        var sessions = _db.GetSessionsForPeriod(WeekStart, weekEnd).ToList();
        var tasks    = _db.GetTasksForPeriod(WeekStart, weekEnd).ToList();

        LoadFocusStatistics(sessions);
        LoadTasksStatistics(tasks);
        LoadDeviationStatistics(tasks, sessions);
        LoadWeekChart(sessions);
        LoadProjectSegments(sessions);
    }

    private void LoadFocusStatistics(List<FocusSession> sessions)
    {
        // Только завершённые сессии, только фактические минуты; ограничиваем физическим максимумом
        int totalMinutes = sessions
            .Where(s => s.IsCompleted)
            .Sum(s => s.ActualMinutes);

        totalMinutes = Math.Min(totalMinutes, MaxWeekMinutes);
        TotalHours = totalMinutes / 60.0;

        TotalFocusedTimeStr =
            $"{totalMinutes / 60} {_localization["HoursShort"]} " +
            $"{totalMinutes % 60:D2} {_localization["MinutesShort"]}";

        // Сравнение с предыдущей неделей
        var prevStart    = WeekStart.AddDays(-7);
        int prevMinutes  = _db.GetSessionsForPeriod(prevStart, WeekStart)
            .Where(s => s.IsCompleted)
            .Sum(s => s.ActualMinutes);
        prevMinutes = Math.Min(prevMinutes, MaxWeekMinutes);

        if (prevMinutes <= 0)
        {
            WeekComparisonStr   = $"0% {_localization["PerWeek"]}";
            WeekComparisonColor = "#9CA3AF";
            return;
        }

        double diffPct = ((double)(totalMinutes - prevMinutes) / prevMinutes) * 100;

        if (diffPct >= 0)
        {
            WeekComparisonStr   = $"↑ {Math.Round(diffPct)}% {_localization["PerWeek"]}";
            WeekComparisonColor = "#10B981";
        }
        else
        {
            WeekComparisonStr   = $"↓ {Math.Abs(Math.Round(diffPct))}% {_localization["PerWeek"]}";
            WeekComparisonColor = "#EF4444";
        }
    }

    private void LoadTasksStatistics(List<TaskItem> tasks)
    {
        int total     = tasks.Count;
        int completed = tasks.Count(t => t.IsCompleted);

        CompletedTasksRatio     = $"{completed} / {total}";
        TasksProgressPercentage = total > 0
            ? ((double)completed / total) * 100
            : 0;
    }

    private void LoadDeviationStatistics(List<TaskItem> tasks, List<FocusSession> sessions)
    {
        // План vs Факт — только за сегодня (независимо от выбранной недели в навигации)
        var today = DateTime.Today;
        int planMinutes = _db.GetTasksByDate(today).Sum(t => t.PlannedDurationMinutes);
        int factMinutes = _db.GetSessionsForDate(today)
            .Where(s => s.IsCompleted)
            .Sum(s => s.ActualMinutes);
        factMinutes = Math.Min(factMinutes, MaxWeekMinutes);

        if (planMinutes == 0)
        {
            DeviationStr      = "—";
            DeviationColor    = "#9CA3AF";
            DeviationSubLabel = _localization["DeviationSubNoPlan"];
            return;
        }

        double pct = ((double)(factMinutes - planMinutes) / planMinutes) * 100;
        // Ограничиваем диапазон отображения ±999%
        pct = Math.Max(-999, Math.Min(999, pct));

        if (pct >= 0)
        {
            DeviationStr      = $"+{Math.Round(pct)}%";
            DeviationColor    = "#10B981";
            DeviationSubLabel = _localization["DeviationSubOnTrack"];
        }
        else
        {
            DeviationStr      = $"{Math.Round(pct)}%";
            DeviationColor    = "#EF4444";
            DeviationSubLabel = _localization["DeviationSubUnder"];
        }
    }

    private void LoadWeekChart(List<FocusSession> sessions)
    {
        WeekDaysChart.Clear();

        var dayNames = new[]
        {
            _localization["MonShort"],
            _localization["TueShort"],
            _localization["WedShort"],
            _localization["ThuShort"],
            _localization["FriShort"],
            _localization["SatShort"],
            _localization["SunShort"]
        };

        int[] minutesByDay = new int[7];
        for (int i = 0; i < 7; i++)
        {
            var dayDate = WeekStart.AddDays(i).Date;
            minutesByDay[i] = sessions
                .Where(s => s.IsCompleted && s.StartTime.Date == dayDate)
                .Sum(s => s.ActualMinutes);
        }

        double maxMinutes = minutesByDay.Max();

        for (int i = 0; i < 7; i++)
        {
            double hours  = minutesByDay[i] / 60.0;
            double height = maxMinutes > 0
                ? (minutesByDay[i] / maxMinutes) * 140.0
                : 10.0;

            bool isToday = WeekStart.AddDays(i).Date == DateTime.Today;

            WeekDaysChart.Add(new DayChartItem
            {
                DayName     = dayNames[i],
                Hours       = Math.Round(hours, 1),
                HeightRatio = Math.Max(10, height),
                Color       = isToday ? "#2563EB" : "#93C5FD"
            });
        }
    }

    private void LoadProjectSegments(List<FocusSession> sessions)
    {
        Segments.Clear();

        var projectGroups = new Dictionary<string, double>();

        foreach (var session in sessions.Where(s => s.IsCompleted && s.TaskId.HasValue))
        {
            var task = _db.GetTask(session.TaskId!.Value);

            string projectName;
            if (task == null || !task.ProjectId.HasValue || task.ProjectId.Value <= 0)
            {
                projectName = _localization["NoProject"];
            }
            else
            {
                var proj = _db.GetAllProjects().FirstOrDefault(p => p.Id == task.ProjectId.Value);
                projectName = proj?.Name ?? _localization["NoProject"];
            }

            if (projectGroups.ContainsKey(projectName))
                projectGroups[projectName] += session.ActualMinutes;
            else
                projectGroups[projectName] = session.ActualMinutes;
        }

        double totalHoursSum = projectGroups.Sum(x => x.Value) / 60.0;

        var colors = new[] { "#2563EB", "#F59E0B", "#10B981", "#8B5CF6", "#EC4899" };
        int colorIndex = 0;

        foreach (var group in projectGroups.OrderByDescending(g => g.Value))
        {
            double hours = group.Value / 60.0;
            int h = (int)hours;
            int m = (int)((hours - h) * 60);

            Segments.Add(new AnalyticsSegment
            {
                Label         = group.Key,
                Value         = Math.Round(hours, 1),
                TimeFormatted = $"{h} {_localization["HoursShort"]} {m:D2} {_localization["MinutesShort"]}",
                Percentage    = totalHoursSum > 0 ? (hours / totalHoursSum) * 100.0 : 0,
                Color         = colors[colorIndex++ % colors.Length]
            });
        }
    }
}
