using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Workout;
using FocusFlowFinal.Services;
using System;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class WorkoutViewModel : ObservableObject
{
    private readonly IExerciseRepository  _exercises;
    private readonly IWorkoutRepository   _workouts;
    private readonly IWorkoutInitService  _initService;

    // ── Правая колонка ────────────────────────────────────────────────

    public ExerciseListViewModel ExerciseListVm { get; }

    // ── Вкладка правой колонки ────────────────────────────────────────
    [ObservableProperty] private int _rightTabIndex = 0;

    public WorkoutViewModel(
        IExerciseRepository exercises,
        IWorkoutRepository  workouts,
        IWorkoutInitService initService)
    {
        _exercises   = exercises;
        _workouts    = workouts;
        _initService = initService;

        ExerciseListVm = new ExerciseListViewModel(exercises);
    }

    public async Task InitializeAsync()
    {
        await _initService.EnsureSeededAsync();
        ExerciseListVm.Refresh();
    }
}
