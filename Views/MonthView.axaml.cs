using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml; // ДОБАВЬТЕ ЭТОТ USING
using FocusFlowFinal.ViewModels;
using FocusFlowFinal.Models;

namespace FocusFlowFinal.Views;

public partial class MonthView : UserControl
{
    public MonthView()
    {
        InitializeComponent();
    }

    // ДОБАВЬТЕ ЭТОТ МЕТОД: он вручную подменит автогенерацию на время сборки
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void DayTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is MonthDayItem item)
        {
            if (DataContext is MonthViewModel vm)
            {
                vm.SelectDayCommand.Execute(item);
            }
        }
    }
}
