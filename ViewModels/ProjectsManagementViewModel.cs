using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public partial class ProjectsManagementViewModel : ObservableObject
{
    private readonly IDatabaseService _db;

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private ObservableCollection<ProjectItem> _projects = new();
    [ObservableProperty] private ProjectItem? _selectedProject;

    // Свойства для создания нового проекта
    [ObservableProperty] private string _newProjectName = string.Empty;
    [ObservableProperty] private string _selectedColor = "#3B82F6"; // Цвет по умолчанию (синий)

    // Доступные цвета для быстрого выбора в интерфейсе (палитра)
    public ObservableCollection<string> ColorPalette { get; } = new()
    {
        "#3B82F6", // Синий
        "#10B981", // Зеленый
        "#F59E0B", // Оранжевый
        "#EF4444", // Красный
        "#8B5CF6", // Фиолетовый
        "#EC4899", // Розовый
        "#14B8A6", // Бирюзовый
        "#6B7280"  // Серый
    };

    public ProjectsManagementViewModel()
    {
        _db = ((App)Avalonia.Application.Current!).Services!.GetRequiredService<IDatabaseService>();
        LoadProjects();
    }

    private void LoadProjects()
    {
        Projects.Clear();
        var allProjects = _db.GetAllProjects();
        foreach (var p in allProjects)
        {
            Projects.Add(p);
        }
    }

    [RelayCommand]
    private void AddProject()
    {
        if (string.IsNullOrWhiteSpace(NewProjectName))
            return;

        // Проверяем, нет ли уже проекта с таким же именем
        if (Projects.Any(p => p.Name.Equals(NewProjectName.Trim(), StringComparison.OrdinalIgnoreCase)))
            return;

        var newProject = new ProjectItem
        {
            Name = NewProjectName.Trim(),
            Color = SelectedColor
        };

        _db.UpsertProject(newProject);
        LoadProjects();

        // Сбрасываем поля ввода
        NewProjectName = string.Empty;
        SelectedColor = "#3B82F6";
    }

    [RelayCommand]
    private void DeleteProject(ProjectItem? project)
    {
        if (project == null) return;

        // Наш DatabaseService автоматически отвяжет этот проект от всех задач
        _db.DeleteProject(project.Id);
        LoadProjects();
    }

    [RelayCommand]
    private void SelectColorFromPalette(string colorHex)
    {
        if (!string.IsNullOrEmpty(colorHex))
        {
            SelectedColor = colorHex;
        }
    }

    [RelayCommand]
    private void Close(Avalonia.Controls.Window window)
    {
        window?.Close();
    }
}
