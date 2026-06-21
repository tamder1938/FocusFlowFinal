using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Workout;
using FocusFlowFinal.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public partial class WorkoutProgramListViewModel : ObservableObject
{
    private readonly IWorkoutRepository _repo;

    public ObservableCollection<WorkoutProgramCardViewModel> Programs { get; } = new();

    [ObservableProperty] private WorkoutProgramCardViewModel? _selectedProgram;

    public event EventHandler?                    AddProgramRequested;
    public event EventHandler<WorkoutProgram>?    EditProgramRequested;
    public event EventHandler<WorkoutProgramCardViewModel>? ProgramSelectionChanged;

    public WorkoutProgramListViewModel(IWorkoutRepository repo)
    {
        _repo = repo;
        Refresh();
    }

    partial void OnSelectedProgramChanged(WorkoutProgramCardViewModel? value)
    {
        foreach (var p in Programs) p.IsSelected = false;
        if (value != null) value.IsSelected = true;
        ProgramSelectionChanged?.Invoke(this, value!);
    }

    public void Refresh()
    {
        var prevId = SelectedProgram?.Id;
        Programs.Clear();

        foreach (var p in _repo.GetPrograms()
                     .OrderByDescending(p => p.IsActive)
                     .ThenByDescending(p => p.CreatedAt))
            Programs.Add(new WorkoutProgramCardViewModel(p));

        var restored = Programs.FirstOrDefault(p => p.Id == prevId)
                    ?? Programs.FirstOrDefault(p => p.IsActive)
                    ?? Programs.FirstOrDefault();

        SelectedProgram = restored;
    }

    [RelayCommand]
    private void SelectProgram(WorkoutProgramCardViewModel vm)
        => SelectedProgram = vm;

    [RelayCommand]
    private void AddProgram()
        => AddProgramRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void EditProgram(WorkoutProgramCardViewModel vm)
        => EditProgramRequested?.Invoke(this, vm.Program);

    [RelayCommand]
    private void DeleteProgram(WorkoutProgramCardViewModel vm)
    {
        _repo.DeleteProgram(vm.Id);
        Refresh();
    }

    [RelayCommand]
    private void SetActiveProgram(WorkoutProgramCardViewModel vm)
    {
        _repo.SetActiveProgram(vm.Id);
        Refresh();
    }
}
