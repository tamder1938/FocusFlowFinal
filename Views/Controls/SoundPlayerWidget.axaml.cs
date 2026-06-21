using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using FocusFlowFinal.ViewModels;

namespace FocusFlowFinal.Views.Controls;

public partial class SoundPlayerWidget : UserControl
{
    public SoundPlayerWidget()
    {
        InitializeComponent();
    }

    private SoundViewModel? Vm => DataContext as SoundViewModel;

    private void ChipVolumeSlider_Changed(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is Slider { DataContext: SoundChipItem chip })
            Vm?.SetChipVolumeCommand.Execute(chip);
    }

    private void ManageBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (Vm == null) return;
        var popup = new SoundManagePopup(Vm);
        if (VisualRoot is Window owner)
            popup.ShowDialog(owner);
        else
            popup.Show();
    }
}
