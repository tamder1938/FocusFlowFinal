using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Workout;
using FocusFlowFinal.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

// ── Элемент дня в редакторе программы ──────────────────────────────────────

public partial class DayItemViewModel : ObservableObject
{
    public WorkoutDay Source { get; }

    [ObservableProperty] private int    _dayNumber;
    [ObservableProperty] private string _dayName = string.Empty;

    public ObservableCollection<MuscleGroupItem> MuscleItems { get; } = new();

    public DayItemViewModel(WorkoutDay day)
    {
        Source     = day;
        _dayNumber = day.DayNumber;
        _dayName   = day.Name;

        foreach (var g in Enum.GetValues<MuscleGroup>())
            MuscleItems.Add(new MuscleGroupItem(g)
                { IsSelected = day.TargetMuscles.Contains(g) });
    }

    public WorkoutDay ToModel(int number) => new()
    {
        DayNumber        = number,
        Name             = DayName.Trim().Length > 0 ? DayName.Trim() : $"День {number}",
        TargetMuscles    = MuscleItems.Where(m => m.IsSelected).Select(m => m.Group).ToList(),
        PlannedExercises = Source.PlannedExercises
    };
}

// ── ViewModel диалога программы ────────────────────────────────────────────

public partial class AddProgramViewModel : ObservableObject
{
    private readonly IWorkoutRepository _repo;
    private readonly WorkoutProgram?    _existing;

    [ObservableProperty] private string _name         = string.Empty;
    [ObservableProperty] private string _icon         = "🏋️";
    [ObservableProperty] private string _notes        = string.Empty;
    [ObservableProperty] private string _color        = "#3B82F6";
    [ObservableProperty] private string _errorMessage = string.Empty;

    public ObservableCollection<DayItemViewModel> Days { get; } = new();

    public bool   IsEdit      => _existing != null;
    public string DialogTitle => IsEdit ? "Редактировать программу" : "Новая программа";

    public bool Saved { get; private set; }
    public event EventHandler? CloseRequested;

    // Предустановленные цвета
    public static readonly string[] PresetColors =
    {
        "#3B82F6", "#8B5CF6", "#10B981", "#F59E0B",
        "#EF4444", "#EC4899", "#14B8A6", "#F97316"
    };

    public AddProgramViewModel(IWorkoutRepository repo, WorkoutProgram? existing = null)
    {
        _repo     = repo;
        _existing = existing;

        if (existing != null)
        {
            Name  = existing.Name;
            Icon  = existing.Icon;
            Notes = existing.Notes;
            Color = existing.Color;
            foreach (var d in existing.Days.OrderBy(d => d.DayNumber))
                Days.Add(new DayItemViewModel(d));
        }

        if (Days.Count == 0) AddDayCore();
    }

    // ── Команды ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddDay() => AddDayCore();

    private void AddDayCore()
    {
        var day = new WorkoutDay
        {
            DayNumber        = Days.Count + 1,
            Name             = $"День {Days.Count + 1}",
            TargetMuscles    = new(),
            PlannedExercises = new()
        };
        Days.Add(new DayItemViewModel(day));
    }

    [RelayCommand]
    private void RemoveDay(DayItemViewModel item)
    {
        Days.Remove(item);
        for (int i = 0; i < Days.Count; i++)
            Days[i].DayNumber = i + 1;
    }

    [RelayCommand]
    private void SelectColor(string hex) => Color = hex;

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Введите название программы";
            return;
        }

        var program = _existing ?? new WorkoutProgram { CreatedAt = DateTime.Now };
        program.Name  = Name.Trim();
        program.Icon  = string.IsNullOrEmpty(Icon) ? "🏋️" : Icon;
        program.Notes = Notes.Trim();
        program.Color = Color;
        program.Days  = Days.Select((d, i) => d.ToModel(i + 1)).ToList();

        _repo.UpsertProgram(program);
        Saved = true;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
