using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Workout;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace FocusFlowFinal.ViewModels;

public partial class SessionHistoryItemViewModel : ObservableObject
{
    public WorkoutSession Session { get; }

    [ObservableProperty] private bool _isExpanded;

    public string DateLabel     { get; }
    public string DayLabel      { get; }
    public string DurationLabel { get; }
    public string TonnageLabel  { get; }
    public string SetsLabel     { get; }

    public IReadOnlyList<PerformedExercise> Exercises => Session.Exercises;

    public event EventHandler<WorkoutSession>? DeleteRequested;

    public SessionHistoryItemViewModel(WorkoutSession session)
    {
        Session = session;

        DateLabel     = session.StartedAt.ToString("d MMMM, HH:mm", new CultureInfo("ru-RU"));
        DayLabel      = session.DayName;
        DurationLabel = session.DurationLabel;
        TonnageLabel  = session.TonnageLabel;
        SetsLabel     = $"{session.Exercises.Count} упр. · {session.CompletedSets}/{session.TotalSets} подх.";
    }

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;

    [RelayCommand]
    private void Delete() => DeleteRequested?.Invoke(this, Session);
}
