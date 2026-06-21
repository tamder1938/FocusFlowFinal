using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Workout;
using FocusFlowFinal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public class MuscleGroupItem : ObservableObject
{
    public MuscleGroup Group    { get; }
    public string      Label    { get; }
    private bool _selected;
    public bool IsSelected { get => _selected; set => SetProperty(ref _selected, value); }

    public MuscleGroupItem(MuscleGroup g)
    {
        Group = g;
        Label = MuscleGroupLabels.Get(g);
    }
}

public class EquipmentItem : ObservableObject
{
    public Equipment Eq       { get; }
    public string    Label    { get; }
    private bool _selected;
    public bool IsSelected { get => _selected; set => SetProperty(ref _selected, value); }

    public EquipmentItem(Equipment e)
    {
        Eq    = e;
        Label = EquipmentLabels.Get(e);
    }
}

public partial class AddExerciseViewModel : ObservableObject
{
    private readonly IExerciseRepository _repo;

    [ObservableProperty] private string _name        = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _imageEmoji  = "💪";
    [ObservableProperty] private string _imagePath   = string.Empty;
    [ObservableProperty] private int    _typeIndex   = 0;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public ExerciseType SelectedType => (ExerciseType)TypeIndex;

    public ObservableCollection<MuscleGroupItem> PrimaryMuscleItems   { get; } = new();
    public ObservableCollection<MuscleGroupItem> SecondaryMuscleItems { get; } = new();
    public ObservableCollection<EquipmentItem>   EquipmentItems       { get; } = new();

    public Exercise? Result { get; private set; }
    public bool      Saved  { get; private set; }

    public event EventHandler? CloseRequested;

    public AddExerciseViewModel(IExerciseRepository repo)
    {
        _repo = repo;

        foreach (var g in Enum.GetValues<MuscleGroup>())
        {
            PrimaryMuscleItems.Add(new MuscleGroupItem(g));
            SecondaryMuscleItems.Add(new MuscleGroupItem(g));
        }
        foreach (var e in Enum.GetValues<Equipment>())
            EquipmentItems.Add(new EquipmentItem(e));
    }

    [RelayCommand]
    private async Task PickImage()
    {
        var topLevel = GetTopLevel();
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выбрать изображение",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images")
                    { Patterns = new[] { "*.jpg","*.jpeg","*.png","*.webp","*.gif" } }
            }
        });

        if (files.Count > 0)
            ImagePath = files[0].TryGetLocalPath() ?? string.Empty;
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Название обязательно";
            return;
        }

        var key = $"user_{Guid.NewGuid():N}";

        var exercise = new Exercise
        {
            Key              = key,
            Name             = Name.Trim(),
            Description      = Description.Trim(),
            Type             = SelectedType,
            ImageEmoji       = string.IsNullOrEmpty(ImageEmoji) ? "💪" : ImageEmoji,
            ImagePath        = ImagePath,
            IsBuiltin        = false,
            CreatedAt        = DateTime.Now,
            PrimaryMuscles   = PrimaryMuscleItems.Where(m => m.IsSelected).Select(m => m.Group).ToList(),
            SecondaryMuscles = SecondaryMuscleItems.Where(m => m.IsSelected).Select(m => m.Group).ToList(),
            Equipment        = EquipmentItems.Where(e => e.IsSelected).Select(e => e.Eq).ToList()
        };

        _repo.Upsert(exercise);
        Result = exercise;
        Saved  = true;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private static Avalonia.Controls.TopLevel? GetTopLevel()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desk)
            return desk.MainWindow;
        return null;
    }
}
