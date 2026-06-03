using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool _isGeneralTab = true;
    [ObservableProperty] private bool _isNotificationsTab;
    [ObservableProperty] private bool _isHotkeysTab;
    [ObservableProperty] private bool _isDataTab;

    [ObservableProperty] private string _selectedLanguage = "Русский";
    public ObservableCollection<string> Languages { get; } = new() { "Русский", "English" };

    [ObservableProperty] private bool _systemNotificationsEnabled = true;
    [ObservableProperty] private bool _soundNotificationsEnabled = true;

    [ObservableProperty] private int _currentThemeMode; // 0=Light,1=Dark,2=Auto

    public SettingsViewModel()
    {
        var settings = AppSettings.Load();
        CurrentThemeMode = settings.ThemeMode;
        SelectedLanguage = settings.Language;
        SystemNotificationsEnabled = settings.SystemNotifications;
        SoundNotificationsEnabled = settings.SoundNotifications;
    }

    [RelayCommand]
    private void SelectTab(string tabName)
    {
        IsGeneralTab = tabName == "General";
        IsNotificationsTab = tabName == "Notifications";
        IsHotkeysTab = tabName == "Hotkeys";
        IsDataTab = tabName == "Data";
    }

    [RelayCommand]
    private void SetTheme(string themeModeStr)
    {
        int newMode = int.Parse(themeModeStr);
        var app = App.Current as App;
        if (app != null)
        {
            // Применяем тему через App (один вызов!)
            app.ApplyTheme(newMode);
            CurrentThemeMode = newMode;

            // Сохраняем в настройки
            var settings = AppSettings.Load();
            settings.ThemeMode = newMode;
            settings.Save();
        }
    }

    [RelayCommand]
    private void Save()
    {
        var settings = AppSettings.Load();
        settings.ThemeMode = CurrentThemeMode;
        settings.Language = SelectedLanguage;
        settings.SystemNotifications = SystemNotificationsEnabled;
        settings.SoundNotifications = SoundNotificationsEnabled;
        settings.Save();

        LocalizationService.Instance.CurrentLanguage = settings.Language;
        CloseWindow();
    }

    [RelayCommand]
    private void Cancel() => CloseWindow();

    [RelayCommand]
    private async System.Threading.Tasks.Task ExportData()
    {
        // Логика экспорта
    }

    [RelayCommand]
    private void ClearAllData()
    {
        // Логика очистки данных
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