
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
    private double _tasksProgressPercentage;

    [ObservableProperty]
    private double _productivityPercentage;

    [ObservableProperty]
    private string _productivityLevel = string.Empty;

    [ObservableProperty]
    private string _productivityColor = "#EF4444";

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

    public string CurrentWeekText =>
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
        var end = WeekStart.AddDays(7);

        var currentWeekSessions = _db
            .GetSessionsForPeriod(WeekStart, end)
            .ToList();

        var currentWeekTasks = _db
            .GetTasksForPeriod(WeekStart, end)
            .ToList();

        LoadFocusStatistics(currentWeekSessions);
        LoadTasksStatistics(currentWeekTasks);
        LoadProductivityStatistics(currentWeekSessions, currentWeekTasks);
        LoadWeekChart(currentWeekSessions);
        LoadProjectSegments(currentWeekSessions);
    }

    private void LoadFocusStatistics(List<FocusSession> sessions)
    {
        int totalMinutes = sessions.Sum(s =>
            s.ActualMinutes > 0
                ? s.ActualMinutes
                : s.PlannedMinutes);

        TotalHours = totalMinutes / 60.0;

        TotalFocusedTimeStr =
            $"{totalMinutes / 60} {_localization["HoursShort"]} " +
            $"{totalMinutes % 60:D2} {_localization["MinutesShort"]}";

        var prevWeekStart = WeekStart.AddDays(-7);

        var prevWeekSessions = _db
            .GetSessionsForPeriod(prevWeekStart, WeekStart)
            .ToList();

        int prevTotalMinutes = prevWeekSessions.Sum(s =>
            s.ActualMinutes > 0
                ? s.ActualMinutes
                : s.PlannedMinutes);

        if (prevTotalMinutes <= 0)
        {
            WeekComparisonStr = $"0% {_localization["PerWeek"]}";
            WeekComparisonColor = "#9CA3AF";
            return;
        }

        double diffPct =
            ((double)(totalMinutes - prevTotalMinutes) / prevTotalMinutes) * 100;

        if (diffPct >= 0)
        {
            WeekComparisonStr =
                $"↑ {Math.Round(diffPct)}% {_localization["PerWeek"]}";

            WeekComparisonColor = "#10B981";
        }
        else
        {
            WeekComparisonStr =
                $"↓ {Math.Abs(Math.Round(diffPct))}% {_localization["PerWeek"]}";

            WeekComparisonColor = "#EF4444";
        }
    }

    private void LoadTasksStatistics(List<TaskItem> tasks)
    {
        int totalTasksCount = tasks.Count;
        int completedTasksCount = tasks.Count(t => t.IsCompleted);

        CompletedTasksRatio = $"{completedTasksCount} / {totalTasksCount}";

        TasksProgressPercentage =
            totalTasksCount > 0
                ? ((double)completedTasksCount / totalTasksCount) * 100
                : 0;
    }

    private void LoadProductivityStatistics(
        List<FocusSession> sessions,
        List<TaskItem> tasks)
    {
        int totalMinutes = sessions.Sum(s =>
            s.ActualMinutes > 0
                ? s.ActualMinutes
                : s.PlannedMinutes);

        ProductivityPercentage =
            tasks.Count > 0
                ? (TasksProgressPercentage * 0.6) +
                  (Math.Min(100, (totalMinutes / 1200.0) * 100) * 0.4)
                : 0;

        ProductivityPercentage =
            Math.Min(100, Math.Round(ProductivityPercentage));

        if (ProductivityPercentage >= 75)
        {
            ProductivityLevel = _localization["HighLevel"];
            ProductivityColor = "#10B981";
        }
        else if (ProductivityPercentage >= 40)
        {
            ProductivityLevel = _localization["MediumLevel"];
            ProductivityColor = "#F59E0B";
        }
        else
        {
            ProductivityLevel = _localization["LowLevel"];
            ProductivityColor = "#EF4444";
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
                .Where(s => s.StartTime.Date == dayDate)
                .Sum(s =>
                    s.ActualMinutes > 0
                        ? s.ActualMinutes
                        : s.PlannedMinutes);
        }

        double maxMinutes = minutesByDay.Max();

        for (int i = 0; i < 7; i++)
        {
            double hours = minutesByDay[i] / 60.0;

            double height = maxMinutes > 0
                ? (minutesByDay[i] / maxMinutes) * 140.0
                : 10.0;

            bool isToday =
                WeekStart.AddDays(i).Date == DateTime.Today;

            WeekDaysChart.Add(new DayChartItem
            {
                DayName = dayNames[i],
                Hours = Math.Round(hours, 1),
                HeightRatio = Math.Max(10, height),
                Color = isToday ? "#2563EB" : "#93C5FD"
            });
        }
    }

    private void LoadProjectSegments(List<FocusSession> sessions)
    {
        Segments.Clear();

        var projectGroups = new Dictionary<string, double>();

        foreach (var session in sessions.Where(s => s.TaskId.HasValue))
        {
            var task = _db.GetTask(session.TaskId.Value);

            string projectName =
                task == null || string.IsNullOrWhiteSpace(task.Project)
                    ? _localization["NoProject"]
                    : task.Project;

            int actualMinutes =
                session.ActualMinutes > 0
                    ? session.ActualMinutes
                    : session.PlannedMinutes;

            if (projectGroups.ContainsKey(projectName))
            {
                projectGroups[projectName] += actualMinutes;
            }
            else
            {
                projectGroups[projectName] = actualMinutes;
            }
        }

        double totalHoursSum =
            projectGroups.Sum(x => x.Value) / 60.0;

        var colors = new[]
        {
            "#2563EB",
            "#F59E0B",
            "#10B981",
            "#8B5CF6",
            "#EC4899"
        };

        int colorIndex = 0;

        foreach (var group in projectGroups.OrderByDescending(g => g.Value))
        {
            double hours = group.Value / 60.0;

            int h = (int)hours;
            int m = (int)((hours - h) * 60);

            Segments.Add(new AnalyticsSegment
            {
                Label = group.Key,
                Value = Math.Round(hours, 1),
                TimeFormatted =
                    $"{h} {_localization["HoursShort"]} " +
                    $"{m:D2} {_localization["MinutesShort"]}",

                Percentage = totalHoursSum > 0
                    ? (hours / totalHoursSum) * 100.0
                    : 0,

                Color = colors[colorIndex % colors.Length]
            });

            colorIndex++;
        }
    }
}
