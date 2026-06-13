using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public enum TimerCompletionResult { None, Completed, Extended, Deferred, Deleted }

public partial class TimerCompletionViewModel : ObservableObject
{
    public string TaskTitle { get; }
    public TimerCompletionResult Result { get; private set; } = TimerCompletionResult.None;
    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAskingQuestion))]
    private bool _isShowingOptions;

    [ObservableProperty] private string _extendMinutesText = "15";
    [ObservableProperty] private string _extendError = string.Empty;

    // Populated by Extend() after successful validation; read by TimerViewModel
    public int ExtendMinutes { get; private set; } = 15;

    public bool IsAskingQuestion => !IsShowingOptions;

    public TimerCompletionViewModel(TaskItem task)
    {
        TaskTitle = task.Title;
    }

    [RelayCommand]
    private void MarkCompleted()
    {
        Result = TimerCompletionResult.Completed;
        CloseDialog();
    }

    [RelayCommand]
    private void ExpandOptions() => IsShowingOptions = true;

    [RelayCommand]
    private void Extend()
    {
        var text = ExtendMinutesText?.Trim() ?? string.Empty;
        if (int.TryParse(text, out int minutes) && minutes >= 1)
        {
            ExtendMinutes = minutes;
            ExtendError   = string.Empty;
            Result        = TimerCompletionResult.Extended;
            CloseDialog();
        }
        else
        {
            ExtendError = Loc["TimerExtendError"];
        }
    }

    [RelayCommand]
    private void Defer()
    {
        Result = TimerCompletionResult.Deferred;
        CloseDialog();
    }

    [RelayCommand]
    private void DeleteTask()
    {
        Result = TimerCompletionResult.Deleted;
        CloseDialog();
    }

    private void CloseDialog()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Windows.FirstOrDefault(w => w.DataContext == this)?.Close();
    }
}
