using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly IDatabaseService _db;
    private readonly ITemplateService _templateService;
    private MonthViewModel? _currentMonthVM;
    private Action<DateTime>? _daySelectedHandler;
    private int _currentThemeMode;

    public int CurrentThemeMode
    {
        get => _currentThemeMode;
        set => SetProperty(ref _currentThemeMode, value);
    }

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private object? _currentCalendarView;
    [ObservableProperty] private string _currentViewTitle = "Day";
    [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
    [ObservableProperty] private TaskListViewModel? _currentTaskListViewModel;
    [ObservableProperty] private TimerViewModel? _currentTimerViewModel;
    [ObservableProperty] private string _todayPlanTimeStr = "0:00";
    [ObservableProperty] private string _todayFactTimeStr = "0:00";
    [ObservableProperty] private double _todayFactProgress;
    [ObservableProperty] private string _todayDeviationStr = "0%";
    [ObservableProperty] private IBrush _todayDeviationColor = Brushes.Gray;

    public string SelectedDateFormatted => SelectedDate.ToString("dd.MM.yyyy", Loc.CurrentLanguage == "English" ? new CultureInfo("en-US") : new CultureInfo("ru-RU"));
    public bool IsDayView => CurrentCalendarView is DayViewModel;
    public bool IsWeekView => CurrentCalendarView is WeekViewModel;
    public bool IsMonthView => CurrentCalendarView is MonthViewModel;
    public bool IsYearView => CurrentCalendarView is YearViewModel;
    public bool IsNotDayView => !IsDayView;
    public bool IsNotWeekView => !IsWeekView;
    public bool IsNotMonthView => !IsMonthView;
    public bool IsNotYearView => !IsYearView;

    private void OnStartTimerRequested(TaskItem task)
    {
        CurrentTimerViewModel?.StartTaskTimer(task);
    }

    public MainViewModel(IServiceProvider services, ITemplateService templateService)
    {
        _services = services;
        _templateService = templateService;
        _db = _services.GetRequiredService<IDatabaseService>();

        CurrentTaskListViewModel = _services.GetRequiredService<TaskListViewModel>();
        CurrentTimerViewModel = _services.GetRequiredService<TimerViewModel>();

        CurrentTaskListViewModel.StartTimerRequested += OnStartTimerRequested;
        CurrentTaskListViewModel.FocusRequested += OnFocusRequested;

        Loc.PropertyChanged += (s, e) =>
        {
            UpdateTitleTranslation();
            OnPropertyChanged(nameof(SelectedDateFormatted));
            RefreshTodayMiniStats();
        };

        var settings = AppSettings.Load();
        CurrentThemeMode = settings.ThemeMode;

        SwitchToDay();
        RefreshTodayMiniStats();
    }

    public void RefreshTodayMiniStats()
    {
        var today = DateTime.Today;
        var tasks = _db.GetTasksByDate(today).ToList();
        int totalPlannedMinutes = tasks.Sum(t => t.PlannedDurationMinutes);
        var sessions = _db.GetSessionsForDate(today).ToList();
        int totalActualMinutes = sessions.Sum(s => s.ActualMinutes > 0 ? s.ActualMinutes : s.PlannedMinutes);

        TodayPlanTimeStr = $"{totalPlannedMinutes / 60}:{totalPlannedMinutes % 60:D2}";
        TodayFactTimeStr = $"{totalActualMinutes / 60}:{totalActualMinutes % 60:D2}";
        TodayFactProgress = totalPlannedMinutes > 0 ? Math.Min(100, (double)totalActualMinutes / totalPlannedMinutes * 100) : 0;

        if (totalPlannedMinutes > 0)
        {
            int diff = totalActualMinutes - totalPlannedMinutes;
            double pct = (double)diff / totalPlannedMinutes * 100;
            TodayDeviationStr = pct >= 0 ? $"+{Math.Round(pct)}%" : $"{Math.Round(pct)}%";
            TodayDeviationColor = pct >= 0 ? Brush.Parse("#10B981") : Brush.Parse("#EF4444");
        }
        else
        {
            TodayDeviationStr = "0%";
            TodayDeviationColor = Brushes.Gray;
        }
    }

    private void OnFocusRequested(TaskItem task) => CurrentTimerViewModel?.StartFocusForTask(task);

    private void UpdateTitleTranslation()
    {
        if (IsDayView) CurrentViewTitle = Loc["TitleDay"];
        else if (IsWeekView) CurrentViewTitle = Loc["TitleWeek"];
        else if (IsMonthView) CurrentViewTitle = Loc["TitleMonth"];
        else if (IsYearView) CurrentViewTitle = Loc["TitleYear"];
    }

    [RelayCommand] private void GoToToday() { SelectedDate = DateTime.Today; RefreshTodayMiniStats(); }
    [RelayCommand] private void OpenAnalytics() { new AnalyticsWindow { DataContext = new AnalyticsViewModel(_db) }.Show(); }
    [RelayCommand] private void OpenSettings() { new SettingsWindow { DataContext = new SettingsViewModel() }.Show(); }
    [RelayCommand] private void OpenTemplates() { new TemplatesWindow { DataContext = new TemplatesViewModel(_templateService) }.Show(); }

    [RelayCommand]
    private void ToggleTheme()
    {
        int newMode = (CurrentThemeMode + 1) % 3;
        (App.Current as App)?.ApplyTheme(newMode);
        CurrentThemeMode = newMode;
        var settings = AppSettings.Load();
        settings.ThemeMode = newMode;
        settings.Save();
    }

    // ========== ОТКРЫТИЕ ОКНА ЗАДАЧИ С ПОДПИСКОЙ НА УДАЛЕНИЕ ==========
    public async Task OpenTaskDialog(TaskItem task, Window? owner = null)
    {
        var vm = new TaskDialogViewModel(task);
        // Подписываемся на удаление задачи
        vm.TaskDeleted += OnTaskDeleted;
        var window = new TaskDialog { DataContext = vm };
        if (owner == null)
        {
            if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                owner = desktop.MainWindow;
        }
        await window.ShowDialog(owner ?? window);
        // После закрытия окна обновляем список задач и статистику
        CurrentTaskListViewModel?.RefreshTasks();
        RefreshTodayMiniStats();
        // Отписываемся (необязательно, но аккуратно)
        vm.TaskDeleted -= OnTaskDeleted;
    }

    private void OnTaskDeleted(int taskId)
    {
        // Обновляем задачу в списке и статистику
        CurrentTaskListViewModel?.RefreshTasks();
        RefreshTodayMiniStats();
    }

    // ========== ОСНОВНЫЕ МЕТОДЫ ПЕРЕКЛЮЧЕНИЯ ==========

    [RelayCommand]
    private void SwitchToDay()
    {
        var vm = _services.GetRequiredService<DayViewModel>();
        vm.SelectedDate = SelectedDate;
        CurrentCalendarView = vm;
        UpdateTitleTranslation();
        UpdateViewFlags();
    }

    [RelayCommand]
    private void SwitchToWeek()
    {
        var vm = _services.GetRequiredService<WeekViewModel>();
        vm.GoToWeek(SelectedDate);
        CurrentCalendarView = vm;
        UpdateTitleTranslation();
        UpdateViewFlags();
    }

    [RelayCommand]
    private void SwitchToMonth()
    {
        var vm = _services.GetRequiredService<MonthViewModel>();
        CurrentCalendarView = vm;
        UpdateTitleTranslation();
        UpdateViewFlags();
    }

    [RelayCommand]
    private void SwitchToYear()
    {
        var vm = _services.GetRequiredService<YearViewModel>();
        vm.GoToYear(SelectedDate.Year);
        CurrentCalendarView = vm;
        UpdateTitleTranslation();
        UpdateViewFlags();
    }

    private void UpdateViewFlags()
    {
        OnPropertyChanged(nameof(IsDayView));
        OnPropertyChanged(nameof(IsWeekView));
        OnPropertyChanged(nameof(IsMonthView));
        OnPropertyChanged(nameof(IsYearView));
        OnPropertyChanged(nameof(IsNotDayView));
        OnPropertyChanged(nameof(IsNotWeekView));
        OnPropertyChanged(nameof(IsNotMonthView));
        OnPropertyChanged(nameof(IsNotYearView));
    }
}