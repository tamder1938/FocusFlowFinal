using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Views;
using System.Collections.ObjectModel;

namespace FocusFlowFinal.ViewModels;

public partial class TasksViewModel : ObservableObject
{
    [ObservableProperty]
    private TaskItem? selectedTask;

    public ObservableCollection<TaskItem> Tasks { get; set; } = new();

    [RelayCommand]
    private void EditTask(TaskItem? task)
    {
        if (task == null)
            return;

        var dialog = new TaskDialog();

        dialog.DataContext = new TaskDialogViewModel(task);

        dialog.Show();
    }
}