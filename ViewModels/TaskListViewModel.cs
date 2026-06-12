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

/// <summary>Фильтр отображения задач по статусу выполнения (Проблема 6, 12).</summary>
public enum CompletionFilter { All = 0, Active = 1, Done = 2 }

public partial class TaskListViewModel : ObservableObject
{
    private readonly IDatabaseService _db;

    // Полный список задач, загруженный из БД
    [ObservableProperty] private ObservableCollection<TaskItem> _allTasks = new();

    // ИСПРАВЛЕНО (Проблема 6): два отдельных списка — активные и выполненные задачи
    [ObservableProperty] private ObservableCollection<TaskItem> _activeTasks = new();
    [ObservableProperty] private ObservableCollection<TaskItem> _completedTasks = new();

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

    // Expander для выполненных задач: развёрнут по умолчанию
    [ObservableProperty] private bool _isCompletedExpanderExpanded = true;

    // ИСПРАВЛЕНО (Проблема 6): заголовок "Выполненные задачи" для Expander
    public string CompletedTasksHeader =>
        $"{Loc["CompletedTasksHeader"]} ({CompletedTasks.Count})";

    /// <summary>true, если есть хотя бы одна выполненная задача (для IsVisible Expander-а).</summary>
    public bool HasCompletedTasks => CompletedTasks.Count > 0;

    public TaskListViewModel(IDatabaseService db)
    {
        _db = db;

        // ИСПРАВЛЕНО (Проблема 9, 10, 12): при смене языка перезагружаем
        // проекты (локализованные имена "Все проекты"/"Без проекта")
        // и пересчитываем DurationText/CompletedTasksHeader.
        Loc.PropertyChanged += (_, _) =>
        {
            var keepProjectId = SelectedProject?.Id;
            LoadProjects();
            SelectedProject = Projects.FirstOrDefault(p => p.Id == keepProjectId) ?? Projects.FirstOrDefault();
            ApplyFilter();
            OnPropertyChanged(nameof(CompletedTasksHeader));
            OnPropertyChanged(nameof(HasCompletedTasks));
        };

        LoadProjects();
        LoadTasks();
    }

    // ИСПРАВЛЕНО (Проблема 12): "Все проекты" и "Без проекта" локализованы
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
            if (task.ProjectId.HasValue && projectColors.TryGetValue(task.ProjectId.Value, out var color))
                task.ProjectColor = color;
            else
                task.ProjectColor = null;
        }

        AllTasks = new ObservableCollection<TaskItem>(tasks);
        ApplyFilter();
    }

    // ИСПРАВЛЕНО (Проблема 6): разделяем задачи на активные / выполненные,
    // применяя при этом фильтры по приоритету и проекту к обеим коллекциям.
    private void ApplyFilter()
    {
        var query = AllTasks.AsEnumerable();

        // Фильтр по приоритету
        if (SelectedPriorityFilter != -1)
            query = query.Where(t => t.Priority == SelectedPriorityFilter);

        // Фильтр по проекту
        if (SelectedProject != null)
        {
            if      (SelectedProject.Id == 0)  { /* все проекты */ }
            else if (SelectedProject.Id == -1) query = query.Where(t => t.ProjectId == null);
            else                                query = query.Where(t => t.ProjectId == SelectedProject.Id);
        }

        var filtered = query.ToList();

        var active    = filtered.Where(t => !t.IsCompleted).OrderBy(t => t.Priority).ToList();
        var completed = filtered.Where(t => t.IsCompleted).OrderBy(t => t.Priority).ToList();

        // Фильтр по статусу:
        // Done    → выполненные задачи в основном списке (с визуальным стилем "выполнено"),
        //           Expander скрыт.
        // Active  → только невыполненные, Expander скрыт.
        // All     → невыполненные в основном списке, выполненные в Expander.
        switch (CompletionFilter)
        {
            case CompletionFilter.Done:
                ActiveTasks    = new ObservableCollection<TaskItem>(completed);
                CompletedTasks = new ObservableCollection<TaskItem>();
                break;
            case CompletionFilter.Active:
                ActiveTasks    = new ObservableCollection<TaskItem>(active);
                CompletedTasks = new ObservableCollection<TaskItem>();
                break;
            default: // All
                ActiveTasks    = new ObservableCollection<TaskItem>(active);
                CompletedTasks = new ObservableCollection<TaskItem>(completed);
                break;
        }

        OnPropertyChanged(nameof(CompletedTasksHeader));
        OnPropertyChanged(nameof(HasCompletedTasks));
    }

    public void RefreshTasks() => LoadTasks();

    // ── Фильтр по приоритету ─────────────────────────────────────
    [RelayCommand]
    private void SetPriorityFilter(string priorityStr)
    {
        if (int.TryParse(priorityStr, out int priority))
            SelectedPriorityFilter = priority;
    }

    // ИСПРАВЛЕНО (Проблема 12): фильтр статуса выполнения, локализованные подписи в XAML
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

    // ── Переключение статуса выполнения (Проблема 6) ─────────────
    /// <summary>
    /// Инвертирует <see cref="TaskItem.IsCompleted"/>, сохраняет в БД и
    /// перемещает задачу между списками ActiveTasks / CompletedTasks.
    /// </summary>
    [RelayCommand]
    private void ToggleTaskCompletion(TaskItem? task)
    {
        if (task == null) return;
        task.IsCompleted = !task.IsCompleted;

        // При фильтре «Все» авто-разворачиваем Expander, чтобы пользователь
        // видел, куда переместилась только что выполненная задача.
        if (task.IsCompleted && CompletionFilter == CompletionFilter.All)
            IsCompletedExpanderExpanded = true;

        _db.UpsertTask(task);
        ApplyFilter();
        TriggerMainStatsRefresh();
        RefreshCalendarUI();
    }

    // ── Редактирование и добавление ──────────────────────────────

    // ИСПРАВЛЕНО (Проблема 5): после закрытия диалога ВСЕГДА вызываем
    // LoadTasks(), независимо от результата — это гарантирует, что
    // удалённая через TaskDialog задача исчезнет из списка.
    [RelayCommand]
    private async Task EditTask(TaskItem? task)
    {
        if (task == null) return;

        var dialogVm = new TaskDialogViewModel(task);

        // ИСПРАВЛЕНО (Проблема 5): подписка на TaskDeleted — при удалении
        // обновляем список задач и статистику немедленно.
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

        // ИСПРАВЛЕНО (Проблема 5): обновляем список в любом случае —
        // это покрывает как сохранение, так и удаление задачи.
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

    // ── Вспомогательные ─────────────────────────────────────────
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
