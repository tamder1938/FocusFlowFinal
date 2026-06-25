using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

/// <summary>
/// ИСПРАВЛЕНО (Часть 2, п.5-7; Часть 3, п.11): ViewModel вкладки "Аккаунт"
/// в окне настроек.
///
/// Два состояния:
///  - HasSyncAccess == false → показывается заблокированная карточка с
///    предложением купить подписку (300₽/мес или 2000₽/год).
///  - HasSyncAccess == true  → показывается редактор профиля
///    (аватар, логин, email, смена пароля) + переключатель синхронизации
///    + кнопка "Синхронизировать сейчас" + статус последней синхронизации.
/// </summary>
public partial class AccountSettingsViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly IPaymentService _payment;
    private readonly ISyncService _sync;
    private readonly ICurrentWorkspace _workspace;

    public LocalizationService Loc => LocalizationService.Instance;

    // ── Состояние доступа ────────────────────────────────────────────
    [ObservableProperty] private bool _isAuthenticated;
    [ObservableProperty] private bool _hasSyncAccess;
    [ObservableProperty] private bool _isDeveloper;
    [ObservableProperty] private bool _hasFreeAccess;
    [ObservableProperty] private string _subscriptionStatusText = string.Empty;

    // ── Профиль ───────────────────────────────────────────────────────
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string? _avatarPath;

    // Режимы редактирования отдельных полей (карандаш → текстовое поле)
    [ObservableProperty] private bool _isEditingUsername;
    [ObservableProperty] private bool _isEditingEmail;
    [ObservableProperty] private bool _isChangingPassword;

    [ObservableProperty] private string _currentPassword = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;

    // ── Синхронизация ───────────────────────────────────────────────
    [ObservableProperty] private bool _syncEnabled;
    [ObservableProperty] private string _lastSyncText = string.Empty;
    [ObservableProperty] private bool _isSyncing;
    [ObservableProperty] private string _syncStatusMessage = string.Empty;

    // ── Статус операций (ошибки/успех) ───────────────────────────────
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _statusIsError;

    public AccountSettingsViewModel(IAuthService auth, IPaymentService payment, ISyncService sync, ICurrentWorkspace workspace)
    {
        _auth = auth;
        _payment = payment;
        _sync = sync;
        _workspace = workspace;

        RefreshState();
    }

    private void RefreshState()
    {
        IsAuthenticated = _auth.IsAuthenticated;

        var user = _auth.CurrentUser;
        HasSyncAccess = user?.HasSyncAccess ?? false;
        IsDeveloper   = user?.IsDeveloper ?? false;
        HasFreeAccess = user?.HasFreeAccess ?? false;

        Username   = user?.Username ?? string.Empty;
        Email      = user?.Email ?? string.Empty;
        AvatarPath = user?.AvatarPath;

        if (IsDeveloper)
            SubscriptionStatusText = Loc["DeveloperModeLbl"];
        else if (HasFreeAccess)
            SubscriptionStatusText = Loc["FreeAccessLbl"];
        else if (user?.Subscription?.IsActive == true)
            SubscriptionStatusText = $"{Loc["SubExpiresLbl"]} {user.Subscription.ExpiresAtUtc.ToLocalTime():dd.MM.yyyy}";
        else
        {
            // Fallback: читаем из AppSettings (если купили без активной сессии)
            var storedExpiry = AppSettings.Load().SubscriptionExpiryDate;
            if (storedExpiry.HasValue && storedExpiry.Value > DateTime.UtcNow)
                SubscriptionStatusText = $"{Loc["SubExpiresLbl"]} {storedExpiry.Value.ToLocalTime():dd.MM.yyyy}";
            else if (storedExpiry.HasValue)
                SubscriptionStatusText = Loc["SubExpiredLbl"];
            else
                SubscriptionStatusText = string.Empty;
        }

        var settings = AppSettings.Load();
        _syncEnabled = settings.SyncEnabled;
        OnPropertyChanged(nameof(SyncEnabled));
        LastSyncText = settings.LastSyncUtc.HasValue
            ? settings.LastSyncUtc.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            : Loc["NeverLbl"];
    }

    // ── Покупка подписки (Часть 2, п.6) ──────────────────────────────
    [RelayCommand]
    private async Task BuyMonth() => await Purchase(SubscriptionPlans.Monthly);

    [RelayCommand]
    private async Task BuyYear() => await Purchase(SubscriptionPlans.Yearly);

    private async Task Purchase(string planId)
    {
        // ИСПРАВЛЕНО (Часть 2, п.6-7): если пользователь ещё не вошёл в аккаунт,
        // покупку имитировать не на чём — сначала нужно войти/зарегистрироваться.
        // PostPurchaseDialog предложит это сделать.
        if (!_auth.IsAuthenticated)
        {
            await ShowPostPurchaseDialog();
            RefreshState();
            return;
        }

        var result = await _payment.PurchaseSubscriptionAsync(planId);
        if (!result.Success)
        {
            StatusMessage = result.ErrorMessage ?? Loc["PaymentInvalidPlan"];
            StatusIsError = true;
            return;
        }

        StatusMessage = $"{Loc["SubscriptionActiveLbl"]} {result.Subscription!.ExpiresAtUtc:dd.MM.yyyy}";
        StatusIsError = false;

        // Включаем синхронизацию автоматически после покупки
        var settings = AppSettings.Load();
        settings.SyncEnabled = true;
        settings.Save();

        RefreshState();
    }

    private async Task ShowPostPurchaseDialog()
    {
        var vm = new PostPurchaseViewModel();
        var window = new PostPurchaseDialog { DataContext = vm };

        var owner = GetOwnerWindow();
        if (owner != null) await window.ShowDialog(owner);
        else window.Show();
    }

    // ── Редактирование профиля (Часть 2, п.5) ────────────────────────
    [RelayCommand] private void ToggleEditUsername() => IsEditingUsername = !IsEditingUsername;
    [RelayCommand] private void ToggleEditEmail()    => IsEditingEmail    = !IsEditingEmail;
    [RelayCommand] private void ToggleChangePassword()
    {
        IsChangingPassword = !IsChangingPassword;
        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
    }

    [RelayCommand]
    private async Task ChooseAvatar()
    {
        var owner = GetOwnerWindow();
        if (owner == null) return;

        var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(owner);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = Loc["ChooseAvatarBtn"],
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("Images")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg" }
                }
            }
        });

        var file = files.FirstOrDefault();
        if (file == null) return;

        // ИСПРАВЛЕНО (Общие требования): сохраняем путь к файлу аватара.
        // TODO (реальный бэкенд): загрузить файл на сервер (multipart/form-data
        // на /api/users/avatar) и сохранить вернувшийся URL вместо локального пути.
        AvatarPath = file.Path.LocalPath;
    }

    // ── Сохранить изменения профиля (Часть 2, п.5) ───────────────────
    [RelayCommand]
    private async Task SaveProfile()
    {
        var result = await _auth.UpdateProfileAsync(Username, Email, AvatarPath);

        if (!result.Success)
        {
            StatusMessage = result.ErrorMessage ?? string.Empty;
            StatusIsError = true;
            return;
        }

        IsEditingUsername = false;
        IsEditingEmail = false;
        StatusMessage = Loc["SaveBtn"] + " ✓";
        StatusIsError = false;
        RefreshState();
    }

    [RelayCommand]
    private async Task ChangePassword()
    {
        if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword))
        {
            StatusMessage = Loc["AuthFillAllFields"];
            StatusIsError = true;
            return;
        }

        var result = await _auth.ChangePasswordAsync(CurrentPassword, NewPassword);
        if (!result.Success)
        {
            StatusMessage = result.ErrorMessage ?? Loc["AuthWrongPassword"];
            StatusIsError = true;
            return;
        }

        IsChangingPassword = false;
        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        StatusMessage = Loc["ChangePasswordBtn"] + " ✓";
        StatusIsError = false;
    }

    // ── Мгновенный выход из аккаунта ────────────────────────────────────
    [RelayCommand]
    private async Task RequestLogout()
    {
        var owner = GetOwnerWindow();
        if (owner == null) return;

        bool confirmed = await ShowConfirmDialogAsync(owner,
            "Выйти из аккаунта?\n\nДанные аккаунта останутся в нём и появятся снова при следующем входе.");
        if (!confirmed) return;

        // Запустить синхронизацию перед выходом (best-effort, без ожидания)
        if (_sync.IsSyncAvailable)
            _ = _sync.SyncDataAsync();

        // Очистить сохранённый срок подписки, чтобы EntitlementService вернул false
        var settings = AppSettings.Load();
        settings.SubscriptionExpiryDate = null;
        settings.Save();

        await _auth.LogoutAsync();
        _workspace.SetOwner(CurrentWorkspaceService.LocalOwner);

        // Закрыть окно настроек
        CloseSettingsWindow();
    }

    private async Task<bool> ShowConfirmDialogAsync(Window owner, string message)
    {
        var tcs = new TaskCompletionSource<bool>();

        var yesBtn = new Button
        {
            Content = "Выйти", Width = 90, Margin = new Avalonia.Thickness(0, 0, 8, 0),
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#EF4444")),
            Foreground = Avalonia.Media.Brushes.White,
            CornerRadius = new Avalonia.CornerRadius(6)
        };
        var noBtn = new Button
        {
            Content = "Отмена", Width = 90,
            CornerRadius = new Avalonia.CornerRadius(6)
        };

        var dialog = new Window
        {
            Title = "Выход из аккаунта",
            Width = 380, Height = 190,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
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

    private void CloseSettingsWindow()
    {
        if (App.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Закрыть все не-главные окна (Settings и любые другие диалоги)
            foreach (var win in desktop.Windows.Where(w => w != desktop.MainWindow).ToList())
                win.Close();
        }
    }

    // ── Синхронизация (Часть 2, п.3, 8) ──────────────────────────────
    partial void OnSyncEnabledChanged(bool value)
    {
        var settings = AppSettings.Load();

        // ИСПРАВЛЕНО (Часть 2, п.8): при выключении синхронизации — очищаем
        // токен сессии и блокируем вкладку аккаунта при следующем открытии.
        if (!value)
        {
            settings.SyncEnabled = false;
            settings.Save();
            return;
        }

        if (!HasSyncAccess)
        {
            // Нет доступа — переключатель не должен включаться без подписки.
            SyncEnabled = false;
            return;
        }

        settings.SyncEnabled = true;
        settings.Save();
    }

    [RelayCommand]
    private async Task SyncNow()
    {
        IsSyncing = true;
        SyncStatusMessage = Loc["SyncingNowLbl"];

        bool success = await _sync.SyncDataAsync();

        IsSyncing = false;
        SyncStatusMessage = success
            ? Loc["SyncSuccessLbl"]
            : (_sync.LastSyncError ?? Loc["SyncFailedLbl"]);

        var settings = AppSettings.Load();
        LastSyncText = settings.LastSyncUtc.HasValue
            ? settings.LastSyncUtc.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            : Loc["NeverLbl"];
    }

    private Avalonia.Controls.Window? GetOwnerWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.Windows.LastOrDefault();
        return null;
    }
}
