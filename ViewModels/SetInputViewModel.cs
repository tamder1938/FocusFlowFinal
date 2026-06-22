using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Workout;
using System;

namespace FocusFlowFinal.ViewModels;

public partial class SetInputViewModel : ObservableObject
{
    [ObservableProperty] private int     _setNumber;
    [ObservableProperty] private decimal _weightKg   = 20m;
    [ObservableProperty] private decimal _reps       = 10m;
    [ObservableProperty] private int     _durationSec = 60;
    [ObservableProperty] private bool    _isCompleted;

    public ExerciseMetric Metric { get; }

    public bool ShowWeight   => Metric == ExerciseMetric.WeightReps;
    public bool ShowReps     => Metric is ExerciseMetric.WeightReps
                                       or ExerciseMetric.RepsOnly
                                       or ExerciseMetric.TimeReps;
    public bool ShowDuration => Metric is ExerciseMetric.TimeOnly
                                       or ExerciseMetric.TimeReps;

    public SetInputViewModel(ExerciseMetric metric = ExerciseMetric.WeightReps)
    {
        Metric = metric;
    }

    public event EventHandler? CompleteRequested;

    [RelayCommand]
    private void Complete()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        CompleteRequested?.Invoke(this, EventArgs.Empty);
    }

    public PerformedSet ToModel() => new()
    {
        SetNumber   = SetNumber,
        WeightKg    = ShowWeight   ? (double)WeightKg : 0,
        Reps        = ShowReps     ? (int)Reps        : 0,
        DurationSec = ShowDuration ? DurationSec      : 0,
        IsCompleted = IsCompleted,
        PerformedAt = IsCompleted ? DateTime.Now : default
    };
}
