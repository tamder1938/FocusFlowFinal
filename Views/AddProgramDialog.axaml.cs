using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using FocusFlowFinal.ViewModels;

namespace FocusFlowFinal.Views;

public partial class AddProgramDialog : Window
{
    public AddProgramDialog()
    {
        InitializeComponent();
        Opened += (_, _) => HighlightSelectedColor(Vm?.Color);
    }

    private AddProgramViewModel? Vm => DataContext as AddProgramViewModel;

    private void ColorSwatch_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border b && Vm != null)
        {
            Vm.Color = b.Tag?.ToString() ?? "#3B82F6";
            HighlightSelectedColor(Vm.Color);
        }
    }

    private void HighlightSelectedColor(string? selected)
    {
        if (ColorPanel == null) return;
        foreach (var child in ColorPanel.Children)
        {
            if (child is not Border swatch) continue;
            bool isSel = swatch.Tag?.ToString() == selected;
            swatch.BorderThickness = new Thickness(isSel ? 3 : 0);
            swatch.BorderBrush     = isSel ? Brushes.White : null;
            swatch.BoxShadow       = isSel
                ? new BoxShadows(BoxShadow.Parse("0 0 0 2 #3B82F6"))
                : default;
        }
    }
}
