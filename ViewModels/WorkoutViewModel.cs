using CommunityToolkit.Mvvm.ComponentModel;
using FocusFlowFinal.Services;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class WorkoutViewModel : ObservableObject
{
    private readonly IExerciseRepository _exercises;
    private readonly IWorkoutRepository  _workouts;
    private readonly IWorkoutInitService _initService;

    // ── Левая колонка — программы ─────────────────────────────────────
    public WorkoutProgramListViewModel ProgramListVm { get; }
    public IWorkoutRepository          WorkoutRepo   => _workouts;

    // ── Правая колонка — упражнения ───────────────────────────────────
    public ExerciseListViewModel ExerciseListVm { get; }

    // ── Состояние центральной колонки ─────────────────────────────────
    public bool HasSelectedProgram => ProgramListVm.SelectedProgram != null;

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

        ProgramListVm  = new WorkoutProgramListViewModel(_workouts);
        ExerciseListVm = new ExerciseListViewModel(_exercises);

        ProgramListVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkoutProgramListViewModel.SelectedProgram))
                OnPropertyChanged(nameof(HasSelectedProgram));
        };
    }

    public async Task InitializeAsync()
    {
        await _initService.EnsureSeededAsync();
        ExerciseListVm.Refresh();
        ProgramListVm.Refresh();
    }
}
