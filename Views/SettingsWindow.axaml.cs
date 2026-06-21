using System;
using Avalonia.Controls;
using Avalonia.Input;
using FocusFlowFinal.ViewModels;

namespace FocusFlowFinal.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        (DataContext as IDisposable)?.Dispose();
    }

    /// <summary>
    /// ИСПРАВЛЕНО (Проблема 1): клик по карточке темы вызывает SetThemeCommand,
    /// который только обновляет CurrentThemeMode в памяти ViewModel —
    /// сама тема приложения НЕ меняется до нажатия «Сохранить».
    /// </summary>
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
}
