using Avalonia.Controls;
using FocusFlowFinal.ViewModels;

namespace FocusFlowFinal.Views;

public partial class SoundManagePopup : Window
{
    public SoundManagePopup()
    {
        InitializeComponent();
    }

    public SoundManagePopup(SoundViewModel vm) : this()
    {
        DataContext = vm;
    }
}
