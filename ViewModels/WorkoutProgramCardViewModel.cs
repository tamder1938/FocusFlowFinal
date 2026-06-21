using CommunityToolkit.Mvvm.ComponentModel;
using FocusFlowFinal.Models.Workout;
using System.Collections.Generic;

namespace FocusFlowFinal.ViewModels;

public partial class WorkoutProgramCardViewModel : ObservableObject
{
    public WorkoutProgram Program { get; }

    public int              Id            => Program.Id;
    public string           Name          => Program.Name;
    public string           Icon          => Program.Icon;
    public string           Color         => Program.Color;
    public string           Notes         => Program.Notes;
    public bool             IsActive      => Program.IsActive;
    public string           DaysLabel     => Program.DaysLabel;
    public int              DaysCount     => Program.Days.Count;
    public int              ExerciseCount => Program.ExerciseCount;
    public List<WorkoutDay> Days          => Program.Days;

    [ObservableProperty] private bool _isSelected;

    public WorkoutProgramCardViewModel(WorkoutProgram p) => Program = p;
}
