using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
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

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private object? _currentCalendarView;
    [ObservableProperty] private string _currentViewTitle = "Day";
    [ObservableProperty] private DateTime _selectedDate   = DateTime.Today;
    [ObservableProperty] private TaskListViewModel? _currentTaskListViewModel;
    [ObservableProperty] private TimerViewModel? _currentTimerViewModel;
    [ObservableProperty] private SoundViewModel? _soundViewModel;
    [ObservableProperty] private string _todayPlanTimeStr  = "0:00";
    [ObservableProperty] private string _todayFactTimeStr  = "0:00";
    [ObservableProperty] private double _todayFactProgress;
    [ObservableProperty] private string _todayDeviationStr = "0%";
    [ObservableProperty] private IBrush _todayDeviationColor = Brushes.Gray;

    public string SelectedDateFormatted => SelectedDate.ToString(
        "dd.MM.yyyy",
        Loc.CurrentLanguage == "English" ? new CultureInfo("en-US") : new CultureInfo("ru-RU"));

    public bool IsDayView   => CurrentCalendarView is DayViewModel;
    public bool IsWeekView  => CurrentCalendarView is WeekViewModel;
    public bool IsMonthView => CurrentCalendarView is MonthViewModel;
    public bool IsYearView  => CurrentCalendarView is YearViewModel;
    public bool IsNotDayView   => !IsDayView;
    public bool IsNotWeekView  => !IsWeekView;
    public bool IsNotMonthView => !IsMonthView;
    public bool IsNotYearView  => !IsYearView;

    // Hotkey tooltip labels — live values from HotkeyService
    public string HotkeyDayLabel     => Services.HotkeyService.Get("Day");
    public string HotkeyWeekLabel    => Services.HotkeyService.Get("Week");
    public string HotkeyMonthLabel   => Services.HotkeyService.Get("Month");
    public string HotkeyYearLabel    => Services.HotkeyService.Get("Year");
    public string HotkeyNewTaskLabel => Services.HotkeyService.Get("NewTask");
    public string HotkeyTodayLabel   => Services.HotkeyService.Get("Today");

    public void RefreshHotkeyLabels()
    {
        OnPropertyChanged(nameof(HotkeyDayLabel));
        OnPropertyChanged(nameof(HotkeyWeekLabel));
        OnPropertyChanged(nameof(HotkeyMonthLabel));
        OnPropertyChanged(nameof(HotkeyYearLabel));
        OnPropertyChanged(nameof(HotkeyNewTaskLabel));
        OnPropertyChanged(nameof(HotkeyTodayLabel));
    }

    [RelayCommand]
    private void AddNewTask() => CurrentTaskListViewModel?.AddTaskCommand.Execute(null);

    public MainViewModel(IServiceProvider services, ITemplateService templateService)
    {
        _services        = services;
        _templateService = templateService;
        _db              = _services.GetRequiredService<IDatabaseService>();

        CurrentTaskListViewModel = _services.GetRequiredService<TaskListViewModel>();
        CurrentTimerViewModel    = _services.GetRequiredService<TimerViewModel>();

        // Фоновые звуки
        var soundSvc  = _services.GetRequiredService<ISoundService>();
        var soundRepo = _services.GetRequiredService<ISoundRepository>();
        SoundViewModel = new SoundViewModel(soundSvc, soundRepo);

        // Подписка на смену фазы Помодоро
        CurrentTimerViewModel.PropertyChanged += OnTimerStateChanged;

        CurrentTaskListViewModel.StartTimerRequested += OnStartTimerRequested;
        CurrentTaskListViewModel.FocusRequested      += OnFocusRequested;

        Loc.PropertyChanged += (s, e) =>
        {
            UpdateTitleTranslation();
            OnPropertyChanged(nameof(SelectedDateFormatted));
            RefreshTodayMiniStats();
        };

        SwitchToWeek();
        RefreshTodayMiniStats();

        Services.HotkeyService.Changed += (_, _) => RefreshHotkeyLabels();

        // Реакция на смену профиля (вход/выход): обновить данные и флаги фич.
        var workspace = _services.GetRequiredService<ICurrentWorkspace>();
        workspace.WorkspaceChanged += OnWorkspaceChanged;

        // ИСПРАВЛЕНО (Часть 2, п.3): периодическая синхронизация каждые 5 минут
        // (требование: "при запуске приложения и каждые 5 минут загружаем изменения с сервера").
        _syncService = _services.GetRequiredService<ISyncService>();
        _syncTimer = new Avalonia.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(5)
        };
        _syncTimer.Tick += async (_, _) =>
        {
            if (_syncService.IsSyncAvailable)
                await _syncService.SyncDataAsync();
        };
        _syncTimer.Start();
    }

    private readonly ISyncService _syncService;
    private readonly Avalonia.Threading.DispatcherTimer _syncTimer;

    private void OnWorkspaceChanged(object? sender, EventArgs e)
    {
        // WorkspaceChanged can fire from a background thread (e.g., inside Supabase await
        // continuations that use ConfigureAwait(false)). Dispatch all UI work to the UI thread.
        Dispatcher.UIThread.Post(() =>
        {
            RefreshAllFeatureFlags();
            RefreshFinanceModuleState();
            RefreshHabitTrackerState();
            RefreshTodayMiniStats();
            CurrentTaskListViewModel?.RefreshTasks();
            RefreshCurrentCalendarView();

            var workspace = _services.GetRequiredService<ICurrentWorkspace>();
            if (workspace.CurrentOwnerKey == CurrentWorkspaceService.LocalOwner)
                _ = CheckAndEnforceLimitsAsync();
        });
    }

    private async Task CheckAndEnforceLimitsAsync()
    {
        var db = _services.GetRequiredService<IDatabaseService>();
        int taskCount    = db.GetAllTasks().Count();
        int projectCount = db.GetAllProjects().Count();
        int eventCount   = db.CountBaseEvents();

        bool limitsOk = taskCount    <= EntitlementService.TaskLimit
                     && projectCount <= EntitlementService.ProjectLimit
                     && eventCount   <= EntitlementService.EventLimit;
        if (limitsOk) return;

        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        bool? loginAgain = null;
        var vm     = new LimitEnforcementViewModel(db, _services.GetRequiredService<IAuthService>());
        var dialog = new LimitEnforcementDialog { DataContext = vm };
        vm.SetWindow(dialog);

        await dialog.ShowDialog(mainWindow);

        if (vm.SignInAgainRequested)
        {
            // Открыть окно входа
            var authSvc = _services.GetRequiredService<IAuthService>();
            var loginVm = new LoginViewModel(authSvc);
            var loginWin = new LoginWindow { DataContext = loginVm };
            await loginWin.ShowDialog(mainWindow);
        }
        else
        {
            // Обновить UI после удаления
            RefreshAllFeatureFlags();
            CurrentTaskListViewModel?.RefreshTasks();
            RefreshCurrentCalendarView();
            RefreshTodayMiniStats();
        }
    }

    // ── Переключение на дневной вид с конкретной датой ──────────────
    /// <summary>Внешняя точка входа: переключиться на DayView и показать <paramref name="date"/>.</summary>
    private void OnDaySelectedFromCalendar(DateTime date)
    {
        SelectedDate = date;
        SwitchToDay();
    }

    private void OnMonthSelectedFromYear(DateTime monthStart)
    {
        SelectedDate = monthStart;
        SwitchToMonth();
    }

    private void OnStartTimerRequested(TaskItem task) =>
        CurrentTimerViewModel?.StartTaskTimer(task);

    private void OnFocusRequested(TaskItem task) =>
        CurrentTimerViewModel?.StartFocusForTask(task);

    public void RefreshTodayMiniStats()
    {
        var today  = DateTime.Today;
        var tasks  = _db.GetTasksByDate(today).ToList();
        int plan   = tasks.Sum(t => t.PlannedDurationMinutes);
        var sesh   = _db.GetSessionsForDate(today).ToList();
        int actual = sesh.Sum(s => s.ActualMinutes > 0 ? s.ActualMinutes : s.PlannedMinutes);

        TodayPlanTimeStr   = $"{plan / 60}:{plan % 60:D2}";
        TodayFactTimeStr   = $"{actual / 60}:{actual % 60:D2}";
        TodayFactProgress  = plan > 0 ? Math.Min(100, (double)actual / plan * 100) : 0;

        if (plan > 0)
        {
            int diff    = actual - plan;
            double pct  = (double)diff / plan * 100;
            TodayDeviationStr   = pct >= 0 ? $"+{Math.Round(pct)}%" : $"{Math.Round(pct)}%";
            TodayDeviationColor = pct >= 0 ? Brush.Parse("#10B981") : Brush.Parse("#EF4444");
        }
        else
        {
            TodayDeviationStr   = "0%";
            TodayDeviationColor = Brushes.Gray;
        }
    }

    private void UpdateTitleTranslation()
    {
        if      (IsDayView)   CurrentViewTitle = Loc["TitleDay"];
        else if (IsWeekView)  CurrentViewTitle = Loc["TitleWeek"];
        else if (IsMonthView) CurrentViewTitle = Loc["TitleMonth"];
        else if (IsYearView)  CurrentViewTitle = Loc["TitleYear"];
    }

    // ── Команды ─────────────────────────────────────────────────────
    [RelayCommand] private void GoToToday()      { SelectedDate = DateTime.Today; RefreshTodayMiniStats(); }

    [RelayCommand]
    private async void OpenAnalytics()
    {
        var yearSvc = _services.GetRequiredService<IYearStatisticsService>();
        var win = new AnalyticsWindow { DataContext = new AnalyticsViewModel(_db, yearSvc) };
        var owner = GetMainWindow();
        if (owner != null) await win.ShowDialog(owner);
        else win.Show();
    }

    [RelayCommand]
    private async void OpenSettings()
    {
        var win = new SettingsWindow { DataContext = new SettingsViewModel() };
        var owner = GetMainWindow();
        if (owner != null) await win.ShowDialog(owner);
        else win.Show();
    }

    [RelayCommand]
    private async void OpenTemplates()
    {
        var win = new TemplatesWindow { DataContext = new TemplatesViewModel(_templateService, _db) };
        var owner = GetMainWindow();
        if (owner != null) await win.ShowDialog(owner);
        else win.Show();
    }

    private bool IsPremium => _services.GetRequiredService<IEntitlementService>().IsPremiumActive;

    public bool IsFinanceModuleEnabled      => AppSettings.Load().FinanceModuleEnabled      && IsPremium;
    public bool IsHabitTrackerEnabled       => AppSettings.Load().IsHabitTrackerEnabled     && IsPremium;
    public bool IsBackgroundSoundsEnabled   => AppSettings.Load().BackgroundSoundsEnabled   && IsPremium;
    public bool IsMoodTrackerEnabled        => AppSettings.Load().MoodTrackerEnabled        && IsPremium;
    public bool IsNotesAndDiaryEnabled      => AppSettings.Load().NotesAndDiaryEnabled      && IsPremium;
    public bool IsWorkoutTrackerEnabled     => AppSettings.Load().WorkoutTrackerEnabled     && IsPremium;
    public bool IsMediaTrackerEnabled       => AppSettings.Load().MediaTrackerEnabled       && IsPremium;
    public bool IsExtendedStatisticsEnabled => AppSettings.Load().ExtendedStatisticsEnabled && IsPremium;

    public void RefreshFinanceModuleState() =>
        OnPropertyChanged(nameof(IsFinanceModuleEnabled));

    public void RefreshHabitTrackerState() =>
        OnPropertyChanged(nameof(IsHabitTrackerEnabled));

    public void RefreshAllFeatureFlags()
    {
        OnPropertyChanged(nameof(IsBackgroundSoundsEnabled));
        OnPropertyChanged(nameof(IsMoodTrackerEnabled));
        OnPropertyChanged(nameof(IsNotesAndDiaryEnabled));
        OnPropertyChanged(nameof(IsWorkoutTrackerEnabled));
        OnPropertyChanged(nameof(IsMediaTrackerEnabled));
        OnPropertyChanged(nameof(IsExtendedStatisticsEnabled));
    }

    private bool RequirePremium()
    {
        if (IsPremium) return true;
        var ns = _services.GetRequiredService<INotificationService>();
        ns.Show(Loc["Premium_RequiredTitle"], Loc["Premium_RequiredNotif"], NotificationLevel.Warning);
        return false;
    }

    [RelayCommand]
    private async void OpenFinance()
    {
        if (!RequirePremium()) return;
        var ns  = _services.GetRequiredService<INotificationService>();
        var vm  = new FinanceViewModel(_db, ns);
        var win = new Views.FinanceWindow { DataContext = vm };
        var owner = GetMainWindow();
        if (owner != null) await win.ShowDialog(owner);
        else win.Show();
    }

    [RelayCommand]
    private async void OpenHabits()
    {
        if (!RequirePremium()) return;
        var vm  = new HabitViewModel(_db);
        var win = new Views.HabitWindow { DataContext = vm };
        var owner = GetMainWindow();
        if (owner != null) await win.ShowDialog(owner);
        else win.Show();
    }

    [RelayCommand]
    private async void OpenNotes()
    {
        if (!RequirePremium()) return;
        var repo   = _services.GetRequiredService<INoteRepository>();
        var export = _services.GetRequiredService<NoteExportService>();
        var vm     = new NoteViewModel(repo, export);
        var win    = new Views.NotesWindow { DataContext = vm };
        var owner  = GetMainWindow();
        if (owner != null) await win.ShowDialog(owner);
        else win.Show();
        if (CurrentCalendarView is MonthViewModel mv) mv.RefreshMonth();
    }

    [RelayCommand]
    private async void OpenWorkout()
    {
        if (!RequirePremium()) return;
        var exercises = _services.GetRequiredService<IExerciseRepository>();
        var workouts  = _services.GetRequiredService<IWorkoutRepository>();
        var initSvc   = _services.GetRequiredService<IWorkoutInitService>();
        var vm        = new WorkoutViewModel(exercises, workouts, initSvc);
        var win       = new Views.WorkoutWindow { DataContext = vm };
        var owner     = GetMainWindow();
        if (owner != null) await win.ShowDialog(owner);
        else win.Show();
    }

    [RelayCommand]
    private async void OpenMedia()
    {
        if (!RequirePremium()) return;
        var repo    = _services.GetRequiredService<IMediaRepository>();
        var posters = _services.GetRequiredService<IMediaPosterService>();
        var vm      = new MediaViewModel(repo, posters);
        var win     = new Views.MediaWindow { DataContext = vm };
        var owner   = GetMainWindow();
        if (owner != null) await win.ShowDialog(owner);
        else win.Show();
    }

    [RelayCommand]
    private async void OpenMood()
    {
        if (!RequirePremium()) return;
        var repo     = _services.GetRequiredService<IMoodRepository>();
        var photoSvc = _services.GetRequiredService<IMoodPhotoService>();
        var stats    = _services.GetRequiredService<IMoodStatisticsService>();
        var vm       = new MoodViewModel(repo, photoSvc, stats);
        var win      = new Views.MoodWindow { DataContext = vm };
        var owner    = GetMainWindow();
        if (owner != null) await win.ShowDialog(owner);
        else win.Show();
    }

    [RelayCommand]
    private async void OpenPlaces()
    {
        var repo   = _services.GetRequiredService<IPlaceRepository>();
        var maps   = _services.GetRequiredService<IYandexMapsService>();
        var export = _services.GetRequiredService<PlaceExportService>();
        var vm     = new PlacesViewModel(repo, maps, export);
        var win    = new Views.PlacesWindow { DataContext = vm };
        var owner  = GetMainWindow();
        if (owner != null) await win.ShowDialog(owner);
        else win.Show();
    }

    [RelayCommand]
    private async void OpenWishlist()
    {
        if (!RequirePremium()) return;
        var repo    = _services.GetRequiredService<IWishlistRepository>();
        var exportSvc = _services.GetRequiredService<WishlistExportService>();
        var vm      = new WishlistListViewModel(repo, exportSvc);
        var win     = new Views.WishlistWindow { DataContext = vm };
        var owner   = GetMainWindow();
        if (owner != null) await win.ShowDialog(owner);
        else win.Show();
    }

    [RelayCommand]
    private async void OpenFriends()
    {
        if (!RequirePremium()) return;
        var friendSvc = _services.GetRequiredService<IFriendService>();
        var authSvc   = _services.GetRequiredService<IAuthService>();
        var vm  = new FriendsViewModel(friendSvc, authSvc);
        var win = new Views.FriendsWindow { DataContext = vm };
        var owner = GetMainWindow();
        if (owner != null) await win.ShowDialog(owner);
        else win.Show();
    }

    [RelayCommand]
    private async void OpenCalendarShare()
    {
        if (!RequirePremium()) return;
        var shareSvc  = _services.GetRequiredService<ICalendarShareService>();
        var friendSvc = _services.GetRequiredService<IFriendService>();
        var authSvc   = _services.GetRequiredService<IAuthService>();
        var vm  = new CalendarShareViewModel(shareSvc, friendSvc, authSvc);
        var win = new Views.CalendarShareDialog { DataContext = vm };
        var owner = GetMainWindow();
        if (owner != null) await win.ShowDialog(owner);
        else win.Show();
    }

    private void OnTimerStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TimerViewModel.State)) return;
        var soundSvc = _services.GetService(typeof(ISoundService)) as ISoundService;
        if (soundSvc == null) return;

        var phase = CurrentTimerViewModel?.State switch
        {
            TimerState.Working => PomodoroPhase.Work,
            TimerState.Break   => PomodoroPhase.Break,
            _                  => PomodoroPhase.Stopped
        };
        soundSvc.OnPomodoroPhaseChanged(phase);
    }

    private Window? GetMainWindow() =>
        (App.Current?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    // ── Открытие диалога задачи ─────────────────────────────────────
    public async Task OpenTaskDialog(TaskItem task, Window? owner = null)
    {
        var vm     = new TaskDialogViewModel(task);
        vm.TaskDeleted += OnTaskDeleted;
        var window = new TaskDialog { DataContext = vm };

        if (owner == null)
        {
            if (App.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                owner = desktop.MainWindow;
        }

        await window.ShowDialog(owner ?? window);
        CurrentTaskListViewModel?.RefreshTasks();
        RefreshCurrentCalendarView();
        RefreshTodayMiniStats();
        vm.TaskDeleted -= OnTaskDeleted;
    }

    private void OnTaskDeleted(int taskId)
    {
        CurrentTaskListViewModel?.RefreshTasks();
        RefreshCurrentCalendarView();
        RefreshTodayMiniStats();
    }

    public void RefreshCurrentCalendarView()
    {
        if      (CurrentCalendarView is DayViewModel   dayVm)   dayVm.Refresh();
        else if (CurrentCalendarView is WeekViewModel  weekVm)  weekVm.RefreshWeek();
        else if (CurrentCalendarView is MonthViewModel monthVm) monthVm.RefreshMonth();
        else if (CurrentCalendarView is YearViewModel  yearVm)  yearVm.GoToYear(yearVm.CurrentYear);
    }

    // ── Переключение видов ──────────────────────────────────────────

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

        // Подписываемся на клик по заголовку дня → DayView
        vm.DaySelected += OnDaySelectedFromCalendar;

        CurrentCalendarView = vm;
        UpdateTitleTranslation();
        UpdateViewFlags();
    }

    [RelayCommand]
    private void SwitchToMonth()
    {
        var vm = _services.GetRequiredService<MonthViewModel>();
        vm.GoToMonth(SelectedDate);

        // Подписываемся на клик по числу месяца → DayView
        vm.DaySelected += OnDaySelectedFromCalendar;

        CurrentCalendarView = vm;
        UpdateTitleTranslation();
        UpdateViewFlags();
    }

    [RelayCommand]
    private void SwitchToYear()
    {
        var vm = _services.GetRequiredService<YearViewModel>();
        vm.GoToYear(SelectedDate.Year);

        // Подписываемся на клик по дню в годовом виде → DayView
        vm.DaySelected += OnDaySelectedFromCalendar;

        // Подписываемся на клик по карточке месяца → MonthView
        vm.MonthSelected += OnMonthSelectedFromYear;

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
