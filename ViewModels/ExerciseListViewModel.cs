using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Workout;
using FocusFlowFinal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public partial class ExerciseListViewModel : ObservableObject
{
    private readonly IExerciseRepository _repo;
    public IExerciseRepository Repository => _repo;

    // ── Фильтры ───────────────────────────────────────────────────────

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private int    _muscleFilterIndex = 0;
    [ObservableProperty] private int    _equipmentFilterIndex = 0;

    // ── Список ────────────────────────────────────────────────────────

    public ObservableCollection<Exercise> Items { get; } = new();

    // ── Параметры фильтров ────────────────────────────────────────────

    public List<string> MuscleOptions { get; } = new()
    {
        "Все группы", "Грудь", "Спина", "Ноги", "Плечи",
        "Бицепс", "Трицепс", "Кор", "Кардио", "Всё тело"
    };

    public List<string> EquipmentOptions { get; } = new()
    {
        "Всё оборудование", "Штанга", "Гантели", "Тренажёр",
        "Кроссовер", "Своё тело", "Гиря", "Кардио-тренажёр", "Другое"
    };

    private static readonly MuscleGroup?[] MuscleMap = {
        null,
        MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Legs, MuscleGroup.Shoulders,
        MuscleGroup.Biceps, MuscleGroup.Triceps, MuscleGroup.Core, MuscleGroup.Cardio,
        MuscleGroup.FullBody
    };

    private static readonly Equipment?[] EquipmentMap = {
        null,
        Equipment.Barbell, Equipment.Dumbbell, Equipment.Machine,
        Equipment.Cable, Equipment.Bodyweight, Equipment.Kettlebell,
        Equipment.CardioMachine, Equipment.Other
    };

    // ── Конструктор ───────────────────────────────────────────────────

    public ExerciseListViewModel(IExerciseRepository repo)
    {
        _repo = repo;
        ApplyFilters();
    }

    // ── Реакции на фильтры ────────────────────────────────────────────

    partial void OnSearchQueryChanged(string v)        => ApplyFilters();
    partial void OnMuscleFilterIndexChanged(int v)     => ApplyFilters();
    partial void OnEquipmentFilterIndexChanged(int v)  => ApplyFilters();

    private void ApplyFilters()
    {
        var filter = new ExerciseFilter
        {
            Query     = SearchQuery,
            Muscle    = MuscleFilterIndex    > 0 && MuscleFilterIndex    < MuscleMap.Length
                            ? MuscleMap[MuscleFilterIndex]
                            : null,
            Equipment = EquipmentFilterIndex > 0 && EquipmentFilterIndex < EquipmentMap.Length
                            ? EquipmentMap[EquipmentFilterIndex]
                            : null
        };

        Items.Clear();
        foreach (var ex in _repo.GetFiltered(filter))
            Items.Add(ex);
    }

    // ── Команды ───────────────────────────────────────────────────────

    public event EventHandler? AddExerciseRequested;
    public event EventHandler<Exercise>? ExerciseHistoryRequested;

    [RelayCommand]
    private void AddExercise() =>
        AddExerciseRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void ShowHistory(Exercise exercise) =>
        ExerciseHistoryRequested?.Invoke(this, exercise);

    public void Refresh() => ApplyFilters();
}
