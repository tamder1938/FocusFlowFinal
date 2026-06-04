using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class TaskListViewModel : ObservableObject
{
    private readonly IDatabaseService _db;

    [ObservableProperty]
    private ObservableCollection<TaskItem> _allTasks = new();

    [ObservableProperty]
    private ObservableCollection<TaskItem> _filteredTasks = new();

    [ObservableProperty]
    private TaskItem? _selectedTask;

    private int _selectedPriorityFilter = -1;
    public int SelectedPriorityFilter
    {
        get => _selectedPriorityFilter;
        set
        {
            if (SetProperty(ref _selectedPriorityFilter, value))
                ApplyFilter();
        }
    }

    private ObservableCollection<ProjectItem> _projects = new();
    public ObservableCollection<ProjectItem> Projects
    {
        get => _projects;
        set => SetProperty(ref _projects, value);
    }

    private ProjectItem? _selectedProject;
    public ProjectItem? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (SetProperty(ref _selectedProject, value))
                ApplyFilter();
        }
    }

    public event Action<TaskItem>? FocusRequested;
    public LocalizationService Loc => LocalizationService.Instance;

    // Событие для запуска таймера (чтобы не создавать жёсткую связь)
    public event Action<TaskItem>? StartTimerRequested;

    public TaskListViewModel(IDatabaseService db)
    {
        _db = db;
        LoadProjects();
        LoadTasks();
    }

    private void LoadProjects()
    {
        Projects.Clear();
        Projects.Add(new ProjectItem { Id = 0, Name = "Все проекты", Color = "#9CA3AF" });
        Projects.Add(new ProjectItem { Id = -1, Name = "Без проекта", Color = "#9CA3AF" });
        var userProjects = _db.GetAllProjects();
        foreach (var p in userProjects)
            Projects.Add(p);
        SelectedProject = Projects.FirstOrDefault();
    }

    private void LoadTasks()
    {
        var tasks = _db.GetAllTasks().ToList();

        foreach (var task in tasks.Where(t => t.Priority == null).ToList())
        {
            task.Priority = 1;
            _db.UpsertTask(task);
        }

        if (Projects.Count == 0) LoadProjects();

        var projectColors = Projects.ToDictionary(p => p.Id, p => p.Color);

        foreach (var task in tasks)
        {
            if (task.ProjectId.HasValue && projectColors.TryGetValue(task.ProjectId.Value, out var color))
                task.ProjectColor = color;
            else
                task.ProjectColor = null;
        }

        AllTasks = new ObservableCollection<TaskItem>(tasks);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var query = AllTasks.AsEnumerable();

        if (SelectedPriorityFilter != -1)
            query = query.Where(t => t.Priority == SelectedPriorityFilter);

        if (SelectedProject != null)
        {
            if (SelectedProject.Id == 0) { }
            else if (SelectedProject.Id == -1)
                query = query.Where(t => t.ProjectId == null);
            else
                query = query.Where(t => t.ProjectId == SelectedProject.Id);
        }

        FilteredTasks = new ObservableCollection<TaskItem>(query);
    }

    public void RefreshTasks()
    {
        LoadTasks();
    }

    [RelayCommand]
    private void SetPriorityFilter(string priorityStr)
    {
        if (int.TryParse(priorityStr, out int priority))
            SelectedPriorityFilter = priority;
        else if (priorityStr == "-1")
            SelectedPriorityFilter = -1;
    }

    [RelayCommand]
    private async Task EditTask(TaskItem? task)
    {
        if (task == null) return;
        var dialog = new TaskDialog { DataContext = new TaskDialogViewModel(task) };
        var owner = (App.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner == null) return;
        var result = await dialog.ShowDialog<bool?>(owner);
        if (result == true)
        {
            _db.UpsertTask(task);
            LoadTasks();
            RefreshCalendarUI();
            TriggerMainStatsRefresh();
        }
    }

    [RelayCommand]
    private async Task AddTask()
    {
        var newTask = new TaskItem { Title = string.Empty, DueDate = DateTime.Today, Priority = 1, PlannedDurationMinutes = 30 };
        var dialog = new TaskDialog { DataContext = new TaskDialogViewModel(newTask) };
        var owner = (App.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner == null) return;
        var result = await dialog.ShowDialog<bool?>(owner);
        if (result == true && !string.IsNullOrWhiteSpace(newTask.Title))
        {
            _db.UpsertTask(newTask);
            if (newTask.DueDate.HasValue) _db.DeleteEventForTask(newTask.Id);
            LoadTasks();
            RefreshCalendarUI();
            TriggerMainStatsRefresh();
        }
    }

    [RelayCommand]
    private void StartTimer(TaskItem? task)
    {
        if (task != null && task.PlannedDurationMinutes > 0)
            StartTimerRequested?.Invoke(task);
    }

    private void TriggerMainStatsRefresh()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            if (desktop.MainWindow?.DataContext is MainViewModel mainVm)
                mainVm.RefreshTodayMiniStats();
    }

    private void RefreshCalendarUI()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            if (desktop.MainWindow?.DataContext is MainViewModel mainVm)
            {
                if (mainVm.CurrentCalendarView is DayViewModel dayVm) dayVm.LoadEvents();
                else if (mainVm.CurrentCalendarView is MonthViewModel monthVm) monthVm.GoToMonth(monthVm.CurrentMonthDate);
                else if (mainVm.CurrentCalendarView is YearViewModel yearVm) yearVm.GoToYear(yearVm.CurrentYear);
            }
    }
}