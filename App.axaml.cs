using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using FocusFlowFinal.ViewModels;
using FocusFlowFinal.Views;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FocusFlowFinal;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var settings = AppSettings.Load();
        ApplyTheme(settings.ThemeMode);
        LocalizationService.Instance.CurrentLanguage = settings.Language;

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            mainWindow.DataContext = Services.GetRequiredService<MainViewModel>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void ApplyTheme(int themeMode)
    {
        var variant = themeMode switch
        {
            0 => ThemeVariant.Light,
            1 => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
        RequestedThemeVariant = variant;

        var resources = Application.Current?.Resources;
        if (resources == null) return;

        if (variant == ThemeVariant.Dark)
        {
            resources["SettingsWindowBackground"] = new SolidColorBrush(Color.Parse("#1E1E1E"));
            resources["SidePanelBackground"] = new SolidColorBrush(Color.Parse("#151515"));
            resources["CardBackground"] = new SolidColorBrush(Color.Parse("#2D2D30"));
            resources["CardBorder"] = new SolidColorBrush(Color.Parse("#3E3E42"));
            resources["HeaderForeground"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
            resources["PrimaryText"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
            resources["SecondaryText"] = new SolidColorBrush(Color.Parse("#AAAAAA"));
            resources["ButtonBackground"] = new SolidColorBrush(Color.Parse("#2D2D30"));
            resources["ButtonForeground"] = new SolidColorBrush(Color.Parse("#E0E0E0"));
            resources["AccentBackground"] = new SolidColorBrush(Color.Parse("#3B82F6"));
            resources["SideNavButtonBackground"] = new SolidColorBrush(Colors.Transparent);
            resources["SideNavButtonForeground"] = new SolidColorBrush(Color.Parse("#D1D5DB"));
            resources["SideNavButtonHoverBackground"] = new SolidColorBrush(Color.Parse("#3E3E42"));
            resources["SideNavButtonActiveBackground"] = new SolidColorBrush(Color.Parse("#3B82F6"));
            resources["HotkeyBadgeBackground"] = new SolidColorBrush(Color.Parse("#3E3E42"));
            resources["HotkeyBadgeBorder"] = new SolidColorBrush(Color.Parse("#555555"));
            resources["HotkeyText"] = new SolidColorBrush(Color.Parse("#90CAF9"));
            resources["ComboBoxBackground"] = new SolidColorBrush(Color.Parse("#2D2D30"));
            resources["DangerCardBackground"] = new SolidColorBrush(Color.Parse("#2D1A1A"));
            resources["DangerCardBorder"] = new SolidColorBrush(Color.Parse("#E53E3E"));
            resources["DangerText"] = new SolidColorBrush(Color.Parse("#FCA5A5"));
            resources["DangerButtonBackground"] = new SolidColorBrush(Color.Parse("#E53E3E"));
        }
        else
        {
            resources["SettingsWindowBackground"] = new SolidColorBrush(Color.Parse("#F3F4F6"));
            resources["SidePanelBackground"] = new SolidColorBrush(Color.Parse("#EEF2F6"));
            resources["CardBackground"] = new SolidColorBrush(Colors.White);
            resources["CardBorder"] = new SolidColorBrush(Color.Parse("#E5E7EB"));
            resources["HeaderForeground"] = new SolidColorBrush(Color.Parse("#1E3A8A"));
            resources["PrimaryText"] = new SolidColorBrush(Color.Parse("#111827"));
            resources["SecondaryText"] = new SolidColorBrush(Color.Parse("#6B7280"));
            resources["ButtonBackground"] = new SolidColorBrush(Colors.White);
            resources["ButtonForeground"] = new SolidColorBrush(Color.Parse("#374151"));
            resources["AccentBackground"] = new SolidColorBrush(Color.Parse("#3B82F6"));
            resources["SideNavButtonBackground"] = new SolidColorBrush(Colors.Transparent);
            resources["SideNavButtonForeground"] = new SolidColorBrush(Color.Parse("#374151"));
            resources["SideNavButtonHoverBackground"] = new SolidColorBrush(Color.Parse("#E5E7EB"));
            resources["SideNavButtonActiveBackground"] = new SolidColorBrush(Color.Parse("#3B82F6"));
            resources["HotkeyBadgeBackground"] = new SolidColorBrush(Colors.White);
            resources["HotkeyBadgeBorder"] = new SolidColorBrush(Color.Parse("#D1D5DB"));
            resources["HotkeyText"] = new SolidColorBrush(Color.Parse("#2563EB"));
            resources["ComboBoxBackground"] = new SolidColorBrush(Colors.White);
            resources["DangerCardBackground"] = new SolidColorBrush(Color.Parse("#FEF2F2"));
            resources["DangerCardBorder"] = new SolidColorBrush(Color.Parse("#FEE2E2"));
            resources["DangerText"] = new SolidColorBrush(Color.Parse("#991B1B"));
            resources["DangerButtonBackground"] = new SolidColorBrush(Color.Parse("#EF4444"));
            resources["BorderBrush"] = new SolidColorBrush(Color.Parse("#777777")); 
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<ITemplateService, TemplateService>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<DayViewModel>();
        services.AddTransient<WeekViewModel>();
        services.AddTransient<MonthViewModel>();
        services.AddTransient<YearViewModel>();
        services.AddTransient<TaskListViewModel>();
        services.AddTransient<TimerViewModel>();
        services.AddTransient<SettingsViewModel>();
    }
}