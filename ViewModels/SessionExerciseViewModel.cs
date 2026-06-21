using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Workout;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public partial class SessionExerciseViewModel : ObservableObject
{
    public string ExerciseKey        { get; }
    public string ExerciseName       { get; }
    public string ImageEmoji         { get; }
    public int    DefaultRestSeconds { get; }

    [ObservableProperty] private bool _isExpanded = true;

    public ObservableCollection<SetInputViewModel> Sets { get; } = new();

    public string CompletedSetsLabel =>
        $"{Sets.Count(s => s.IsCompleted)}/{Sets.Count} подходов";

    public event EventHandler<SetInputViewModel>? SetCompleted;

    public SessionExerciseViewModel(
        Exercise exercise, int defaultSets = 3, int defaultReps = 10, int restSec = 90)
    {
        ExerciseKey        = exercise.Key;
        ExerciseName       = exercise.Name;
        ImageEmoji         = exercise.ImageEmoji;
        DefaultRestSeconds = restSec;

        for (int i = 0; i < Math.Max(1, defaultSets); i++)
            AddSetCore(defaultReps);
    }

    [RelayCommand]
    private void AddSet()
        => AddSetCore((int)(Sets.LastOrDefault()?.Reps ?? 10m));

    [RelayCommand]
    private void RemoveLastSet()
    {
        if (Sets.Count == 0) return;
        Sets.RemoveAt(Sets.Count - 1);
        OnPropertyChanged(nameof(CompletedSetsLabel));
    }

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;

    private void AddSetCore(int reps)
    {
        var weight = Sets.LastOrDefault()?.WeightKg ?? 20m;
        var set    = new SetInputViewModel
        {
            SetNumber = Sets.Count + 1,
            Reps      = reps,
            WeightKg  = weight
        };
        set.PropertyChanged   += OnSetPropertyChanged;
        set.CompleteRequested += OnSetCompleteRequested;
        Sets.Add(set);
        OnPropertyChanged(nameof(CompletedSetsLabel));
    }

    private void OnSetPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SetInputViewModel.IsCompleted))
            OnPropertyChanged(nameof(CompletedSetsLabel));
    }

    private void OnSetCompleteRequested(object? sender, EventArgs e)
    {
        if (sender is SetInputViewModel set)
            SetCompleted?.Invoke(this, set);
    }

    public PerformedExercise ToModel() => new()
    {
        ExerciseKey  = ExerciseKey,
        ExerciseName = ExerciseName,
        Sets         = Sets.Select(s => s.ToModel()).ToList()
    };
}
