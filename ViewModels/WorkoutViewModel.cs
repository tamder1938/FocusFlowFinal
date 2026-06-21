using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Workout;
using FocusFlowFinal.Services;
using System;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class WorkoutViewModel : ObservableObject
{
    private readonly IExerciseRepository _exercises;
    private readonly IWorkoutRepository  _workouts;
    private readonly IWorkoutInitService _initService;

    // ── Левая колонка ──────────────────────────────────────────────────
    public WorkoutProgramListViewModel  ProgramListVm { get; }
    public SessionHistoryViewModel      HistoryVm     { get; }
    public WorkoutAnalyticsViewModel    AnalyticsVm   { get; }
    public IWorkoutRepository           WorkoutRepo   => _workouts;

    // ── Правая колонка ─────────────────────────────────────────────────
    public ExerciseListViewModel ExerciseListVm { get; }

    // ── Центральная колонка: состояния ─────────────────────────────────
    public bool HasSelectedProgram  => ProgramListVm.SelectedProgram != null;

    [ObservableProperty] private ActiveSessionViewModel? _activeSessionVm;

    public bool IsSessionActive        => ActiveSessionVm != null;
    public bool IsNotSessionActive     => ActiveSessionVm == null;
    public bool ShowProgramPlaceholder => !HasSelectedProgram && !IsSessionActive;
    public bool ShowProgramDetail      => HasSelectedProgram  && !IsSessionActive;

    // ── Вкладка правой колонки ─────────────────────────────────────────
    [ObservableProperty] private int _rightTabIndex = 0;

    public WorkoutViewModel(
        IExerciseRepository exercises,
        IWorkoutRepository  workouts,
        IWorkoutInitService initService)
    {
        _exercises   = exercises;
        _workouts    = workouts;
        _initService = initService;

        ProgramListVm  = new WorkoutProgramListViewModel(_workouts);
        HistoryVm      = new SessionHistoryViewModel(_workouts);
        AnalyticsVm    = new WorkoutAnalyticsViewModel(_workouts);
        ExerciseListVm = new ExerciseListViewModel(_exercises);

        ProgramListVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkoutProgramListViewModel.SelectedProgram))
                RefreshCenterFlags();
        };
    }

    public async Task InitializeAsync()
    {
        await _initService.EnsureSeededAsync();
        ExerciseListVm.Refresh();
        ProgramListVm.Refresh();
    }

    // ── Флаги центральной колонки ──────────────────────────────────────

    partial void OnActiveSessionVmChanged(ActiveSessionViewModel? value)
        => RefreshCenterFlags();

    private void RefreshCenterFlags()
    {
        OnPropertyChanged(nameof(HasSelectedProgram));
        OnPropertyChanged(nameof(IsSessionActive));
        OnPropertyChanged(nameof(IsNotSessionActive));
        OnPropertyChanged(nameof(ShowProgramPlaceholder));
        OnPropertyChanged(nameof(ShowProgramDetail));
    }

    // ── Запуск / завершение сессии ─────────────────────────────────────

    [RelayCommand]
    private void StartSession(WorkoutDay day)
    {
        if (ProgramListVm.SelectedProgram == null) return;

        var vm = new ActiveSessionViewModel(
            ProgramListVm.SelectedProgram.Program, day, _workouts, _exercises);
        vm.SessionFinished += OnSessionFinished;
        ActiveSessionVm = vm;
    }

    private void OnSessionFinished(object? sender, EventArgs e)
    {
        if (ActiveSessionVm != null)
        {
            ActiveSessionVm.SessionFinished -= OnSessionFinished;
            ActiveSessionVm.Dispose();
        }
        ActiveSessionVm = null;
        ProgramListVm.Refresh();
        HistoryVm.Refresh();
        AnalyticsVm.Refresh();
    }

    // ── Добавить упражнение в активную сессию ──────────────────────────

    [RelayCommand]
    private void AddExerciseToSession(Exercise exercise)
        => ActiveSessionVm?.AddExercise(exercise);
}
