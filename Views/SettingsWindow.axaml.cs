using System;
using Avalonia.Controls;
using Avalonia.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.ViewModels;

namespace FocusFlowFinal.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        (DataContext as SettingsViewModel)?.MarkInitialized();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        (DataContext as IDisposable)?.Dispose();
    }

    private void ThemeCard_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border &&
            border.Tag is string tag &&
            DataContext is SettingsViewModel vm)
        {
            if (vm.SetThemeCommand.CanExecute(tag))
                vm.SetThemeCommand.Execute(tag);
        }
    }

    private void ThemeTile_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border &&
            border.Tag is string tag &&
            DataContext is SettingsViewModel vm &&
            Enum.TryParse<AppTheme>(tag, out var theme))
        {
            vm.SelectedTheme = theme;
        }
    }
}
