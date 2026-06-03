using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;

namespace FocusFlowFinal.ViewModels;

public partial class TimerViewModel : ObservableObject
{
    private readonly IDatabaseService _db;
    private DispatcherTimer? _timer;
    private DateTime _segmentStartTime;
    private int _currentCycle = 0;

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private TimerState _state = TimerState.Idle;
    [ObservableProperty] private string _timeDisplay = "00:00";
    [ObservableProperty] private int _totalSeconds;
    [ObservableProperty] private int _elapsedSeconds;
    [ObservableProperty] private double _progress;

    [ObservableProperty] private TimerTemplate? _selectedTemplate;
    [ObservableProperty] private ObservableCollection<TimerTemplate> _templates = new();
    [ObservableProperty] private TaskItem? _currentTask;

    [ObservableProperty] private int _workMinutes = 25;
    [ObservableProperty] private int _breakMinutes = 5;
    [ObservableProperty] private int _cycles = 4;

    [ObservableProperty] private bool _isCustomMode;
    [ObservableProperty] private string _newTemplateName = string.Empty;

    public TimerViewModel(IDatabaseService db)
    {
        _db = db;
        LoadTemplates();

        WorkMinutes = 25;
        BreakMinutes = 5;
        Cycles = 4;
    }

    private void LoadTemplates()
    {
        Templates.Clear();
        var readonlyTemplates = _db.GetAllTimerTemplates();
        foreach (var t in readonlyTemplates)
        {
            Templates.Add(t);
        }
    }

    partial void OnSelectedTemplateChanged(TimerTemplate? value)
    {
        if (value != null && !IsCustomMode)
        {
            WorkMinutes = value.WorkMinutes;
            BreakMinutes = value.BreakMinutes;
            Cycles = value.Cycles;
        }
    }

    [RelayCommand]
    private void CreateTemplate()
    {
        if (string.IsNullOrWhiteSpace(NewTemplateName))
            return;

        var newTemplate = new TimerTemplate
        {
            Id = 0,
            Name = NewTemplateName,
            WorkMinutes = WorkMinutes,
            BreakMinutes = BreakMinutes,
            Cycles = Cycles
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
                _currentCycle = 1;
                _segmentStartTime = DateTime.Now;
                TotalSeconds = WorkMinutes * 60;
                ElapsedSeconds = 0;
                State = TimerState.Working;

                var session = new FocusSession
                {
                    TaskId = CurrentTask?.Id,
                    StartTime = DateTime.Now,
                    PlannedMinutes = WorkMinutes,
                    IsCompleted = false
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
                activeSession.EndTime = DateTime.Now;
                activeSession.ActualMinutes = (int)(activeSession.EndTime.Value - activeSession.StartTime).TotalMinutes;
                activeSession.IsCompleted = true;
                _db.UpdateFocusSession(activeSession);
            }
            State = TimerState.Idle;
            TimeDisplay = "00:00";
            ElapsedSeconds = 0;
            Progress = 0;
            CurrentTask = null;

            // СВЯЗЬ В РЕАЛЬНОМ ВРЕМЕНИ: Обновляем мини-статистику на главном экране
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

    private void TimerTick(object? sender, EventArgs e)
    {
        ElapsedSeconds++;
        Progress = (double)ElapsedSeconds / TotalSeconds;
        TimeDisplay = $"{ElapsedSeconds / 60:D2}:{ElapsedSeconds % 60:D2}";

        if (ElapsedSeconds >= TotalSeconds)
        {
            var settings = AppSettings.Load();
            if (settings.SoundNotifications)
            {
                Console.Beep();
            }

            if (State == TimerState.Working)
            {
                _currentCycle++;
                if (_currentCycle > Cycles * 2 - 1)
                {
                    StopTimer();
                    return;
                }
                State = TimerState.Break;
                TotalSeconds = BreakMinutes * 60;
            }
            else if (State == TimerState.Break)
            {
                State = TimerState.Working;
                TotalSeconds = WorkMinutes * 60;
                _currentCycle++;
                if (_currentCycle > Cycles * 2)
                {
                    StopTimer();
                    return;
                }
            }
            _segmentStartTime = DateTime.Now;
            ElapsedSeconds = 0;
            Progress = 0;

            // Пересчитываем аналитику при завершении цикла Помодоро
            TriggerMainStatsRefresh();
        }
    }

    private void TriggerMainStatsRefresh()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow?.DataContext is MainViewModel mainVm)
            {
                mainVm.RefreshTodayMiniStats();
            }
        }
    }
}
