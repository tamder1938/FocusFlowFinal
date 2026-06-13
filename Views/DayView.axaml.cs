using Avalonia.Controls;
using Avalonia.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.ViewModels;

namespace FocusFlowFinal.Views;

public partial class DayView : UserControl
{
    public DayView()
    {
        InitializeComponent();

        // Уведомляем ViewModel об актуальной ширине области событий,
        // чтобы EventLayoutCalculator правильно делил пространство между
        // пересекающимися событиями.
        EventsAreaGrid.SizeChanged += (_, args) =>
        {
            if (DataContext is DayViewModel vm && args.NewSize.Width > 0)
                vm.UpdateCanvasWidth(args.NewSize.Width);
        };
    }

    private async void EventTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border
            && border.DataContext is EventDisplayItem displayItem
            && displayItem.OriginalEvent != null
            && DataContext is DayViewModel vm)
        {
            await vm.EditEvent(displayItem.OriginalEvent);
        }
    }
}
