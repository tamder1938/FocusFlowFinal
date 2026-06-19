using Avalonia.Controls;
using Avalonia.Input;
using FocusFlowFinal.ViewModels;

namespace FocusFlowFinal.Views;

public partial class HabitWindow : Window
{
    public HabitWindow()
    {
        InitializeComponent();
    }

    private void HabitCard_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border
            && border.DataContext is HabitDisplayItem item
            && DataContext is HabitViewModel vm)
        {
            vm.SelectHabitCommand.Execute(item);
        }
    }
}
