using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FocusFlowFinal.Views;

public partial class WeekView : UserControl
{
    public WeekView()
    {
        InitializeComponent();
    }

    // Ручной метод инициализации на случай сбоя автогенератора Avalonia
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
