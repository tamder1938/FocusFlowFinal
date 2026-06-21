using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.YearStats;
using FocusFlowFinal.Services;
using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;

namespace FocusFlowFinal.ViewModels;

public partial class DayStatsViewModel : ObservableObject
{
    private readonly LocalizationService _loc = LocalizationService.Instance;

    [ObservableProperty] private string _dateTitle   = string.Empty;
    [ObservableProperty] private bool   _hasAnyData;

    // Tasks
    [ObservableProperty] private bool   _hasTasks;
    [ObservableProperty] private string _tasksLabel = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _completedTasks = new();

    // Habits
    [ObservableProperty] private bool   _hasHabits;
    [ObservableProperty] private string _habitsLabel = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _doneHabits    = new();
    [ObservableProperty] private ObservableCollection<string> _skippedHabits = new();

    // Events
    [ObservableProperty] private bool   _hasEvents;
    [ObservableProperty] private string _eventsLabel = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _events = new();

    // Pomodoro
    [ObservableProperty] private bool   _hasPomodoro;
    [ObservableProperty] private string _pomodoroLabel = string.Empty;

    // Workout
    [ObservableProperty] private bool   _hasWorkout;
    [ObservableProperty] private string _workoutLabel = string.Empty;

    // Mood
    [ObservableProperty] private bool   _hasMood;
    [ObservableProperty] private string _moodLabel  = string.Empty;
    [ObservableProperty] private string _moodColor  = "#9CA3AF";

    // Note
    [ObservableProperty] private bool   _hasNote;
    [ObservableProperty] private string _noteLabel = string.Empty;

    private static readonly string[] MoodLabels = { "Ужасно","Плохо","Нейтрально","Хорошо","Супер" };
    private static readonly string[] MoodColors = { "#EF4444","#F97316","#EAB308","#22C55E","#10B981" };

    public DayStatsViewModel(DayActivityData data)
    {
        DateTitle = data.Date.ToString("d MMMM yyyy");

        // Tasks
        HasTasks  = data.CompletedTasks.Count > 0;
        TasksLabel = $"{data.CompletedTasks.Count} {_loc["DayStats_Done"]}";
        foreach (var t in data.CompletedTasks) CompletedTasks.Add("✓ " + t);

        // Habits
        HasHabits   = data.TotalHabits > 0;
        HabitsLabel = $"{data.DoneHabits.Count} {_loc["DayStats_Of"]} {data.TotalHabits}";
        foreach (var h in data.DoneHabits)    DoneHabits.Add("✓ " + h);
        foreach (var h in data.SkippedHabits) SkippedHabits.Add("✗ " + h);

        // Events
        HasEvents   = data.Events.Count > 0;
        EventsLabel = data.Events.Count.ToString();
        foreach (var e in data.Events) Events.Add(e);

        // Pomodoro
        HasPomodoro   = data.PomodoroCount > 0;
        int h2        = data.PomodoroTotalMin / 60;
        int m2        = data.PomodoroTotalMin % 60;
        PomodoroLabel = $"{data.PomodoroCount} {_loc["DayStats_Sessions"]} · {h2}:{m2:D2}";

        // Workout
        HasWorkout   = data.HasWorkout;
        double tons  = Math.Round(data.WorkoutTonnageKg / 1000.0, 2);
        WorkoutLabel = $"{data.WorkoutName} · {data.WorkoutExercises} {_loc["DayStats_Exercises"]}"
                     + (tons > 0 ? $" · {tons} т" : "");

        // Mood
        HasMood = data.MoodLevel.HasValue;
        if (data.MoodLevel.HasValue)
        {
            int idx   = Math.Clamp(data.MoodLevel.Value - 1, 0, 4);
            MoodLabel = MoodLabels[idx];
            MoodColor = MoodColors[idx];
        }

        // Note
        HasNote   = data.HasNote;
        NoteLabel = data.NotePreview;

        HasAnyData = HasTasks || HasHabits || HasEvents || HasPomodoro || HasWorkout || HasMood || HasNote;
    }

    [RelayCommand]
    private void CloseWindow(Window? win) => win?.Close();
}
