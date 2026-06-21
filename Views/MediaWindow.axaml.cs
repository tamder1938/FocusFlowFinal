using Avalonia.Controls;
using Avalonia.Interactivity;
using FocusFlowFinal.Models.Media;
using FocusFlowFinal.ViewModels;

namespace FocusFlowFinal.Views;

public partial class MediaWindow : Window
{
    public MediaWindow()
    {
        InitializeComponent();
    }

    private MediaViewModel? Vm => DataContext as MediaViewModel;

    private void ItemCard_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border { DataContext: MediaItem item } && Vm != null)
            Vm.SelectedItem = item;
    }

    private void StatusBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var vm = Vm?.SelectedItemVm;
        if (vm == null) return;

        vm.Status = (string?)btn.Tag switch
        {
            "Planned"    => MediaStatus.Planned,
            "InProgress" => MediaStatus.InProgress,
            "Completed"  => MediaStatus.Completed,
            "Dropped"    => MediaStatus.Dropped,
            _            => vm.Status
        };
    }

    private void OverallScore_Tapped(object? sender, RoutedEventArgs e)
    {
        var vm = Vm?.SelectedItemVm;
        if (vm == null) return;
        vm.IsEditingOverall = true;
        vm.IsOverallManual  = true;
    }
}
