using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

/// <summary>Фильтр отображения задач по статусу выполнения.</summary>
public enum CompletionFilter { All = 0, Active = 1, Done = 2 }

public partial class TaskListViewModel : ObservableObject
{
    private readonly IDatabaseService _db;

    // Полный список задач из БД
    [ObservableProperty] private ObservableCollection<TaskItem> _allTasks = new();

    // Единый отображаемый список: активные → выполненные
    [ObservableProperty] private ObservableCollection<TaskItem> _filteredTasks = new();

    [ObservableProperty] private TaskItem? _selectedTask;

    private int _selectedPriorityFilter = -1;
    public int SelectedPriorityFilter
    {
        get => _selectedPriorityFilter;
        set { if (SetProperty(ref _selectedPriorityFilter, value)) ApplyFilter(); }
    }

    private CompletionFilter _completionFilter = CompletionFilter.All;
    public CompletionFilter CompletionFilter
    {
        get => _completionFilter;
        set { if (SetProperty(ref _completionFilter, value)) ApplyFilter(); }
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
        set { if (SetProperty(ref _selectedProject, value)) ApplyFilter(); }
    }

    public event Action<TaskItem>? FocusRequested;
    public event Action<TaskItem>? StartTimerRequested;
    public LocalizationService Loc => LocalizationService.Instance;

    public TaskListViewModel(IDatabaseService db)
    {
        _db = db;

        Loc.PropertyChanged += (_, _) =>
        {
            var keepProjectId = SelectedProject?.Id;
            LoadProjects();
            SelectedProject = Projects.FirstOrDefault(p => p.Id == keepProjectId) ?? Projects.FirstOrDefault();
            ApplyFilter();
        };

        LoadProjects();
        LoadTasks();
    }

    private void LoadProjects()
    {
        var previousSelection = SelectedProject?.Id;

        Projects.Clear();
        Projects.Add(new ProjectItem { Id = 0,  Name = Loc["AllProjectsLbl"], Color = "#9CA3AF" });
        Projects.Add(new ProjectItem { Id = -1, Name = Loc["NoProject"],      Color = "#9CA3AF" });
        foreach (var p in _db.GetAllProjects())
            Projects.Add(p);

        SelectedProject = previousSelection.HasValue
            ? Projects.FirstOrDefault(p => p.Id == previousSelection.Value) ?? Projects[0]
            : Projects[0];
    }

    private void LoadTasks()
    {
        var tasks = _db.GetAllTasks().ToList();

        if (Projects.Count == 0) LoadProjects();

        var projectColors = Projects.ToDictionary(p => p.Id, p => p.Color);
        foreach (var task in tasks)
        {
            // Цвет проекта для цветной полоски
            if (task.ProjectId.HasValue && projectColors.TryGetValue(task.ProjectId.Value, out var color))
                task.ProjectColor = color;
            else
                task.ProjectColor = null;

            // Инициализируем observable-обёртки подзадач
            var capturedTask = task;
            task.InitSubtaskViewItems(() => OnSubtaskToggled(capturedTask));
        }

        AllTasks = new ObservableCollection<TaskItem>(tasks);
        ApplyFilter();
    }

    private void OnSubtaskToggled(TaskItem task)
    {
        // Автовыполнение: если все подзадачи выполнены → отмечаем задачу
        if (task.Subtasks.Count > 0)
        {
            bool allDone = task.Subtasks.All(s => s.IsCompleted);
            if (task.IsCompleted != allDone)
                task.IsCompleted = allDone;
        }

        _db.UpsertTask(task);
        ApplyFilter();
        TriggerMainStatsRefresh();
        RefreshCalendarUI();
    }

    // ── Фильтрация ───────────────────────────────────────────────────
    // При фильтре «Все»: сначала активные (по приоритету, затем по дате),
    // затем выполненные (так же). При остальных фильтрах — только нужная группа.
    private void ApplyFilter()
    {
        var query = AllTasks.AsEnumerable();

        if (SelectedPriorityFilter != -1)
            query = query.Where(t => t.Priority == SelectedPriorityFilter);

        if (SelectedProject != null)
        {
            if      (SelectedProject.Id == 0)  { /* все проекты */ }
            else if (SelectedProject.Id == -1) query = query.Where(t => t.ProjectId == null);
            else                                query = query.Where(t => t.ProjectId == SelectedProject.Id);
        }

        var filtered = query.ToList();

        IEnumerable<TaskItem> result;

        switch (CompletionFilter)
        {
            case CompletionFilter.Active:
                result = filtered
                    .Where(t => !t.IsCompleted)
                    .OrderBy(t => t.Priority)
                    .ThenBy(t => t.DueDate ?? DateTime.MaxValue);
                break;

            case CompletionFilter.Done:
                result = filtered
                    .Where(t => t.IsCompleted)
                    .OrderBy(t => t.Priority)
                    .ThenBy(t => t.DueDate ?? DateTime.MaxValue);
                break;

            default: // All: сначала активные, потом выполненные
                var active = filtered
                    .Where(t => !t.IsCompleted)
                    .OrderBy(t => t.Priority)
                    .ThenBy(t => t.DueDate ?? DateTime.MaxValue);

                var completed = filtered
                    .Where(t => t.IsCompleted)
                    .OrderBy(t => t.Priority)
                    .ThenBy(t => t.DueDate ?? DateTime.MaxValue);

                result = active.Concat(completed);
                break;
        }

        FilteredTasks = new ObservableCollection<TaskItem>(result);
    }

    public void RefreshTasks() => LoadTasks();

    // ── Фильтр по приоритету ─────────────────────────────────────────
    [RelayCommand]
    private void SetPriorityFilter(string priorityStr)
    {
        if (int.TryParse(priorityStr, out int priority))
            SelectedPriorityFilter = priority;
    }

    [RelayCommand]
    private void SetCompletionFilter(string filterStr)
    {
        CompletionFilter = filterStr switch
        {
            "1" => CompletionFilter.Active,
            "2" => CompletionFilter.Done,
            _   => CompletionFilter.All
        };
    }

    // ── Переключение статуса выполнения ──────────────────────────────
    [RelayCommand]
    private void ToggleTaskCompletion(TaskItem? task)
    {
        if (task == null) return;
        task.IsCompleted = !task.IsCompleted;

        if (task.Subtasks.Count > 0)
        {
            if (task.IsCompleted)
            {
                // Задача отмечена выполненной → помечаем все подзадачи тоже
                foreach (var sub in task.Subtasks)
                    sub.IsCompleted = true;
            }
            else
            {
                // Задача снята с выполнения → сбрасываем все подзадачи
                foreach (var sub in task.Subtasks)
                    sub.IsCompleted = false;
            }
            // Перестраиваем SubtaskViewItems, чтобы чекбоксы сразу отразили новое состояние
            var capturedTask = task;
            task.InitSubtaskViewItems(() => OnSubtaskToggled(capturedTask));
        }

        _db.UpsertTask(task);
        ApplyFilter();
        TriggerMainStatsRefresh();
        RefreshCalendarUI();
    }

    // ── Редактирование и добавление ──────────────────────────────────
    [RelayCommand]
    private async Task EditTask(TaskItem? task)
    {
        if (task == null) return;

        var dialogVm = new TaskDialogViewModel(task);

        dialogVm.TaskDeleted += _ =>
        {
            LoadTasks();
            RefreshCalendarUI();
            TriggerMainStatsRefresh();
        };

        var dialog = new TaskDialog { DataContext = dialogVm };
        var owner = (App.Current?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner == null) return;

        var result = await dialog.ShowDialog<bool?>(owner);

        if (result == true)
            _db.UpsertTask(task);

        LoadTasks();
        RefreshCalendarUI();
        TriggerMainStatsRefresh();
    }

    [RelayCommand]
    private async Task AddTask()
    {
        var newTask = new TaskItem
        {
            Title                  = string.Empty,
            DueDate                = DateTime.Today,
            Priority               = 1,
            PlannedDurationMinutes = 30
        };
        var dialog = new TaskDialog { DataContext = new TaskDialogViewModel(newTask) };
        var owner  = (App.Current?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
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
        if (task != null && task.PlannedDurationMinutes > 0 && !task.IsCompleted)
            StartTimerRequested?.Invoke(task);
    }

    // ── Вспомогательные ──────────────────────────────────────────────
    private void TriggerMainStatsRefresh()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            if (desktop.MainWindow?.DataContext is MainViewModel mainVm)
                mainVm.RefreshTodayMiniStats();
    }

    private void RefreshCalendarUI()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            if (desktop.MainWindow?.DataContext is MainViewModel mainVm)
                mainVm.RefreshCurrentCalendarView();
    }
}
