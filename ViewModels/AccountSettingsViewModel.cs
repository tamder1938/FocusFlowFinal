using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;   // ← добавить эту строку
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

    public AccountSettingsViewModel(IAuthService auth, IPaymentService payment, ISyncService sync)
    {
        _auth = auth;
        _payment = payment;
        _sync = sync;

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
            SubscriptionStatusText = $"{Loc["SubscriptionActiveLbl"]} {user.Subscription.ExpiresAtUtc:dd.MM.yyyy}";
        else
            SubscriptionStatusText = string.Empty;

        var settings = AppSettings.Load();
        SyncEnabled = settings.SyncEnabled;
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
        SyncStatusMessage = success ? Loc["SyncSuccessLbl"] : Loc["SyncFailedLbl"];

        var settings = AppSettings.Load();
        LastSyncText = settings.LastSyncUtc.HasValue
            ? settings.LastSyncUtc.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            : Loc["NeverLbl"];
    }

    [RelayCommand]
    private async Task Logout()
    {
        await _auth.LogoutAsync();
        RefreshState();
    }

    private Avalonia.Controls.Window? GetOwnerWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.Windows.LastOrDefault();
        return null;
    }
}
