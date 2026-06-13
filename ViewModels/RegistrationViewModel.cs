using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;   // ← добавить эту строку
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

/// <summary>
/// ИСПРАВЛЕНО (Часть 2, п.4): регистрация нового аккаунта.
/// Поля: логин, email, пароль, подтверждение пароля.
/// После успеха — автоматический вход (см. <see cref="IAuthService.RegisterAsync"/>).
/// </summary>
public partial class RegistrationViewModel : ObservableObject
{
    private readonly IAuthService _auth;

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;

    public bool? DialogResult { get; private set; }

    public RegistrationViewModel(IAuthService auth)
    {
        _auth = auth;
    }

    [RelayCommand]
    private async Task Register()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ErrorMessage = Loc["AuthFillAllFields"];
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = Loc["PasswordsDontMatch"];
            return;
        }

        IsBusy = true;
        var result = await _auth.RegisterAsync(Email, Password, Username);
        IsBusy = false;

        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage ?? Loc["AuthEmailTaken"];
            return;
        }

        var settings = AppSettings.Load();
        settings.HasCompletedFirstRun = true;
        settings.IsLocalOnlyMode = false;
        settings.SyncEnabled = result.User!.HasSyncAccess;
        settings.Save();

        DialogResult = true;
        CloseWindow();
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        CloseWindow();
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
