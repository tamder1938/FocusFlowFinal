using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class TimerViewModel : ObservableObject
{
    private readonly IDatabaseService _db;
    private DispatcherTimer? _timer;
    private DateTime _segmentStartTime;
    private int _currentCycle = 0;

    public LocalizationService Loc => LocalizationService.Instance;

    /// <summary>
    /// Срабатывает при завершении рабочего сегмента Помодоро.
    /// </summary>
    public event Action<string?>? TimerFinished;

    [ObservableProperty] private TimerState _state = TimerState.Idle;
    [ObservableProperty] private string _timeDisplay = "00:00";
    [ObservableProperty] private int _totalSeconds;
    [ObservableProperty] private int _elapsedSeconds;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressDisplay))]
    private double _progress;

    public string ProgressDisplay => $"{(int)(Progress * 100)}%";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimerSubtitle))]
    [NotifyPropertyChangedFor(nameof(CanDeleteSelectedTemplate))]
    private TimerTemplate? _selectedTemplate;

    [ObservableProperty] private ObservableCollection<TimerTemplate> _templates = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentTaskProjectName))]
    private TaskItem? _currentTask;

    // Для встроенного шаблона возвращаем локализованное имя, а не сохранённое в БД (всегда RU)
    public string TimerSubtitle
    {
        get
        {
            if (SelectedTemplate == null || SelectedTemplate.IsBuiltIn)
                return Loc["PomodoroDefault"];
            return SelectedTemplate.Name;
        }
    }

    public bool CanDeleteSelectedTemplate => SelectedTemplate != null && !SelectedTemplate.IsBuiltIn;

    /// <summary>Название проекта текущей задачи, или null если проект не задан.</summary>
    public string? CurrentTaskProjectName
    {
        get
        {
            var task = CurrentTask;
            if (task == null || !task.ProjectId.HasValue || task.ProjectId.Value <= 0)
                return null;
            try
            {
                return _db.GetAllProjects().FirstOrDefault(p => p.Id == task.ProjectId.Value)?.Name;
            }
            catch { return null; }
        }
    }

    [ObservableProperty] private int _workMinutes  = 25;
    [ObservableProperty] private int _breakMinutes = 5;
    [ObservableProperty] private int _cycles       = 4;

    [ObservableProperty] private bool _isCustomMode;
    [ObservableProperty] private string _newTemplateName = string.Empty;

    public TimerViewModel(IDatabaseService db)
    {
        _db = db;
        LoadTemplates();

        var builtIn = Templates.FirstOrDefault(t => t.IsBuiltIn);
        SelectedTemplate = builtIn ?? Templates.FirstOrDefault();

        if (SelectedTemplate != null)
        {
            WorkMinutes  = SelectedTemplate.WorkMinutes;
            BreakMinutes = SelectedTemplate.BreakMinutes;
            Cycles       = SelectedTemplate.Cycles;
        }
        else
        {
            WorkMinutes  = 25;
            BreakMinutes = 5;
            Cycles       = 4;
        }

        Loc.PropertyChanged += (_, _) =>
        {
            LoadTemplates();
            OnPropertyChanged(nameof(TimerSubtitle));
        };
    }

    private void LoadTemplates()
    {
        var previousId = SelectedTemplate?.Id;
        Templates.Clear();
        foreach (var t in _db.GetAllTimerTemplates())
        {
            // Для встроенного шаблона обновляем имя из локализации (в БД хранится RU-строка)
            if (t.IsBuiltIn)
                t.Name = Loc["PomodoroDefault"];
            Templates.Add(t);
        }
        // Восстанавливаем выбор после перезагрузки
        if (previousId.HasValue)
            SelectedTemplate = Templates.FirstOrDefault(t => t.Id == previousId) ?? SelectedTemplate;
    }

    [RelayCommand]
    private async Task DeleteTimerTemplate(TimerTemplate? template)
    {
        if (template == null || template.IsBuiltIn) return;

        var owner = GetOwnerWindow();
        if (owner == null) return;

        bool confirmed = await ShowConfirmDialog(owner, Loc["DeleteTemplateConfirm"]);
        if (!confirmed) return;

        bool wasSelected = SelectedTemplate?.Id == template.Id;
        _db.DeleteTimerTemplate(template.Id);
        LoadTemplates();

        if (wasSelected)
            SelectedTemplate = Templates.FirstOrDefault(t => t.IsBuiltIn) ?? Templates.FirstOrDefault();
    }

    private Avalonia.Controls.Window? GetOwnerWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    private async Task<bool> ShowConfirmDialog(Avalonia.Controls.Window owner, string message)
    {
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

        var yesBtn = new Avalonia.Controls.Button
        {
            Content = Loc["Yes"], Width = 90, Margin = new Avalonia.Thickness(0, 0, 8, 0),
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#EF4444")),
            Foreground = Avalonia.Media.Brushes.White,
            CornerRadius = new Avalonia.CornerRadius(6)
        };
        var noBtn = new Avalonia.Controls.Button
        {
            Content = Loc["No"], Width = 90,
            CornerRadius = new Avalonia.CornerRadius(6)
        };

        var dialog = new Avalonia.Controls.Window
        {
            Title = Loc["TemplatesHeader"],
            Width = 340, Height = 150,
            WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = (Avalonia.Media.IBrush?)Avalonia.Application.Current?.Resources["CardBackground"],
            Content = new Avalonia.Controls.StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 18,
                Children =
                {
                    new Avalonia.Controls.TextBlock
                    {
                        Text = message,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Foreground = (Avalonia.Media.IBrush?)Avalonia.Application.Current?.Resources["PrimaryText"]
                    },
                    new Avalonia.Controls.StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Children = { yesBtn, noBtn }
                    }
                }
            }
        };

        yesBtn.Click += (_, _) => { tcs.TrySetResult(true); dialog.Close(); };
        noBtn.Click  += (_, _) => { tcs.TrySetResult(false); dialog.Close(); };
        dialog.Closed += (_, _) => tcs.TrySetResult(false);

        await dialog.ShowDialog(owner);
        return await tcs.Task;
    }

    partial void OnSelectedTemplateChanged(TimerTemplate? value)
    {
        if (value != null && !IsCustomMode)
        {
            WorkMinutes  = value.WorkMinutes;
            BreakMinutes = value.BreakMinutes;
            Cycles       = value.Cycles;
        }
    }

    [RelayCommand]
    private void CreateTemplate()
    {
        if (string.IsNullOrWhiteSpace(NewTemplateName)) return;

        var newTemplate = new TimerTemplate
        {
            Id           = 0,
            Name         = NewTemplateName,
            WorkMinutes  = WorkMinutes,
            BreakMinutes = BreakMinutes,
            Cycles       = Cycles
        };

        _db.UpsertTimerTemplate(newTemplate);
        LoadTemplates();
        NewTemplateName = string.Empty;
        SelectedTemplate = Templates.LastOrDefault();
    }

    [RelayCommand]
    private void StartTimer()
    {
        if (State == TimerState.Idle || State == TimerState.Paused)
        {
            if (State == TimerState.Idle)
            {
                _currentCycle    = 1;
                _segmentStartTime = DateTime.Now;
                TotalSeconds     = WorkMinutes * 60;
                ElapsedSeconds   = 0;
                State            = TimerState.Working;

                var session = new FocusSession
                {
                    TaskId         = CurrentTask?.Id,
                    StartTime      = DateTime.Now,
                    PlannedMinutes = WorkMinutes,
                    IsCompleted    = false
                };
                _db.AddFocusSession(session);
            }
            else
            {
                State = _currentCycle % 2 == 1 ? TimerState.Working : TimerState.Break;
            }

            StartDispatcherTimer();
        }
    }

    [RelayCommand]
    private void PauseTimer()
    {
        if (State == TimerState.Working || State == TimerState.Break)
        {
            State = TimerState.Paused;
            StopDispatcherTimer();
        }
    }

    [RelayCommand]
    private void StopTimer()
    {
        if (State != TimerState.Idle)
        {
            StopDispatcherTimer();

            var activeSession = _db.GetActiveSession();
            if (activeSession != null)
            {
                activeSession.EndTime       = DateTime.Now;
                activeSession.ActualMinutes = (int)(activeSession.EndTime.Value - activeSession.StartTime).TotalMinutes;
                activeSession.IsCompleted   = true;
                _db.UpdateFocusSession(activeSession);
            }

            State          = TimerState.Idle;
            TimeDisplay    = "00:00";
            ElapsedSeconds = 0;
            Progress       = 0;
            CurrentTask    = null;

            TriggerMainStatsRefresh();
        }
    }

    public void StartFocusForTask(TaskItem task)
    {
        CurrentTask = task;
        StartTimerCommand.Execute(null);
    }

    private void StartDispatcherTimer()
    {
        if (_timer == null)
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += TimerTick;
        }
        if (!_timer.IsEnabled)
            _timer.Start();
    }

    private void StopDispatcherTimer()
    {
        _timer?.Stop();
    }

    private async void TimerTick(object? sender, EventArgs e)
    {
        ElapsedSeconds++;
        Progress = (double)ElapsedSeconds / TotalSeconds;
        var remaining = Math.Max(0, TotalSeconds - ElapsedSeconds);
        TimeDisplay = $"{remaining / 60:D2}:{remaining % 60:D2}";

        if (ElapsedSeconds < TotalSeconds) return;

        if (State == TimerState.Working)
        {
            TimerFinished?.Invoke(CurrentTask?.Title);
            _currentCycle++;

            if (_currentCycle > Cycles * 2 - 1)
            {
                StopDispatcherTimer();
                await HandleTimerCompletionAsync();
                return;
            }

            State        = TimerState.Break;
            TotalSeconds = BreakMinutes * 60;
        }
        else if (State == TimerState.Break)
        {
            State        = TimerState.Working;
            TotalSeconds = WorkMinutes * 60;
            _currentCycle++;

            if (_currentCycle > Cycles * 2)
            {
                StopDispatcherTimer();
                await HandleTimerCompletionAsync();
                return;
            }
        }

        _segmentStartTime = DateTime.Now;
        ElapsedSeconds    = 0;
        Progress          = 0;
        TriggerMainStatsRefresh();
    }

    private async Task HandleTimerCompletionAsync()
    {
        var settings = AppSettings.Load();

        // Если задача не задана или включено автовыполнение — не показываем диалог
        if (CurrentTask == null || settings.MarkTaskCompletedOnTimerFinish)
        {
            AutoCompleteCurrentTask();
            StopTimer();
            return;
        }

        var vm     = new TimerCompletionViewModel(CurrentTask);
        var dialog = new TimerCompletionDialog { DataContext = vm };
        var owner  = GetOwnerWindow();
        await dialog.ShowDialog(owner ?? dialog);

        var task = CurrentTask; // сохраняем до StopTimer()

        switch (vm.Result)
        {
            case TimerCompletionResult.Completed:
                AutoCompleteCurrentTask();
                StopTimer();
                break;

            case TimerCompletionResult.Extended:
                // Перезапускаем таймер на указанное количество минут
                _currentCycle  = 1;
                TotalSeconds   = vm.ExtendMinutes * 60;
                ElapsedSeconds = 0;
                Progress       = 0;
                State          = TimerState.Working;
                StartDispatcherTimer();
                break;

            case TimerCompletionResult.Deferred:
                CurrentTask = null;
                StopTimer();
                break;

            case TimerCompletionResult.Deleted:
                if (task != null)
                {
                    _db.DeleteTask(task.Id);
                    RefreshAfterDelete();
                }
                CurrentTask = null;
                StopTimer();
                break;

            default:
                // Диалог закрыт без действия — просто стоп
                StopTimer();
                break;
        }
    }

    private void AutoCompleteCurrentTask()
    {
        var task = CurrentTask;
        if (task == null || task.IsCompleted) return;

        task.IsCompleted = true;
        _db.UpsertTask(task);

        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow?.DataContext is MainViewModel mainVm)
            {
                mainVm.CurrentTaskListViewModel?.RefreshTasks();
                mainVm.RefreshCurrentCalendarView();
            }
        }
    }

    private void RefreshAfterDelete()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow?.DataContext is MainViewModel mainVm)
            {
                mainVm.CurrentTaskListViewModel?.RefreshTasks();
                mainVm.RefreshCurrentCalendarView();
            }
        }
    }

    private void TriggerMainStatsRefresh()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow?.DataContext is MainViewModel mainVm)
                mainVm.RefreshTodayMiniStats();
        }
    }

    public void StartTaskTimer(TaskItem task)
    {
        StopTimer();

        CurrentTask  = task;
        WorkMinutes  = task.PlannedDurationMinutes;
        BreakMinutes = 1;
        Cycles       = 1;
        _currentCycle = 1;

        TotalSeconds   = task.PlannedDurationMinutes * 60;
        ElapsedSeconds = 0;
        TimeDisplay    = "00:00";
        Progress       = 0;
        State          = TimerState.Working;

        var session = new FocusSession
        {
            TaskId         = task.Id,
            StartTime      = DateTime.Now,
            PlannedMinutes = task.PlannedDurationMinutes,
            IsCompleted    = false
        };
        _db.AddFocusSession(session);

        StartDispatcherTimer();
    }
}
