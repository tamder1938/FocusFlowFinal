using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class TaskDialogViewModel : ObservableObject
{
    private readonly TaskItem _task;
    private readonly IDatabaseService _db;
    private readonly ITemplateService _templateService;

    public bool IsEditMode => _task.Id > 0;

    public bool IsHighSelected   => PriorityIndex == 0;
    public bool IsMediumSelected => PriorityIndex == 1;
    public bool IsLowSelected    => PriorityIndex == 2;

    public string HighButtonColor   => PriorityIndex == 0 ? "#EF4444" : "#E5E7EB";
    public string MediumButtonColor => PriorityIndex == 1 ? "#F59E0B" : "#E5E7EB";
    public string LowButtonColor    => PriorityIndex == 2 ? "#10B981" : "#E5E7EB";

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private string _title        = string.Empty;
    [ObservableProperty] private string _description  = string.Empty;
    [ObservableProperty] private DateTimeOffset _dueDate = DateTimeOffset.Now;

    [ObservableProperty] private bool _hasDate;
    [ObservableProperty] private bool _isTimeBound;
    [ObservableProperty] private bool _isDurationSet;

    [ObservableProperty] private int _startHour   = 9;
    [ObservableProperty] private int _startMinute = 0;
    [ObservableProperty] private int _endHour     = 10;
    [ObservableProperty] private int _endMinute   = 0;

    [ObservableProperty] private int _durationMinutes = 30;

    [ObservableProperty] private int _priority      = 1;
    [ObservableProperty] private int _priorityIndex = 1;

    [ObservableProperty] private ObservableCollection<ProjectItem>    _projectsList   = new();
    [ObservableProperty] private ProjectItem?                          _selectedProject;

    [ObservableProperty] private bool   _saveAsTemplate;
    [ObservableProperty] private string _templateName  = string.Empty;

    [ObservableProperty] private ObservableCollection<TaskTemplate> _taskTemplates = new();
    [ObservableProperty] private TaskTemplate? _selectedTaskTemplate;

    // ── Подзадачи ──────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<SubtaskEditItem> _subtasks = new();

    // ── Место ───────────────────────────────────────────────────────────
    [ObservableProperty] private bool _hasLocation;
    [ObservableProperty] private PlaceLocation? _selectedLocation;

    public event Action<int>? TaskDeleted;

    // Флаг для предотвращения рекурсии при пересчёте времени
    private bool _isRecalculating;

    partial void OnPriorityIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsHighSelected));
        OnPropertyChanged(nameof(IsMediumSelected));
        OnPropertyChanged(nameof(IsLowSelected));
        OnPropertyChanged(nameof(HighButtonColor));
        OnPropertyChanged(nameof(MediumButtonColor));
        OnPropertyChanged(nameof(LowButtonColor));
    }

    // ── Двунаправленная логика времени начала / окончания / длительности ──

    // При изменении времени начала или длительности → пересчитываем конец
    partial void OnStartHourChanged(int value)   => RecalcEndFromStartAndDuration();
    partial void OnStartMinuteChanged(int value) => RecalcEndFromStartAndDuration();
    partial void OnDurationMinutesChanged(int value) => RecalcEndFromStartAndDuration();

    // При изменении времени конца → пересчитываем длительность
    partial void OnEndHourChanged(int value)   => RecalcDurationFromEnd();
    partial void OnEndMinuteChanged(int value) => RecalcDurationFromEnd();

    private void RecalcEndFromStartAndDuration()
    {
        if (!IsTimeBound || !IsDurationSet || _isRecalculating) return;
        _isRecalculating = true;
        try
        {
            var start = new TimeSpan(
                Math.Max(0, Math.Min(23, StartHour)),
                Math.Max(0, Math.Min(59, StartMinute)), 0);
            var end = start.Add(TimeSpan.FromMinutes(Math.Max(1, DurationMinutes)));
            int totalHours = (int)end.TotalHours;
            EndHour   = totalHours % 24;
            EndMinute = end.Minutes;
        }
        finally { _isRecalculating = false; }
    }

    private void RecalcDurationFromEnd()
    {
        if (!IsTimeBound || _isRecalculating) return;
        _isRecalculating = true;
        try
        {
            var start = new TimeSpan(
                Math.Max(0, Math.Min(23, StartHour)),
                Math.Max(0, Math.Min(59, StartMinute)), 0);
            var end = new TimeSpan(
                Math.Max(0, Math.Min(23, EndHour)),
                Math.Max(0, Math.Min(59, EndMinute)), 0);

            double durationMins = (end - start).TotalMinutes;
            if (durationMins > 0)
            {
                DurationMinutes = (int)durationMins;
                IsDurationSet   = true;
            }
        }
        finally { _isRecalculating = false; }
    }

    public TaskDialogViewModel(TaskItem task, ITemplateService? templateService = null)
    {
        _task = task;
        var services = ((App)Avalonia.Application.Current!).Services!;
        _db              = services.GetRequiredService<IDatabaseService>();
        _templateService = templateService ?? services.GetRequiredService<ITemplateService>();

        Title       = task.Title;
        Description = task.Description ?? string.Empty;
        Priority      = task.Priority;
        PriorityIndex = task.Priority;

        LoadProjectsData(task.ProjectId);

        HasDate = task.DueDate.HasValue;
        DueDate = task.DueDate.HasValue
            ? new DateTimeOffset(task.DueDate.Value, DateTimeOffset.Now.Offset)
            : DateTimeOffset.Now;

        IsTimeBound = task.StartTime.HasValue;
        if (task.StartTime.HasValue)
        {
            StartHour   = task.StartTime.Value.Hours;
            StartMinute = task.StartTime.Value.Minutes;
        }

        // Загружаем EndTime или вычисляем из StartTime + Duration
        if (task.EndTime.HasValue)
        {
            EndHour   = task.EndTime.Value.Hours;
            EndMinute = task.EndTime.Value.Minutes;
        }
        else if (task.StartTime.HasValue && task.PlannedDurationMinutes > 0)
        {
            var end = task.StartTime.Value.Add(TimeSpan.FromMinutes(task.PlannedDurationMinutes));
            EndHour   = (int)end.TotalHours % 24;
            EndMinute = end.Minutes;
        }
        else if (task.StartTime.HasValue)
        {
            EndHour   = task.StartTime.Value.Hours + 1;
            EndMinute = task.StartTime.Value.Minutes;
        }

        IsDurationSet   = task.PlannedDurationMinutes > 0;
        DurationMinutes = IsDurationSet ? task.PlannedDurationMinutes : 30;

        // Загружаем подзадачи
        foreach (var sub in task.Subtasks)
            AddSubtaskItem(sub.Title, sub.IsCompleted);

        // Загружаем место
        HasLocation      = task.Location != null;
        SelectedLocation = task.Location;

        LoadTaskTemplates();
    }

    // ── Подзадачи ──────────────────────────────────────────────────────

    [RelayCommand]
    private void AddSubtask()
    {
        AddSubtaskItem(string.Empty, false);
    }

    private void AddSubtaskItem(string title, bool isCompleted)
    {
        var item = new SubtaskEditItem { Title = title, IsCompleted = isCompleted };
        item.RemoveCommand = new RelayCommand(() => RemoveSubtask(item));
        item.CompletionChanged = SyncMainTaskCompletion;
        Subtasks.Add(item);
    }

    private void RemoveSubtask(SubtaskEditItem item)
    {
        Subtasks.Remove(item);
        SyncMainTaskCompletion();
    }

    private void SyncMainTaskCompletion()
    {
        // Уведомление для UI — реальное обновление происходит при сохранении
    }

    // ── Шаблоны ────────────────────────────────────────────────────────

    private void LoadProjectsData(int? activeProjectId)
    {
        ProjectsList.Clear();
        ProjectsList.Add(new ProjectItem { Id = 0, Name = Loc["NoProject"], Color = "#9CA3AF" });
        foreach (var p in _db.GetAllProjects())
            ProjectsList.Add(p);

        SelectedProject = (activeProjectId.HasValue && activeProjectId.Value > 0)
            ? ProjectsList.FirstOrDefault(p => p.Id == activeProjectId.Value)
            : ProjectsList[0];
    }

    private void LoadTaskTemplates()
    {
        TaskTemplates.Clear();
        foreach (var t in _templateService.GetAllTaskTemplates())
            TaskTemplates.Add(t);
    }

    partial void OnSelectedTaskTemplateChanged(TaskTemplate? value)
    {
        if (value == null) return;
        Title           = value.Title;
        Description     = value.Description;
        Priority        = value.Priority ?? 1;
        PriorityIndex   = Priority;
        DurationMinutes = value.PlannedDurationMinutes;
        IsDurationSet   = value.PlannedDurationMinutes > 0;
        HasDate         = value.HasDate;
        IsTimeBound     = value.IsTimeBound;
        StartHour       = value.StartHour;
        StartMinute     = value.StartMinute;

        // Пересчитываем EndTime по новым значениям
        RecalcEndFromStartAndDuration();

        Subtasks.Clear();
        foreach (var sub in value.Subtasks)
            AddSubtaskItem(sub.Title, false);
    }

    [RelayCommand]
    private void SetPriority(string priorityStr)
    {
        if (int.TryParse(priorityStr, out int p) && p >= 0 && p <= 2)
        {
            Priority      = p;
            PriorityIndex = p;
        }
    }

    // ── Сохранение ─────────────────────────────────────────────────────

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Title)) return;

        _task.Title       = Title.Trim();
        _task.Description = Description.Trim();
        _task.Priority    = PriorityIndex;
        _task.ProjectId   = (SelectedProject != null && SelectedProject.Id > 0) ? SelectedProject.Id : null;
        _task.DueDate     = HasDate ? DueDate.LocalDateTime.Date : null;

        _task.StartTime = IsTimeBound
            ? new TimeSpan(
                Math.Max(0, Math.Min(23, StartHour)),
                Math.Max(0, Math.Min(59, StartMinute)), 0)
            : null;

        _task.EndTime = IsTimeBound
            ? new TimeSpan(
                Math.Max(0, Math.Min(23, EndHour)),
                Math.Max(0, Math.Min(59, EndMinute)), 0)
            : null;

        _task.PlannedDurationMinutes = IsDurationSet ? DurationMinutes : 0;

        _task.Color = _task.Priority switch
        {
            0 => "#EF4444",
            1 => "#F59E0B",
            2 => "#10B981",
            _ => "#9CA3AF"
        };

        // Сохраняем подзадачи (пустые строки игнорируем)
        _task.Subtasks = Subtasks
            .Where(s => !string.IsNullOrWhiteSpace(s.Title))
            .Select(s => new Subtask { Title = s.Title.Trim(), IsCompleted = s.IsCompleted })
            .ToList();

        // Сохраняем место
        _task.Location = HasLocation && SelectedLocation != null &&
                         !string.IsNullOrWhiteSpace(SelectedLocation.DisplayName)
            ? SelectedLocation : null;

        // Автовыполнение основной задачи если все подзадачи выполнены
        if (_task.Subtasks.Count > 0 && _task.Subtasks.All(s => s.IsCompleted))
            _task.IsCompleted = true;

        if (SaveAsTemplate && !string.IsNullOrWhiteSpace(TemplateName))
        {
            var taskTemplate = new TaskTemplate
            {
                Name                   = TemplateName.Trim(),
                Title                  = _task.Title,
                Description            = _task.Description ?? string.Empty,
                PlannedDurationMinutes = _task.PlannedDurationMinutes,
                Priority               = _task.Priority,
                ProjectId              = _task.ProjectId,
                HasDate                = HasDate,
                IsTimeBound            = IsTimeBound,
                StartHour              = StartHour,
                StartMinute            = StartMinute,
                Subtasks               = _task.Subtasks.Select(s => new Subtask { Title = s.Title }).ToList()
            };
            _templateService.UpsertTaskTemplate(taskTemplate);
            LoadTaskTemplates();
        }

        CloseDialog(true);
    }

    [RelayCommand]
    private void Cancel() => CloseDialog(false);

    [RelayCommand]
    private async Task DeleteAsync()
    {
        var owner = GetOwnerWindow();

        IBrush windowBg  = (App.Current?.Resources["CardBackground"] as IBrush)
                           ?? new SolidColorBrush(Colors.White);
        IBrush textBrush = (App.Current?.Resources["PrimaryText"]    as IBrush)
                           ?? new SolidColorBrush(Colors.Black);

        var yesBtn = new Button
        {
            Content      = Loc["Yes"],
            Width        = 90, Height = 36,
            Background   = new SolidColorBrush(Color.Parse("#EF4444")),
            Foreground   = Brushes.White,
            CornerRadius = new CornerRadius(6),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment   = VerticalAlignment.Center,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        var noBtn = new Button
        {
            Content      = Loc["No"],
            Width        = 90, Height = 36,
            Background   = new SolidColorBrush(Color.Parse("#6B7280")),
            Foreground   = Brushes.White,
            CornerRadius = new CornerRadius(6),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment   = VerticalAlignment.Center,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };

        var confirmWindow = new Window
        {
            Title                 = Loc["DeleteBtn"],
            Width                 = 340, Height = 165,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize             = false,
            Background            = windowBg,
            Content               = new StackPanel
            {
                Margin  = new Thickness(24, 20, 24, 20),
                Spacing = 20,
                Children =
                {
                    new TextBlock
                    {
                        Text         = $"{Loc["DeleteBtn"]} «{_task.Title}»?",
                        TextWrapping = TextWrapping.Wrap,
                        Foreground   = textBrush,
                        FontSize     = 14
                    },
                    new StackPanel
                    {
                        Orientation         = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing             = 10,
                        Children            = { yesBtn, noBtn }
                    }
                }
            }
        };

        var tcs = new TaskCompletionSource<bool>();
        yesBtn.Click += (_, _) => { tcs.TrySetResult(true);  confirmWindow.Close(); };
        noBtn.Click  += (_, _) => { tcs.TrySetResult(false); confirmWindow.Close(); };
        confirmWindow.Closed += (_, _) => tcs.TrySetResult(false);

        await confirmWindow.ShowDialog(owner ?? confirmWindow);
        if (await tcs.Task)
        {
            _db.DeleteTask(_task.Id);
            TaskDeleted?.Invoke(_task.Id);
            CloseDialog(false);
        }
    }

    private Window? GetOwnerWindow()
    {
        if (App.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.Windows.FirstOrDefault(w => w.DataContext == this);
        return null;
    }

    private void CloseDialog(bool result)
    {
        if (App.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Windows.FirstOrDefault(w => w.DataContext == this)?.Close(result);
    }

    [RelayCommand]
    private async void OpenProjectsManagement()
    {
        var managementVm = new ProjectsManagementViewModel();
        var window       = new ProjectsManagementWindow { DataContext = managementVm };
        var desktop      = App.Current?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var owner = desktop?.MainWindow;

        if (owner != null && owner.IsVisible) await window.ShowDialog(owner);
        else window.Show();

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            int? currentSelectedId = SelectedProject?.Id;
            LoadProjectsData(currentSelectedId);
        });
    }
}
