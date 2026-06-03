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

    [ObservableProperty]
    private int _selectedPriorityFilter = -1;

    public event Action<TaskItem>? FocusRequested;
    public LocalizationService Loc => LocalizationService.Instance;

    public TaskListViewModel(IDatabaseService db)
    {
        _db = db;
        LoadTasks();
    }

    private void LoadTasks()
    {
        var tasks = _db.GetAllTasks().ToList();
        foreach (var task in tasks.Where(t => t.Priority == null).ToList())
        {
            task.Priority = 1;
            _db.UpsertTask(task);
        }
        AllTasks = new ObservableCollection<TaskItem>(_db.GetAllTasks());
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (SelectedPriorityFilter == -1)
            FilteredTasks = new ObservableCollection<TaskItem>(AllTasks);
        else
            FilteredTasks = new ObservableCollection<TaskItem>(AllTasks.Where(t => t.Priority == SelectedPriorityFilter));
    }

    // Публичный метод для обновления списка извне (например, после удаления задачи)
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
        ApplyFilter();
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