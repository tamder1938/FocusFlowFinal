using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FocusFlowFinal.Views;

public partial class TimerView : UserControl
{
    public TimerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
