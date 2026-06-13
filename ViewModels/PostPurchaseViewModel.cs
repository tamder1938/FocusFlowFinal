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
/// ИСПРАВЛЕНО (Часть 2, п.7): диалог, показываемый сразу после успешной
/// покупки подписки — предлагает создать новый аккаунт или войти в
/// существующий (если пользователь уже регистрировался на другом устройстве).
/// После входа/регистрации синхронизация активируется автоматически.
/// </summary>
public partial class PostPurchaseViewModel : ObservableObject
{
    public LocalizationService Loc => LocalizationService.Instance;

    /// <summary>true, если пользователь успешно вошёл/зарегистрировался.</summary>
    public bool? DialogResult { get; private set; }

    [RelayCommand]
    private async Task CreateAccount()
    {
        var services = ((App)App.Current!).Services!;
        var vm = new RegistrationViewModel(services.GetRequiredService<IAuthService>());
        var window = new RegistrationWindow { DataContext = vm };

        var owner = GetOwnerWindow();
        var result = owner != null ? await window.ShowDialog<bool?>(owner) : null;

        if (result == true)
        {
            DialogResult = true;
            CloseWindow();
        }
    }

    [RelayCommand]
    private async Task SignIn()
    {
        var services = ((App)App.Current!).Services!;
        var vm = new LoginViewModel(services.GetRequiredService<IAuthService>());
        var window = new LoginWindow { DataContext = vm };

        var owner = GetOwnerWindow();
        if (owner != null) await window.ShowDialog(owner);
        else window.Show();

        if (vm.DialogResult == true)
        {
            DialogResult = true;
            CloseWindow();
        }
    }

    [RelayCommand]
    private void Close()
    {
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
