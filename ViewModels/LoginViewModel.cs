using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;   // ← добавить эту строку
using FocusFlowFinal.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

/// <summary>
/// ИСПРАВЛЕНО (Часть 2, п.8): экран первого запуска — вход, переход к
/// регистрации, либо продолжение в локальном режиме (без синхронизации).
/// </summary>
public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _auth;

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;

    /// <summary>Результат: true — пользователь вошёл, false — продолжил локально.</summary>
    public bool? DialogResult { get; private set; }

    public LoginViewModel(IAuthService auth)
    {
        _auth = auth;
    }

    [RelayCommand]
    private async Task Login()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = Loc["AuthFillAllFields"];
            return;
        }

        IsBusy = true;
        var result = await _auth.LoginAsync(Email, Password);
        IsBusy = false;

        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage ?? Loc["AuthInvalidCredentials"];
            return;
        }

        // ИСПРАВЛЕНО: успешный вход — включаем синхронизацию, если есть доступ
        var settings = AppSettings.Load();
        settings.HasCompletedFirstRun = true;
        settings.IsLocalOnlyMode = false;
        settings.SyncEnabled = result.User!.HasSyncAccess;
        settings.Save();

        DialogResult = true;
        CloseWindow();
    }

    [RelayCommand]
    private async Task OpenRegister()
    {
        var services = ((App)App.Current!).Services!;
        var vm = new RegistrationViewModel(services.GetRequiredService<IAuthService>());
        var window = new RegistrationWindow { DataContext = vm };

        var owner = GetOwnerWindow();
        var result = owner != null
            ? await window.ShowDialog<bool?>(owner)
            : null;

        if (result == true)
        {
            DialogResult = true;
            CloseWindow();
        }
    }

    [RelayCommand]
    private void ContinueWithoutSync()
    {
        var settings = AppSettings.Load();
        settings.HasCompletedFirstRun = true;
        settings.IsLocalOnlyMode = true;
        settings.SyncEnabled = false;
        settings.Save();

        DialogResult = false;
        CloseWindow();
    }

    private Avalonia.Controls.Window? GetOwnerWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.Windows.FirstOrDefault(w => w.DataContext == this);
        return null;
    }

    private void CloseWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var win = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            win?.Close(DialogResult);
        }
    }
}
