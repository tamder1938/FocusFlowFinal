using Avalonia.Controls;
using Avalonia.Interactivity;
using FocusFlowFinal.ViewModels;

namespace FocusFlowFinal.Views;

public partial class AddExerciseDialog : Window
{
    public AddExerciseDialog()
    {
        InitializeComponent();
    }

    private AddExerciseViewModel? Vm => DataContext as AddExerciseViewModel;

    private void TypeBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var vm = Vm;
        if (vm == null) return;

        if (int.TryParse(btn.Tag?.ToString(), out var idx))
            vm.TypeIndex = idx;

        // Обновляем стили кнопок
        if (TypeBtnPanel != null)
        {
            foreach (var child in TypeBtnPanel.Children)
            {
                if (child is Button b)
                {
                    b.Classes.Remove("type-btn-active");
                    if (b == btn) b.Classes.Add("type-btn-active");
                }
            }
        }
    }

    private void PrimaryMuscleChip_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border { DataContext: MuscleGroupItem item })
            item.IsSelected = !item.IsSelected;
    }

    private void SecMuscleChip_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border { DataContext: MuscleGroupItem item })
            item.IsSelected = !item.IsSelected;
    }

    private void EquipmentChip_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border { DataContext: EquipmentItem item })
            item.IsSelected = !item.IsSelected;
    }
}
