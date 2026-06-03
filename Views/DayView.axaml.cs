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
    }

    private async void EventTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is EventDisplayItem displayItem && displayItem.OriginalEvent != null)
        {
            if (DataContext is DayViewModel vm)
            {
                // Вызываем существующий метод EditEvent, который сам откроет диалог и обработает удаление/сохранение
                await vm.EditEvent(displayItem.OriginalEvent);
            }
        }
    }
}