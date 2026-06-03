using Avalonia.Controls;
using Avalonia.Styling;
using FocusFlowFinal.ViewModels;
using System.Linq;

namespace FocusFlowFinal.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        UpdateThemeButtonHighlight();
    }

    private void UpdateThemeButtonHighlight()
    {
        if (DataContext is not SettingsViewModel vm) return;

        // Сброс стилей всех кнопок
        LightThemeButton.Classes.Remove("active");
        DarkThemeButton.Classes.Remove("active");
        AutoThemeButton.Classes.Remove("active");

        // Подсветка активной кнопки
        switch (vm.CurrentThemeMode)
        {
            case 0:
                LightThemeButton.Classes.Add("active");
                break;
            case 1:
                DarkThemeButton.Classes.Add("active");
                break;
            case 2:
                AutoThemeButton.Classes.Add("active");
                break;
        }
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is SettingsViewModel vm)
        {
            // Подписываемся на изменение темы, чтобы обновлять подсветку
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SettingsViewModel.CurrentThemeMode))
                    UpdateThemeButtonHighlight();
            };
            UpdateThemeButtonHighlight();
        }
    }
}