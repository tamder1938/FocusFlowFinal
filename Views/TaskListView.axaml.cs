using Avalonia.Controls;
using Avalonia.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.ViewModels;

namespace FocusFlowFinal.Views;

public partial class TaskListView : UserControl
{
    private TaskItem? _lastClickedTask;

    public TaskListView()
    {
        InitializeComponent();

        var listBox = this.FindControl<ListBox>("TasksListBox");

        if (listBox != null)
        {
            listBox.AddHandler(
                InputElement.TappedEvent,
                OnListBoxTapped,
                handledEventsToo: true);
        }
    }

    private void OnListBoxTapped(object? sender, TappedEventArgs e)
    {
        var listBox = this.FindControl<ListBox>("TasksListBox");

        if (listBox == null)
            return;

        if (e.Source is not Control control)
            return;

        if (control.DataContext is not TaskItem task)
            return;

        if (_lastClickedTask == task && listBox.SelectedItem == task)
        {
            listBox.SelectedItem = null;

            if (DataContext is TaskListViewModel vm)
                vm.SelectedTask = null;

            _lastClickedTask = null;
            return;
        }

        _lastClickedTask = task;
    }
}