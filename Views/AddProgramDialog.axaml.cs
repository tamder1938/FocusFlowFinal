using Avalonia.Controls;
using Avalonia.Interactivity;
using FocusFlowFinal.ViewModels;

namespace FocusFlowFinal.Views;

public partial class AddProgramDialog : Window
{
    public AddProgramDialog()
    {
        InitializeComponent();
    }

    private AddProgramViewModel? Vm => DataContext as AddProgramViewModel;

    private void ColorSwatch_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border b && Vm != null)
            Vm.Color = b.Tag?.ToString() ?? "#3B82F6";
    }
}
