using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Workout;
using FocusFlowFinal.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public partial class ActiveSessionViewModel : ObservableObject, IDisposable
{
    private readonly IWorkoutRepository _repo;
    private readonly WorkoutProgram     _program;
    private readonly WorkoutDay         _day;
    private readonly DispatcherTimer    _elapsedTimer;

    public string   ProgramName => _program.Name;
    public string   ProgramIcon => _program.Icon;
    public string   DayName     => _day.Name;
    public DateTime StartedAt   { get; } = DateTime.Now;

    [ObservableProperty] private string _elapsedLabel = "00:00:00";

    public RestTimerViewModel RestTimer { get; } = new();

    public ObservableCollection<SessionExerciseViewModel> Exercises { get; } = new();

    public event EventHandler? SessionFinished;

    public ActiveSessionViewModel(
        WorkoutProgram      program,
        WorkoutDay          day,
        IWorkoutRepository  repo,
        IExerciseRepository exerciseRepo)
    {
        _program = program;
        _day     = day;
        _repo    = repo;

        foreach (var planned in day.PlannedExercises.OrderBy(e => e.Order))
        {
            var ex = exerciseRepo.GetByKey(planned.ExerciseKey);
            if (ex == null) continue;
            AddExerciseCore(ex, planned.Sets, planned.Reps, planned.RestSeconds);
        }

        _elapsedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _elapsedTimer.Tick += (_, _) => UpdateElapsed();
        _elapsedTimer.Start();
    }

    public void AddExercise(Exercise exercise)
    {
        if (Exercises.Any(e => e.ExerciseKey == exercise.Key)) return;
        AddExerciseCore(exercise, 3, 10, 90);
    }

    private void AddExerciseCore(Exercise ex, int sets, int reps, int restSec)
    {
        var vm = new SessionExerciseViewModel(ex, sets, reps, restSec);
        vm.SetCompleted += (_, _) => RestTimer.Start(vm.DefaultRestSeconds);
        Exercises.Add(vm);
    }

    private void UpdateElapsed()
    {
        var e = DateTime.Now - StartedAt;
        ElapsedLabel = $"{(int)e.TotalHours:D2}:{e.Minutes:D2}:{e.Seconds:D2}";
    }

    [RelayCommand]
    private void Finish()
    {
        _elapsedTimer.Stop();
        RestTimer.Stop();

        var session = new WorkoutSession
        {
            ProgramId  = _program.Id,
            DayNumber  = _day.DayNumber,
            DayName    = _day.Name,
            StartedAt  = StartedAt,
            FinishedAt = DateTime.Now,
            Exercises  = Exercises.Select(e => e.ToModel()).ToList()
        };

        _repo.UpsertSession(session);
        SessionFinished?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Abandon()
    {
        _elapsedTimer.Stop();
        RestTimer.Stop();
        SessionFinished?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _elapsedTimer.Stop();
        RestTimer.Dispose();
    }
}
