using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Workout;
using System;

namespace FocusFlowFinal.ViewModels;

public partial class SetInputViewModel : ObservableObject
{
    [ObservableProperty] private int     _setNumber;
    [ObservableProperty] private decimal _weightKg = 20m;
    [ObservableProperty] private decimal _reps     = 10m;
    [ObservableProperty] private bool    _isCompleted;

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
        WeightKg    = (double)WeightKg,
        Reps        = (int)Reps,
        IsCompleted = IsCompleted,
        PerformedAt = IsCompleted ? DateTime.Now : default
    };
}
