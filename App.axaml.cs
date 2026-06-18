using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using FocusFlowFinal.ViewModels;
using FocusFlowFinal.Views;
using FocusFlowFinal.Models.Finance;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FocusFlowFinal;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var settings = AppSettings.Load();
        ApplyTheme(settings.ThemeMode);
        LocalizationService.Instance.CurrentLanguage = settings.Language;

        var svc = new ServiceCollection();
        ConfigureServices(svc);
        Services = svc.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // ИСПРАВЛЕНО (Часть 2, п.8): восстанавливаем сессию (если токен сохранён)
            var authService = Services.GetRequiredService<IAuthService>();
            _ = authService.RestoreSessionAsync(); // synchronous-enough для локальной заглушки

            var win = new MainWindow { DataContext = Services.GetRequiredService<MainViewModel>() };
            desktop.MainWindow = win;

            win.Opened += (_, _) =>
            {
                var ns = Services.GetRequiredService<INotificationService>() as NotificationService;
                if (ns != null) { ns.Initialize(Avalonia.Controls.TopLevel.GetTopLevel(win)!); ns.StartPolling(); }

                // ИСПРАВЛЕНО (Часть 2, п.8): при первом запуске показываем экран входа/регистрации.
                // "Продолжить без синхронизации" работает как и раньше — все функции доступны локально.
                if (!settings.HasCompletedFirstRun)
                {
                    var loginVm = new LoginViewModel(authService);
                    var loginWindow = new LoginWindow { DataContext = loginVm };
                    _ = loginWindow.ShowDialog(win);
                }

                // ИСПРАВЛЕНО (Часть 2, п.3): первая синхронизация при запуске (если доступна)
                var syncService = Services.GetRequiredService<ISyncService>();
                if (syncService.IsSyncAvailable)
                    _ = syncService.SyncDataAsync();
            };

            var tv = Services.GetRequiredService<TimerViewModel>();
            var nr = Services.GetRequiredService<INotificationService>() as NotificationService;
            if (nr != null) tv.TimerFinished += t => nr.NotifyTimerFinished(t ?? "");

            desktop.Exit += (_, _) =>
                (Services.GetRequiredService<INotificationService>() as NotificationService)?.StopPolling();
        }
        base.OnFrameworkInitializationCompleted();
    }

    public void ApplyTheme(int mode)
    {
        var v = mode switch { 0 => ThemeVariant.Light, 1 => ThemeVariant.Dark, _ => ThemeVariant.Default };
        RequestedThemeVariant = v;
        var r = Resources; if (r == null) return;
        bool d = v == ThemeVariant.Dark;

        // ── Фоны окон ──────────────────────────────────────────────
        Set(r, "WindowBackgroundBrush",        d ? "#0D0F1A" : "#F0F4FA");
        Set(r, "CardBackgroundBrush",          d ? "#1A1D2E" : "#FFFFFF");
        Set(r, "CardSecondaryBackgroundBrush", d ? "#20243A" : "#F8FAFF");
        Set(r, "InputBackgroundBrush",         d ? "#222638" : "#F9FAFB");

        // ── Текст ──────────────────────────────────────────────────
        Set(r, "PrimaryTextBrush",   d ? "#F1F3FF" : "#111827");
        Set(r, "SecondaryTextBrush", d ? "#8B90B8" : "#6B7280");

        // ── Границы ────────────────────────────────────────────────
        Set(r, "BorderBrush",        d ? "#252840" : "#E5E7EB");

        // ── Кнопки ─────────────────────────────────────────────────
        Set(r, "ButtonBackgroundBrush",  d ? "#222638" : "#FFFFFF");
        Set(r, "ButtonForegroundBrush",  d ? "#C8CCE8" : "#374151");
        Set(r, "ButtonBorderBrush",      d ? "#252840" : "#E5E7EB");

        // ── Акцент ─────────────────────────────────────────────────
        Set(r, "AccentBackground",  "#3B82F6");
        Set(r, "AccentHoverBrush",  "#2563EB");
        Set(r, "AccentLightBrush",  d ? "#1E3A5F" : "#EFF6FF");
        Set(r, "AccentTextBrush",   d ? "#93C5FD" : "#2563EB");

        // ── Навигация ──────────────────────────────────────────────
        Set(r, "NavActiveBg",       "#3B82F6");
        Set(r, "NavActiveFg",       "#FFFFFF");
        Set(r, "NavInactiveFg",     d ? "#8B90B8" : "#6B7280");
        Set(r, "NavInactiveBg",     "Transparent");
        Set(r, "NavContainerBg",    d ? "#13162A" : "#EEF2FB");

        // ── Прогресс ───────────────────────────────────────────────
        Set(r, "ProgressBackgroundBrush", d ? "#252840" : "#E5E7EB");

        // ── Settings ───────────────────────────────────────────────
        Set(r, "SettingsWindowBackground",      d ? "#0D0F1A" : "#F3F4F6");
        Set(r, "SidePanelBackground",           d ? "#080A14" : "#EAECF5");
        Set(r, "CardBackground",                d ? "#1A1D2E" : "#FFFFFF");
        Set(r, "CardBorder",                    d ? "#252840" : "#E5E7EB");
        Set(r, "HeaderForeground",              d ? "#F1F3FF" : "#1E3A8A");
        Set(r, "PrimaryText",                   d ? "#F1F3FF" : "#111827");
        Set(r, "SecondaryText",                 d ? "#8B90B8" : "#6B7280");
        Set(r, "SideNavButtonBackground",       "Transparent");
        Set(r, "SideNavButtonForeground",       d ? "#C8CCE8" : "#374151");
        Set(r, "SideNavButtonHoverBackground",  d ? "#252840" : "#E5E7EB");
        Set(r, "SideNavButtonActiveBackground", "#3B82F6");
        Set(r, "HotkeyBadgeBackground",         d ? "#222638" : "#F9FAFB");
        Set(r, "HotkeyBadgeBorder",             d ? "#2D3050" : "#D1D5DB");
        Set(r, "HotkeyText",                    d ? "#93C5FD" : "#2563EB");
        Set(r, "DangerCardBackground",          d ? "#1F1015" : "#FFF1F2");
        Set(r, "DangerCardBorder",              d ? "#4A1A1A" : "#FECDD3");
        Set(r, "DangerText",                    d ? "#FCA5A5" : "#BE123C");
        Set(r, "DangerButtonBackground",        "#EF4444");

        // ── Primary / Secondary action buttons (диалоги) ────────────
        Set(r, "PrimaryActionBrush",      "#4F6EF5");
        Set(r, "PrimaryActionHoverBrush", "#3B5BDB");
        Set(r, "SecondaryActionBrush",     d ? "#222638" : "#E9EDF5");
        Set(r, "SecondaryActionFgBrush",   d ? "#C8CCE8" : "#475569");

        // ── Цвета приоритета (для pill-кнопок) ───────────────────────
        Set(r, "PriorityHighBg",    d ? "#3A1F22" : "#FEE2E2");
        Set(r, "PriorityHighFg",    "#EF4444");
        Set(r, "PriorityMediumBg",  d ? "#3A2A14" : "#FFF1E0");
        Set(r, "PriorityMediumFg",  "#F97316");
        Set(r, "PriorityLowBg",     d ? "#173A2A" : "#D1FAE5");
        Set(r, "PriorityLowFg",     "#10B981");
    }

    private static void Set(IResourceDictionary r, string key, string hex) =>
        r[key] = new SolidColorBrush(Color.Parse(hex));

    private void ConfigureServices(IServiceCollection s)
    {
        s.AddSingleton<IDatabaseService, DatabaseService>();
        s.AddSingleton<ITemplateService, TemplateService>();
        s.AddSingleton<INotificationService, NotificationService>();
        s.AddTransient<MainViewModel>();
        s.AddTransient<DayViewModel>();
        s.AddTransient<WeekViewModel>();
        s.AddTransient<MonthViewModel>();
        s.AddTransient<YearViewModel>();
        s.AddSingleton<TaskListViewModel>();
        s.AddSingleton<TimerViewModel>();
        s.AddTransient<SettingsViewModel>();

        // ИСПРАВЛЕНО (Часть 2-3): аккаунт, подписка, облачная синхронизация
        s.AddSingleton<IAuthService, AuthService>();
        s.AddSingleton<IPaymentService, PaymentServiceStub>();
        s.AddSingleton<ICloudDatabaseService, CloudDatabaseService>();
        s.AddSingleton<ISyncService, CloudSyncService>();

        // Финансовый модуль
        s.AddTransient<FinanceViewModel>();
    }
}
