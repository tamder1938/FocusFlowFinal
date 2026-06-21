using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace FocusFlowFinal.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public LocalizationService Loc => LocalizationService.Instance;

    // ИСПРАВЛЕНО (Часть 2, п.5): подмодель вкладки "Аккаунт"
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

    // ИСПРАВЛЕНО (Проблема 1, 2): временные значения, применяются только по «Сохранить».
    // CurrentThemeMode хранит выбор пользователя ДО сохранения и используется
    // только для подсветки выбранной карточки темы — само приложение тему не меняет.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLightSelected))]
    [NotifyPropertyChangedFor(nameof(IsDarkSelected))]
    [NotifyPropertyChangedFor(nameof(IsAutoSelected))]
    private int _currentThemeMode; // 0=Light,1=Dark,2=Auto

    // ИСПРАВЛЕНО (Проблема 1): вычисляемые флаги для подсветки карточек темы в XAML
    public bool IsLightSelected => CurrentThemeMode == 0;
    public bool IsDarkSelected  => CurrentThemeMode == 1;
    public bool IsAutoSelected  => CurrentThemeMode == 2;

    [ObservableProperty] private bool _systemNotificationsEnabled = true;
    [ObservableProperty] private bool _soundNotificationsEnabled = true;
    [ObservableProperty] private bool _markTaskCompletedOnTimerFinish;
    [ObservableProperty] private bool _financeModuleEnabled;
    [ObservableProperty] private bool _habitTrackerEnabled;

    // Дополнительные модули
    [ObservableProperty] private bool _backgroundSoundsEnabled;
    [ObservableProperty] private bool _moodTrackerEnabled;
    [ObservableProperty] private bool _notesAndDiaryEnabled;
    [ObservableProperty] private bool _workoutTrackerEnabled;
    [ObservableProperty] private bool _mediaTrackerEnabled;
    [ObservableProperty] private bool _extendedStatisticsEnabled;

    // ── Цветовая схема ────────────────────────────────────────────
    [ObservableProperty] private bool    _useSystemAccent;
    [ObservableProperty] private string  _customAccentHex = "#2F6FED";
    [ObservableProperty] private string? _selectedPresetHex;

    private bool _syncingAccent;

    public bool IsCustomAccent
    {
        get => !UseSystemAccent;
        set => UseSystemAccent = !value;
    }

    public bool IsHexValid   => Regex.IsMatch(CustomAccentHex ?? "", @"^#[0-9A-Fa-f]{6}$");
    public bool IsHexInvalid => !IsHexValid;

    public string[] PresetColors => new[]
    {
        "#2F6FED", "#0EA5A0", "#5FB87A", "#7C3AED",
        "#EC4899", "#F59E0B", "#DC4F4F", "#475569"
    };

    partial void OnUseSystemAccentChanged(bool v)
    {
        OnPropertyChanged(nameof(IsCustomAccent));
        ApplyLiveAccent();
    }

    partial void OnCustomAccentHexChanged(string v)
    {
        OnPropertyChanged(nameof(IsHexValid));
        OnPropertyChanged(nameof(IsHexInvalid));
        if (!_syncingAccent)
        {
            _syncingAccent = true;
            SelectedPresetHex = PresetColors.Contains(v) ? v : null;
            _syncingAccent = false;
        }
        if (!UseSystemAccent && IsHexValid)
            ApplyLiveAccent();
    }

    partial void OnSelectedPresetHexChanged(string? v)
    {
        if (_syncingAccent || v == null) return;
        _syncingAccent = true;
        CustomAccentHex = v;
        _syncingAccent = false;
    }

    private void ApplyLiveAccent()
    {
        try
        {
            Color c;
            if (UseSystemAccent)
                c = App.GetSystemAccentColor();
            else if (IsHexValid)
                c = Color.Parse(CustomAccentHex);
            else return;
            ((App)App.Current!).ApplyAccent(c);
        }
        catch { }
    }

    // Исходные значения — для отката при Cancel()
    private readonly int    _originalThemeMode;
    private readonly string _originalLanguage;
    private readonly bool   _originalSystemNotifications;
    private readonly bool   _originalSoundNotifications;
    private readonly bool   _originalUseSystemAccent;
    private readonly string _originalCustomAccentHex;

    // Зона "Данные" — статус экспорта/очистки
    [ObservableProperty] private string _exportStatusText = string.Empty;
    [ObservableProperty] private bool _exportStatusIsError;
    [ObservableProperty] private bool _exportStatusVisible;

    // ── Яндекс.Карты ─────────────────────────────────────────────────────
    [ObservableProperty] private string _yandexSuggestKey  = string.Empty;
    [ObservableProperty] private string _yandexGeocoderKey = string.Empty;
    [ObservableProperty] private string _yandexStaticKey   = string.Empty;
    [ObservableProperty] private string _apiKeysSavedMsg   = string.Empty;

    public SettingsViewModel()
    {
        var settings = AppSettings.Load();

        _originalThemeMode           = settings.ThemeMode;
        _originalLanguage             = settings.Language;
        _originalSystemNotifications  = settings.SystemNotifications;
        _originalSoundNotifications   = settings.SoundNotifications;
        _originalUseSystemAccent      = settings.UseSystemAccent;
        _originalCustomAccentHex      = settings.CustomAccentHex ?? "#2F6FED";

        CurrentThemeMode                 = settings.ThemeMode;
        SelectedLanguage                 = settings.Language;
        SystemNotificationsEnabled       = settings.SystemNotifications;
        SoundNotificationsEnabled        = settings.SoundNotifications;
        MarkTaskCompletedOnTimerFinish   = settings.MarkTaskCompletedOnTimerFinish;
        FinanceModuleEnabled             = settings.FinanceModuleEnabled;
        HabitTrackerEnabled              = settings.IsHabitTrackerEnabled;
        BackgroundSoundsEnabled          = settings.BackgroundSoundsEnabled;
        MoodTrackerEnabled               = settings.MoodTrackerEnabled;
        NotesAndDiaryEnabled             = settings.NotesAndDiaryEnabled;
        WorkoutTrackerEnabled            = settings.WorkoutTrackerEnabled;
        MediaTrackerEnabled              = settings.MediaTrackerEnabled;
        ExtendedStatisticsEnabled        = settings.ExtendedStatisticsEnabled;

        _useSystemAccent  = settings.UseSystemAccent;
        _customAccentHex  = settings.CustomAccentHex ?? "#2F6FED";
        _selectedPresetHex = PresetColors.Contains(_customAccentHex) ? _customAccentHex : null;

        YandexSuggestKey  = settings.YandexSuggestApiKey  ?? string.Empty;
        YandexGeocoderKey = settings.YandexGeocoderApiKey ?? string.Empty;
        YandexStaticKey   = settings.YandexStaticApiKey   ?? string.Empty;

        // ИСПРАВЛЕНО (Часть 2, п.5): создаём подмодель вкладки "Аккаунт"
        var services = ((App)App.Current!).Services!;
        Account = new AccountSettingsViewModel(
            services.GetRequiredService<IAuthService>(),
            services.GetRequiredService<IPaymentService>(),
            services.GetRequiredService<ISyncService>());

        // ИСПРАВЛЕНО (Проблема 9): обновляем локализованные подписи вкладок при смене языка
        Loc.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(AccountTabLabel));
            OnPropertyChanged(nameof(GeneralTabLabel));
            OnPropertyChanged(nameof(NotificationsTabLabel));
            OnPropertyChanged(nameof(HotkeysTabLabel));
            OnPropertyChanged(nameof(DataTabLabel));
            OnPropertyChanged(nameof(FunctionsTabLabel));
        };
    }

    // ── Локализованные заголовки вкладок (Проблема 9; Часть 2, п.5) ─
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
    }

    // ИСПРАВЛЕНО (Проблема 1): SetTheme больше НЕ применяет тему мгновенно.
    // Только запоминает выбор в памяти (CurrentThemeMode), используемый для
    // подсветки выбранной карточки. Реальное применение — в SaveCommand.
    [RelayCommand]
    private void SetTheme(string themeModeStr)
    {
        if (int.TryParse(themeModeStr, out int newMode))
            CurrentThemeMode = newMode;
    }

    // ИСПРАВЛЕНО (Проблема 1, 2): применяем тему, язык и уведомления
    // ТОЛЬКО при нажатии «Сохранить».
    [RelayCommand]
    private void Save()
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
        settings.UseSystemAccent                 = UseSystemAccent;
        settings.CustomAccentHex                 = CustomAccentHex;
        settings.Save();

        var app = App.Current as App;
        app?.ApplyTheme(settings.ThemeMode);
        app?.ApplyAccentFromSettings(settings);

        // Применяем язык
        LocalizationService.Instance.CurrentLanguage = settings.Language;

        // Уведомляем главное окно — обновить данные (тема могла повлиять
        // на формат/локализацию задач, дату и т.п.)
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
            Process.Start(new ProcessStartInfo("https://developer.tech.yandex.ru")
                { UseShellExecute = true });
        }
        catch { }
    }

    [RelayCommand]
    private void Cancel()
    {
        // Откатываем live-preview акцента к исходному значению
        try
        {
            Color originalColor = _originalUseSystemAccent
                ? App.GetSystemAccentColor()
                : Color.Parse(_originalCustomAccentHex);
            ((App)App.Current!).ApplyAccent(originalColor);
        }
        catch { }
        CloseWindow();
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task ExportData()
    {
        var owner = GetOwnerWindow();
        if (owner == null) return;

        var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(owner);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = Loc["ExportBtn"],
            DefaultExtension = "json",
            SuggestedFileName = $"FocusFlow_backup_{System.DateTime.Now:yyyy-MM-dd_HH-mm}.json",
            FileTypeChoices = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("JSON") { Patterns = new[] { "*.json" } }
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

            var json = System.Text.Json.JsonSerializer.Serialize(dto, new System.Text.Json.JsonSerializerOptions
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

    private async System.Threading.Tasks.Task<bool> ShowConfirmDialog(Avalonia.Controls.Window owner, string message)
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
            Width = 360,
            Height = 170,
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

        yesBtn.Click += (_, _) => { tcs.TrySetResult(true); dialog.Close(); };
        noBtn.Click  += (_, _) => { tcs.TrySetResult(false); dialog.Close(); };
        dialog.Closed += (_, _) => tcs.TrySetResult(false);

        await dialog.ShowDialog(owner);
        return await tcs.Task;
    }

    private void CloseWindow()
    {
        if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var win = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            win?.Close();
        }
    }
}
