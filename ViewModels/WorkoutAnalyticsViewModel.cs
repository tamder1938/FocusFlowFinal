using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Workout;
using FocusFlowFinal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public class BarChartItem
{
    public string Label     { get; init; } = "";
    public double Value     { get; init; }
    public double BarHeight { get; init; }
    public string Tooltip   { get; init; } = "";
    public bool   HasValue  => Value > 0;
}

public class ExerciseStatItem
{
    public string ExerciseKey    { get; init; } = "";
    public string ExerciseName   { get; init; } = "";
    public double TotalTonnage   { get; init; }
    public string TonnageLabel   { get; init; } = "";
    public string MaxWeightLabel { get; init; } = "";
    public int    SessionCount   { get; init; }
    public double BarWidth       { get; init; }
}

public partial class WorkoutAnalyticsViewModel : ObservableObject
{
    private readonly IWorkoutRepository _repo;
    private const double MaxBarH = 100.0;
    private const double MaxBarW = 110.0;

    [ObservableProperty] private int    _totalSessions;
    [ObservableProperty] private string _totalTonnageLabel = "—";
    [ObservableProperty] private string _avgDurationLabel  = "—";
    [ObservableProperty] private string _bestSessionLabel  = "—";
    [ObservableProperty] private bool   _isEmpty           = true;

    public ObservableCollection<BarChartItem>    TonnageByDay    { get; } = new();
    public ObservableCollection<ExerciseStatItem> TopExercises   { get; } = new();

    [ObservableProperty] private string? _focusExerciseName;
    [ObservableProperty] private bool    _hasFocusExercise;
    public ObservableCollection<BarChartItem> ExerciseProgress   { get; } = new();

    public WorkoutAnalyticsViewModel(IWorkoutRepository repo)
    {
        _repo = repo;
        Refresh();
    }

    public void Refresh()
    {
        FocusExerciseName = null;
        HasFocusExercise  = false;
        ExerciseProgress.Clear();

        var sessions = _repo.GetRecentSessions(300).ToList();
        IsEmpty       = sessions.Count == 0;
        TotalSessions = sessions.Count;

        if (sessions.Count == 0)
        {
            TonnageByDay.Clear();
            TopExercises.Clear();
            TotalTonnageLabel = "—";
            AvgDurationLabel  = "—";
            BestSessionLabel  = "—";
            return;
        }

        var totalT = sessions.Sum(s => s.TotalTonnage);
        TotalTonnageLabel = Fmt(totalT);

        var done = sessions.Where(s => s.FinishedAt.HasValue).ToList();
        if (done.Any())
        {
            var avgMin = done.Average(s => (s.FinishedAt!.Value - s.StartedAt).TotalMinutes);
            AvgDurationLabel = $"~{(int)avgMin} мин";

            var best = sessions.OrderByDescending(s => s.TotalTonnage).First();
            BestSessionLabel = $"{Fmt(best.TotalTonnage)} ({best.StartedAt:d MMM})";
        }

        BuildTonnageByDay(sessions);
        BuildTopExercises(sessions);
    }

    private void BuildTonnageByDay(List<WorkoutSession> sessions)
    {
        TonnageByDay.Clear();
        var today = DateTime.Today;

        var byDay = sessions
            .Where(s => s.StartedAt.Date >= today.AddDays(-13))
            .GroupBy(s => s.StartedAt.Date)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.TotalTonnage));

        var days   = Enumerable.Range(0, 14).Select(i => today.AddDays(-13 + i)).ToList();
        var values = days.Select(d => byDay.TryGetValue(d, out var v) ? v : 0.0).ToList();
        var maxVal = values.Max();

        for (int i = 0; i < days.Count; i++)
        {
            var t = values[i];
            TonnageByDay.Add(new BarChartItem
            {
                Label     = days[i].Day.ToString(),
                Value     = t,
                BarHeight = maxVal > 0 && t > 0 ? Math.Max(4, MaxBarH * (t / maxVal)) : 0,
                Tooltip   = t > 0 ? $"{days[i]:d MMM}: {Fmt(t)}" : days[i].ToString("d MMM")
            });
        }
    }

    private void BuildTopExercises(List<WorkoutSession> sessions)
    {
        TopExercises.Clear();

        var stats = sessions
            .SelectMany(s => s.Exercises)
            .GroupBy(e => e.ExerciseKey)
            .Select(g =>
            {
                var completedSets = g.SelectMany(e => e.Sets.Where(s => s.IsCompleted)).ToList();
                var tonnage = completedSets.Sum(s => s.WeightKg * s.Reps);
                var maxW    = completedSets.Any() ? completedSets.Max(s => s.WeightKg) : 0.0;
                var cnt     = sessions.Count(s => s.Exercises.Any(e => e.ExerciseKey == g.Key));
                return new
                {
                    Key      = g.Key,
                    Name     = g.First().ExerciseName,
                    Tonnage  = tonnage,
                    MaxW     = maxW,
                    SessCount= cnt
                };
            })
            .Where(x => x.Tonnage > 0)
            .OrderByDescending(x => x.Tonnage)
            .Take(8)
            .ToList();

        var maxT = stats.Any() ? stats[0].Tonnage : 1.0;

        foreach (var x in stats)
        {
            TopExercises.Add(new ExerciseStatItem
            {
                ExerciseKey    = x.Key,
                ExerciseName   = x.Name,
                TotalTonnage   = x.Tonnage,
                TonnageLabel   = Fmt(x.Tonnage),
                MaxWeightLabel = $"{x.MaxW:0.##} кг",
                SessionCount   = x.SessCount,
                BarWidth       = maxT > 0 ? Math.Max(4, MaxBarW * (x.Tonnage / maxT)) : 0
            });
        }
    }

    [RelayCommand]
    private void SelectExercise(ExerciseStatItem item)
    {
        FocusExerciseName = item.ExerciseName;
        HasFocusExercise  = true;
        ExerciseProgress.Clear();

        var sessions = _repo.GetRecentSessions(300)
            .Where(s => s.Exercises.Any(e => e.ExerciseKey == item.ExerciseKey))
            .OrderBy(s => s.StartedAt)
            .Take(10)
            .ToList();

        if (!sessions.Any()) return;

        var values = sessions.Select(s =>
        {
            var sets = s.Exercises
                .Where(e => e.ExerciseKey == item.ExerciseKey)
                .SelectMany(e => e.Sets.Where(x => x.IsCompleted))
                .ToList();
            return sets.Any() ? sets.Max(x => x.WeightKg) : 0.0;
        }).ToList();

        var maxVal = values.Any() ? values.Max() : 1.0;

        for (int i = 0; i < sessions.Count; i++)
        {
            var w = values[i];
            ExerciseProgress.Add(new BarChartItem
            {
                Label     = sessions[i].StartedAt.ToString("d.M"),
                Value     = w,
                BarHeight = maxVal > 0 && w > 0 ? Math.Max(4, MaxBarH * (w / maxVal)) : 0,
                Tooltip   = $"{w:0.##} кг"
            });
        }
    }

    [RelayCommand]
    private void ClearFocusExercise()
    {
        FocusExerciseName = null;
        HasFocusExercise  = false;
        ExerciseProgress.Clear();
    }

    private static string Fmt(double t) =>
        t >= 1000 ? $"{t / 1000:0.#} т" : $"{t:0.#} кг";
}
