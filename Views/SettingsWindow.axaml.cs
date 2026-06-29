using System;
using System.Collections.Generic;
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

    private void OnHotkeyKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb || DataContext is not SettingsViewModel vm)
            return;
        var gesture = BuildGestureString(e);
        if (gesture == null) return;

        var action = tb.Tag as string;
        switch (action)
        {
            case "Day":     vm.EditHotkeyDay     = gesture; break;
            case "Week":    vm.EditHotkeyWeek    = gesture; break;
            case "Month":   vm.EditHotkeyMonth   = gesture; break;
            case "Year":    vm.EditHotkeyYear    = gesture; break;
            case "NewTask": vm.EditHotkeyNewTask = gesture; break;
            case "Today":   vm.EditHotkeyToday   = gesture; break;
        }
        e.Handled = true;
    }

    private static string? BuildGestureString(KeyEventArgs e)
    {
        if (e.Key is Key.LeftCtrl  or Key.RightCtrl  or
                     Key.LeftAlt   or Key.RightAlt   or
                     Key.LeftShift or Key.RightShift  or
                     Key.LWin      or Key.RWin        or
                     Key.None      or Key.Tab         or
                     Key.Escape    or Key.Back)
            return null;

        var parts = new List<string>();
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))     parts.Add("Alt");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))   parts.Add("Shift");
        parts.Add(e.Key.ToString());
        return string.Join("+", parts);
    }
}
