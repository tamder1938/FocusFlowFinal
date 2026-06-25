using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

/// <summary>Элемент списка с чекбоксом для диалога ограничения лимитов.</summary>
public partial class LimitItem : ObservableObject
{
    public int      Id      { get; init; }
    public string   Title   { get; init; } = string.Empty;
    public string   Subtitle { get; init; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChecked))]
    private bool _isSelected;

    public bool IsChecked => IsSelected;
}

public partial class LimitEnforcementViewModel : ObservableObject
{
    private readonly IDatabaseService _db;
    private readonly IAuthService _auth;

    // ── Лимиты ─────────────────────────────────────────────────────────
    public const int TaskLimit    = EntitlementService.TaskLimit;
    public const int ProjectLimit = EntitlementService.ProjectLimit;
    public const int EventLimit   = EntitlementService.EventLimit;

    // ── Списки для выбора на удаление ──────────────────────────────────
    public ObservableCollection<LimitItem> Tasks    { get; } = new();
    public ObservableCollection<LimitItem> Projects { get; } = new();
    public ObservableCollection<LimitItem> Events   { get; } = new();

    // ── Превышения ─────────────────────────────────────────────────────
    public bool HasTaskOverflow    => Tasks.Count    > TaskLimit;
    public bool HasProjectOverflow => Projects.Count > ProjectLimit;
    public bool HasEventOverflow   => Events.Count   > EventLimit;

    // ── Счётчики «ещё нужно удалить» ───────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private int _taskDeleteNeeded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private int _projectDeleteNeeded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private int _eventDeleteNeeded;

    public bool CanConfirm => TaskDeleteNeeded <= 0 && ProjectDeleteNeeded <= 0 && EventDeleteNeeded <= 0;

    // ── Результат диалога ──────────────────────────────────────────────
    public bool SignInAgainRequested { get; private set; }

    private Window? _window;

    public LimitEnforcementViewModel(IDatabaseService db, IAuthService auth)
    {
        _db   = db;
        _auth = auth;
        LoadData();
    }

    private void LoadData()
    {
        // Загрузить гостевые данные (workspace уже переключён на "local")
        foreach (var t in _db.GetAllTasks())
            Tasks.Add(new LimitItem { Id = t.Id, Title = t.Title,
                Subtitle = t.DueDate.HasValue ? t.DueDate.Value.ToString("dd.MM.yyyy") : string.Empty });

        foreach (var p in _db.GetAllProjects())
            Projects.Add(new LimitItem { Id = p.Id, Title = p.Name });

        // Только базовые (не виртуальные) события — CountBaseEvents возвращает их кол-во,
        // но нам нужен список: берём события за широкий диапазон без дубликатов серий
        var allEvents = _db.GetEventsForPeriod(DateTime.MinValue, DateTime.MaxValue)
                           .GroupBy(e => e.Id).Select(g => g.First())
                           .OrderBy(e => e.Start).ToList();
        foreach (var e in allEvents)
            Events.Add(new LimitItem { Id = e.Id, Title = e.Title,
                Subtitle = e.Start.ToString("dd.MM.yyyy HH:mm") });

        UpdateCounters();

        // Подписываемся на изменение выбора
        foreach (var item in Tasks)    item.PropertyChanged += (_, _) => UpdateCounters();
        foreach (var item in Projects) item.PropertyChanged += (_, _) => UpdateCounters();
        foreach (var item in Events)   item.PropertyChanged += (_, _) => UpdateCounters();
    }

    private void UpdateCounters()
    {
        int selectedTasks    = Tasks.Count(i => i.IsSelected);
        int selectedProjects = Projects.Count(i => i.IsSelected);
        int selectedEvents   = Events.Count(i => i.IsSelected);

        TaskDeleteNeeded    = Math.Max(0, Tasks.Count    - selectedTasks    - TaskLimit);
        ProjectDeleteNeeded = Math.Max(0, Projects.Count - selectedProjects - ProjectLimit);
        EventDeleteNeeded   = Math.Max(0, Events.Count   - selectedEvents   - EventLimit);

        OnPropertyChanged(nameof(CanConfirm));
        ConfirmCommand.NotifyCanExecuteChanged();
    }

    public void SetWindow(Window window) => _window = window;

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private async Task Confirm()
    {
        // Удалить выбранные задачи
        foreach (var item in Tasks.Where(i => i.IsSelected).ToList())
            _db.DeleteTask(item.Id);

        // Удалить выбранные проекты (задачи проекта — обнуляем ProjectId, а не удаляем)
        foreach (var item in Projects.Where(i => i.IsSelected).ToList())
        {
            // Перенести задачи проекта в «без проекта»
            foreach (var task in _db.GetAllTasks().Where(t => t.ProjectId == item.Id).ToList())
            {
                task.ProjectId = null;
                _db.UpsertTask(task);
            }
            _db.DeleteProject(item.Id);
        }

        // Удалить выбранные события
        foreach (var item in Events.Where(i => i.IsSelected).ToList())
            _db.DeleteEvent(item.Id);

        _window?.Close(true);
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SignInAgain()
    {
        SignInAgainRequested = true;
        _window?.Close(false);
        await Task.CompletedTask;
    }
}
