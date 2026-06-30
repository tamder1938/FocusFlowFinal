using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FocusFlowFinal.ViewModels;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    public LocalizationService Loc => LocalizationService.Instance;

    public AccountSettingsViewModel Account { get; }

    // ── Вкладки ──────────────────────────────────────────────────────
    [ObservableProperty] private bool _isAccountTab = true;
    [ObservableProperty] private bool _isGeneralTab;
    [ObservableProperty] private bool _isNotificationsTab;
    [ObservableProperty] private bool _isHotkeysTab;
    [ObservableProperty] private bool _isDataTab;
    [ObservableProperty] private bool _isFunctionsTab;

    [ObservableProperty] private string _selectedLanguage = "Русский";
    public ObservableCollection<string> Languages { get; } = new() { "Русский", "English" };

    // Light/Dark/Auto theme cards
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLightSelected))]
    [NotifyPropertyChangedFor(nameof(IsDarkSelected))]
    [NotifyPropertyChangedFor(nameof(IsAutoSelected))]
    private int _currentThemeMode;

    public bool IsLightSelected => CurrentThemeMode == 0;
    public bool IsDarkSelected  => CurrentThemeMode == 1;
    public bool IsAutoSelected  => CurrentThemeMode == 2;

    // ── Модули ───────────────────────────────────────────────────────
    [ObservableProperty] private bool _systemNotificationsEnabled = true;
    [ObservableProperty] private bool _soundNotificationsEnabled = true;
    [ObservableProperty] private bool _markTaskCompletedOnTimerFinish;
    [ObservableProperty] private bool _financeModuleEnabled;
    [ObservableProperty] private bool _habitTrackerEnabled;
    [ObservableProperty] private bool _backgroundSoundsEnabled;
    [ObservableProperty] private bool _moodTrackerEnabled;
    [ObservableProperty] private bool _notesAndDiaryEnabled;
    [ObservableProperty] private bool _workoutTrackerEnabled;
    [ObservableProperty] private bool _mediaTrackerEnabled;
    [ObservableProperty] private bool _extendedStatisticsEnabled;

    // ── Статус подписки (premium gating) ────────────────────────────────
    [ObservableProperty] private bool _isPremiumActive;
    [ObservableProperty] private bool _isNotPremiumActive;

    private void RefreshPremiumState()
    {
        var services = ((App)App.Current!).Services!;
        var entitlement = services.GetRequiredService<IEntitlementService>();
        IsPremiumActive    = entitlement.IsPremiumActive;
        IsNotPremiumActive = !IsPremiumActive;
    }

    // ── Палитра тем ──────────────────────────────────────────────────
    public ObservableCollection<ThemeTileItem> ThemeTiles { get; } = new();

    private AppTheme _selectedTheme = AppTheme.Standard;
    public AppTheme SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (_selectedTheme == value) return;
            _selectedTheme = value;
            foreach (var t in ThemeTiles)
                t.IsSelected = t.Theme == value;
            OnPropertyChanged();
        }
    }

    // Исходное значение (для отката при Cancel)
    private readonly AppTheme _savedTheme;
    private readonly int      _originalThemeMode;
    private readonly string   _originalLanguage;
    private readonly bool     _originalSystemNotifications;
    private readonly bool     _originalSoundNotifications;

    // Экспорт/очистка данных
    [ObservableProperty] private string _exportStatusText = string.Empty;
    [ObservableProperty] private bool   _exportStatusIsError;
    [ObservableProperty] private bool   _exportStatusVisible;

    // Яндекс.Карты
    [ObservableProperty] private string _yandexSuggestKey  = string.Empty;
    [ObservableProperty] private string _yandexGeocoderKey = string.Empty;
    [ObservableProperty] private string _yandexStaticKey   = string.Empty;
    [ObservableProperty] private string _apiKeysSavedMsg   = string.Empty;

    // ── Горячие клавиши ──────────────────────────────────────────────
    [ObservableProperty] private string _editHotkeyDay     = string.Empty;
    [ObservableProperty] private string _editHotkeyWeek    = string.Empty;
    [ObservableProperty] private string _editHotkeyMonth   = string.Empty;
    [ObservableProperty] private string _editHotkeyYear    = string.Empty;
    [ObservableProperty] private string _editHotkeyNewTask = string.Empty;
    [ObservableProperty] private string _editHotkeyToday   = string.Empty;
    [ObservableProperty] private string _hotkeyStatusMessage = string.Empty;
    [ObservableProperty] private bool   _hotkeyStatusIsError;

    // DEBUG: счётчик живых экземпляров
    private static int _aliveCount;
    internal static int AliveCount => _aliveCount;

    public SettingsViewModel()
    {
        Interlocked.Increment(ref _aliveCount);
        Debug.WriteLine($"[SettingsVM] Created. Alive={_aliveCount}");

        var settings = AppSettings.Load();

        _savedTheme                  = settings.SelectedTheme;
        _originalThemeMode           = settings.ThemeMode;
        _originalLanguage            = settings.Language;
        _originalSystemNotifications = settings.SystemNotifications;
        _originalSoundNotifications  = settings.SoundNotifications;

        CurrentThemeMode               = settings.ThemeMode;
        SelectedLanguage               = settings.Language;
        SystemNotificationsEnabled     = settings.SystemNotifications;
        SoundNotificationsEnabled      = settings.SoundNotifications;
        MarkTaskCompletedOnTimerFinish = settings.MarkTaskCompletedOnTimerFinish;
        FinanceModuleEnabled           = settings.FinanceModuleEnabled;
        HabitTrackerEnabled            = settings.IsHabitTrackerEnabled;
        BackgroundSoundsEnabled        = settings.BackgroundSoundsEnabled;
        MoodTrackerEnabled             = settings.MoodTrackerEnabled;
        NotesAndDiaryEnabled           = settings.NotesAndDiaryEnabled;
        WorkoutTrackerEnabled          = settings.WorkoutTrackerEnabled;
        MediaTrackerEnabled            = settings.MediaTrackerEnabled;
        ExtendedStatisticsEnabled      = settings.ExtendedStatisticsEnabled;

        YandexSuggestKey  = settings.YandexSuggestApiKey  ?? string.Empty;
        YandexGeocoderKey = settings.YandexGeocoderApiKey ?? string.Empty;
        YandexStaticKey   = settings.YandexStaticApiKey   ?? string.Empty;

        // Строим плитки тем из каталога
        foreach (var (theme, palette) in ThemeCatalog.Palettes)
        {
            ThemeTiles.Add(new ThemeTileItem
            {
                Theme      = theme,
                Name       = palette.Name,
                ThemeTag   = theme.ToString(),
                AccentBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(palette.Accent)),
                LightBrush  = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(palette.Light)),
                IsSelected  = theme == settings.SelectedTheme,
            });
        }
        _selectedTheme = settings.SelectedTheme;

        LoadHotkeyBindings();

        var services = ((App)App.Current!).Services!;
        Account = new AccountSettingsViewModel(
            services.GetRequiredService<IAuthService>(),
            services.GetRequiredService<IPaymentService>(),
            services.GetRequiredService<ISyncService>(),
            services.GetRequiredService<ICurrentWorkspace>());

        Loc.PropertyChanged += OnLocChanged;
        RefreshPremiumState();
    }

    // no-op: сохранено для совместимости с SettingsWindow.OnOpened
    internal void MarkInitialized() { }

    private void OnLocChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(AccountTabLabel));
        OnPropertyChanged(nameof(GeneralTabLabel));
        OnPropertyChanged(nameof(NotificationsTabLabel));
        OnPropertyChanged(nameof(HotkeysTabLabel));
        OnPropertyChanged(nameof(DataTabLabel));
        OnPropertyChanged(nameof(FunctionsTabLabel));
    }

    public void Dispose()
    {
        Interlocked.Decrement(ref _aliveCount);
        Debug.WriteLine($"[SettingsVM] Disposed. Alive={_aliveCount}");
        Loc.PropertyChanged -= OnLocChanged;
    }

    // ── Локализованные заголовки вкладок ─────────────────────────────
    public string AccountTabLabel       => Loc["Account"];
    public string GeneralTabLabel       => Loc["General"];
    public string NotificationsTabLabel => Loc["Notifications"];
    public string HotkeysTabLabel       => Loc["Hotkeys"];
    public string DataTabLabel          => Loc["Data"];
    public string FunctionsTabLabel     => Loc["Settings_Functions"];

    [RelayCommand]
    private void SelectTab(string tabName)
    {
        IsAccountTab       = tabName == "Account";
        IsGeneralTab       = tabName == "General";
        IsNotificationsTab = tabName == "Notifications";
        IsHotkeysTab       = tabName == "Hotkeys";
        IsDataTab          = tabName == "Data";
        IsFunctionsTab     = tabName == "Functions";
        if (tabName == "Functions") RefreshPremiumState();
    }

    [RelayCommand]
    private void GoToSubscription()
    {
        SelectTab("Account");
    }

    [RelayCommand]
    private void SetTheme(string themeModeStr)
    {
        if (int.TryParse(themeModeStr, out int newMode))
            CurrentThemeMode = newMode;
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task Save()
    {
        var settings = AppSettings.Load();
        settings.ThemeMode                       = CurrentThemeMode;
        settings.Language                        = SelectedLanguage;
        settings.SystemNotifications             = SystemNotificationsEnabled;
        settings.SoundNotifications              = SoundNotificationsEnabled;
        settings.MarkTaskCompletedOnTimerFinish  = MarkTaskCompletedOnTimerFinish;
        settings.FinanceModuleEnabled            = FinanceModuleEnabled;
        settings.IsHabitTrackerEnabled           = HabitTrackerEnabled;
        settings.BackgroundSoundsEnabled         = BackgroundSoundsEnabled;
        settings.MoodTrackerEnabled              = MoodTrackerEnabled;
        settings.NotesAndDiaryEnabled            = NotesAndDiaryEnabled;
        settings.WorkoutTrackerEnabled           = WorkoutTrackerEnabled;
        settings.MediaTrackerEnabled             = MediaTrackerEnabled;
        settings.ExtendedStatisticsEnabled       = ExtendedStatisticsEnabled;
        settings.SelectedTheme                   = SelectedTheme;
        settings.Save();

        var app = App.Current as App;
        app?.ApplyTheme(settings.ThemeMode);
        ThemeService.Instance.ApplyTheme(settings.SelectedTheme);

        LocalizationService.Instance.CurrentLanguage = settings.Language;

        RefreshMainWindow();
        CloseWindow();
    }

    [RelayCommand]
    private void SaveApiKeys()
    {
        var settings = AppSettings.Load();
        settings.YandexSuggestApiKey  = YandexSuggestKey.Trim();
        settings.YandexGeocoderApiKey = YandexGeocoderKey.Trim();
        settings.YandexStaticApiKey   = YandexStaticKey.Trim();
        settings.Save();
        ApiKeysSavedMsg = Loc["Maps_KeysSaved"];
    }

    [RelayCommand]
    private void OpenYandexDev()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                "https://developer.tech.yandex.ru") { UseShellExecute = true });
        }
        catch { }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow();
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task ExportData()
    {
        var owner = GetOwnerWindow();
        if (owner == null) return;

        var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(owner);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = Loc["ExportBtn"],
                DefaultExtension = "json",
                SuggestedFileName = $"FocusFlow_backup_{System.DateTime.Now:yyyy-MM-dd_HH-mm}.json",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("JSON")
                        { Patterns = new[] { "*.json" } }
                }
            });

        if (file == null) return;

        try
        {
            var services = ((App)App.Current!).Services!;
            var db = services.GetRequiredService<IDatabaseService>();

            var rangeStart = new System.DateTime(2000, 1, 1);
            var rangeEnd   = new System.DateTime(2100, 1, 1);

            var tasks    = db.GetAllTasks().ToList();
            var projects = db.GetAllProjects().ToList();
            var sessions = db.GetSessionsForPeriod(rangeStart, rangeEnd).ToList();
            var events   = db.GetEventsForPeriod(rangeStart, rangeEnd)
                              .GroupBy(e => e.Id).Select(g => g.First()).ToList();

            var dto = new
            {
                ExportDate = System.DateTime.Now,
                AppVersion = "FocusFlow 1.0",
                Tasks = tasks,
                Events = events,
                Projects = projects,
                Sessions = sessions
            };

            var json = System.Text.Json.JsonSerializer.Serialize(dto,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                });

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new System.IO.StreamWriter(stream);
            await writer.WriteAsync(json);

            ShowExportStatus($"✅ {Loc["ExportSuccess"]}", false);
        }
        catch (System.Exception ex)
        {
            ShowExportStatus($"❌ {Loc["ExportError"]}{ex.Message}", true);
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task ClearAllData()
    {
        var owner = GetOwnerWindow();
        if (owner == null) return;

        bool confirmed = await ShowConfirmDialog(owner, Loc["ClearConfirm"]);
        if (!confirmed) return;

        try
        {
            var services = ((App)App.Current!).Services!;
            var db = services.GetRequiredService<IDatabaseService>();
            db.ClearAllData();
            services.GetRequiredService<ITemplateService>().ClearAll();

            RefreshMainWindow();
            ShowExportStatus($"✅ {Loc["ClearSuccess"]}", false);
        }
        catch (System.Exception ex)
        {
            ShowExportStatus($"❌ {ex.Message}", true);
        }
    }

    private void ShowExportStatus(string message, bool isError)
    {
        ExportStatusText    = message;
        ExportStatusIsError = isError;
        ExportStatusVisible = true;
    }

    private void RefreshMainWindow()
    {
        if (App.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow?.DataContext is MainViewModel mainVm)
            {
                mainVm.CurrentTaskListViewModel?.RefreshTasks();
                mainVm.RefreshTodayMiniStats();
                mainVm.RefreshFinanceModuleState();
                mainVm.RefreshHabitTrackerState();
                mainVm.RefreshAllFeatureFlags();

                if (mainVm.CurrentCalendarView is DayViewModel dayVm) dayVm.LoadEvents();
                else if (mainVm.CurrentCalendarView is WeekViewModel weekVm) weekVm.RefreshWeek();
                else if (mainVm.CurrentCalendarView is MonthViewModel monthVm) monthVm.RefreshMonth();
                else if (mainVm.CurrentCalendarView is YearViewModel yearVm) yearVm.GoToYear(yearVm.CurrentYear);
            }
        }
    }

    private Avalonia.Controls.Window? GetOwnerWindow()
    {
        if (App.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.Windows.FirstOrDefault(w => w.DataContext == this);
        return null;
    }

    private async System.Threading.Tasks.Task<bool> ShowConfirmDialog(
        Avalonia.Controls.Window owner, string message)
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
            Title = Loc["DangerZone"],
            Width = 360, Height = 170,
            WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
            Background = (Avalonia.Media.IBrush?)App.Current?.Resources["CardBackground"],
            CanResize = false,
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
                        Foreground = (Avalonia.Media.IBrush?)App.Current?.Resources["PrimaryText"]
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

        yesBtn.Click += (_, _) => { tcs.TrySetResult(true);  dialog.Close(); };
        noBtn.Click  += (_, _) => { tcs.TrySetResult(false); dialog.Close(); };
        dialog.Closed += (_, _) => tcs.TrySetResult(false);

        await dialog.ShowDialog(owner);
        return await tcs.Task;
    }

    private void CloseWindow()
    {
        if (App.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var win = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            win?.Close();
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task ShowTodayStats()
    {
        var services = ((App)App.Current!).Services!;
        var svc      = services.GetRequiredService<IYearStatisticsService>();
        var date     = System.DateTime.Today;
        var activity = svc.GetDayActivity(date);
        var dlg      = new FocusFlowFinal.Views.DayStatsDialog
        {
            DataContext = new DayStatsViewModel(activity)
        };
        var owner = GetOwnerWindow();
        if (owner != null) await dlg.ShowDialog(owner);
        else dlg.Show();
    }

    // ── Горячие клавиши ──────────────────────────────────────────────

    private void LoadHotkeyBindings()
    {
        var all = Services.HotkeyService.GetAll();
        EditHotkeyDay     = all["Day"];
        EditHotkeyWeek    = all["Week"];
        EditHotkeyMonth   = all["Month"];
        EditHotkeyYear    = all["Year"];
        EditHotkeyNewTask = all["NewTask"];
        EditHotkeyToday   = all["Today"];
        HotkeyStatusMessage = string.Empty;
    }

    [RelayCommand]
    private void SaveHotkeys()
    {
        var bindings = new System.Collections.Generic.Dictionary<string, string>
        {
            ["Day"]     = EditHotkeyDay.Trim(),
            ["Week"]    = EditHotkeyWeek.Trim(),
            ["Month"]   = EditHotkeyMonth.Trim(),
            ["Year"]    = EditHotkeyYear.Trim(),
            ["NewTask"] = EditHotkeyNewTask.Trim(),
            ["Today"]   = EditHotkeyToday.Trim(),
        };

        var error = Services.HotkeyService.Validate(bindings);
        if (error != null)
        {
            HotkeyStatusMessage = error.StartsWith("Hotkeys_")
                ? Loc[error]
                : $"{Loc["Hotkeys_Invalid"]}: {error.Replace("Hotkeys_Invalid: ", "")}";
            HotkeyStatusIsError = true;
            return;
        }

        Services.HotkeyService.SaveAll(bindings);
        HotkeyStatusMessage = $"✅ {Loc["Hotkeys_Saved"]}";
        HotkeyStatusIsError = false;
    }

    [RelayCommand]
    private void ResetHotkeys()
    {
        Services.HotkeyService.ResetToDefaults();
        LoadHotkeyBindings();
    }
}
