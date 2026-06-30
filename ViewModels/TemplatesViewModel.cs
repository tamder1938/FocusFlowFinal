using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class TemplatesViewModel : ObservableObject
{
    private readonly ITemplateService _templateService;
    private readonly IDatabaseService _db;

    [ObservableProperty]
    private ObservableCollection<TaskTemplate> _taskTemplates = new();

    [ObservableProperty]
    private ObservableCollection<EventTemplate> _eventTemplates = new();

    [ObservableProperty]
    private ObservableCollection<TimerTemplate> _timerTemplates = new();

    public TemplatesViewModel(ITemplateService templateService, IDatabaseService db)
    {
        _templateService = templateService;
        _db = db;
        LoadAllTemplates();
    }

    private void LoadAllTemplates()
    {
        LoadTaskTemplates();
        LoadEventTemplates();
        LoadTimerTemplates();
    }

    private void LoadTimerTemplates()
    {
        TimerTemplates.Clear();
        foreach (var t in _db.GetAllTimerTemplates())
            TimerTemplates.Add(t);
    }

    private void LoadTaskTemplates()
    {
        TaskTemplates.Clear();
        var templates = _templateService.GetAllTaskTemplates();
        foreach (var t in templates)
            TaskTemplates.Add(t);
    }

    private void LoadEventTemplates()
    {
        EventTemplates.Clear();
        var templates = _templateService.GetEventTemplates();
        foreach (var t in templates)
            EventTemplates.Add(t);
    }

    // ── Task templates ────────────────────────────────────────────────

    [RelayCommand]
    private async Task CreateTaskTemplate()
    {
        var template = new TaskTemplate();
        var dialogVm = new TaskTemplateDialogViewModel(template);
        var dialog = new TaskTemplateDialog { DataContext = dialogVm };
        await ShowDialogAsync(dialog);
        LoadTaskTemplates();
    }

    [RelayCommand]
    private async Task EditTaskTemplate(TaskTemplate template)
    {
        if (template == null) return;
        var dialogVm = new TaskTemplateDialogViewModel(template);
        var dialog = new TaskTemplateDialog { DataContext = dialogVm };
        await ShowDialogAsync(dialog);
        LoadTaskTemplates();
    }

    [RelayCommand]
    private async Task DeleteTaskTemplate(TaskTemplate template)
    {
        if (template == null) return;
        if (!await ConfirmDeleteAsync()) return;
        _templateService.DeleteTaskTemplate(template.Id);
        LoadTaskTemplates();
    }

    [RelayCommand]
    private async Task CreateTaskFromTemplate(TaskTemplate template)
    {
        if (template == null) return;
        var task = new TaskItem
        {
            Title = template.Title,
            Description = template.Description,
            PlannedDurationMinutes = template.PlannedDurationMinutes,
            Priority = template.Priority ?? 1,
            ProjectId = template.ProjectId,
            DueDate = template.HasDate ? System.DateTime.Today : null,
            StartTime = template.IsTimeBound ? new System.TimeSpan(template.StartHour, template.StartMinute, 0) : null
        };
        var dialogVm = new TaskDialogViewModel(task);
        var dialog = new TaskDialog { DataContext = dialogVm };
        var saved = await ShowDialogAsync(dialog);

        if (saved)
        {
            // TaskDialogViewModel.Save() fills `task` but does not persist it — we do that here
            _db.UpsertTask(task);

            // Refresh the task list visible in the main window
            if (Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow?.DataContext is MainViewModel mainVm)
                    mainVm.CurrentTaskListViewModel?.RefreshTasks();
            }
        }
    }

    // ── Event templates ───────────────────────────────────────────────

    [RelayCommand]
    private async Task CreateEventTemplate()
    {
        var template = new EventTemplate();
        var dialogVm = new EventTemplateDialogViewModel(template);
        var dialog = new EventTemplateDialog { DataContext = dialogVm };
        await ShowDialogAsync(dialog);
        LoadEventTemplates();
    }

    [RelayCommand]
    private async Task EditEventTemplate(EventTemplate template)
    {
        if (template == null) return;
        var dialogVm = new EventTemplateDialogViewModel(template);
        var dialog = new EventTemplateDialog { DataContext = dialogVm };
        await ShowDialogAsync(dialog);
        LoadEventTemplates();
    }

    [RelayCommand]
    private async Task DeleteEventTemplate(EventTemplate template)
    {
        if (template == null) return;
        if (!await ConfirmDeleteAsync()) return;
        _templateService.DeleteEventTemplate(template.Id);
        LoadEventTemplates();
    }

    // ── Timer templates ───────────────────────────────────────────────

    [RelayCommand]
    private async Task CreateTimerTemplate()
    {
        var template = new TimerTemplate { Name = string.Empty };
        var dialogVm = new TimerTemplateDialogViewModel(template);
        var dialog = new TimerTemplateDialog { DataContext = dialogVm };
        await ShowDialogAsync(dialog);
        LoadTimerTemplates();
    }

    [RelayCommand]
    private async Task EditTimerTemplate(TimerTemplate? template)
    {
        if (template == null) return;
        var dialogVm = new TimerTemplateDialogViewModel(template);
        var dialog = new TimerTemplateDialog { DataContext = dialogVm };
        await ShowDialogAsync(dialog);
        LoadTimerTemplates();
    }

    [RelayCommand]
    private async Task DeleteTimerTemplate(TimerTemplate? template)
    {
        if (template == null) return;
        if (!await ConfirmDeleteAsync()) return;
        _db.DeleteTimerTemplate(template.Id);
        LoadTimerTemplates();
    }

    [RelayCommand]
    private void RestoreDefaultTimerTemplates()
    {
        _db.SeedDefaultTimerTemplates();
        LoadTimerTemplates();
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static Window? GetOwnerWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    private static async Task<bool> ShowDialogAsync(Window dialog)
    {
        var owner = GetOwnerWindow();
        if (owner != null)
            return await dialog.ShowDialog<bool>(owner);
        dialog.Show();
        return false;
    }

    private static async Task<bool> ConfirmDeleteAsync()
    {
        var owner = GetOwnerWindow();
        if (owner == null) return true;

        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        var dialog = new Window
        {
            Width = 320, Height = 140,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Avalonia.Media.Brushes.White,
            Title = "Подтверждение"
        };
        var panel = new Avalonia.Controls.StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 16 };
        panel.Children.Add(new TextBlock { Text = "Удалить шаблон?", FontWeight = Avalonia.Media.FontWeight.SemiBold, FontSize = 14 });
        var btns = new Avalonia.Controls.StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        var cancelBtn = new Button { Content = "Отмена" };
        var okBtn = new Button { Content = "Удалить", Background = Avalonia.Media.Brushes.OrangeRed, Foreground = Avalonia.Media.Brushes.White };
        cancelBtn.Click += (_, _) => { tcs.TrySetResult(false); dialog.Close(); };
        okBtn.Click     += (_, _) => { tcs.TrySetResult(true);  dialog.Close(); };
        btns.Children.Add(cancelBtn);
        btns.Children.Add(okBtn);
        panel.Children.Add(btns);
        dialog.Content = panel;
        _ = dialog.ShowDialog(owner);
        return await tcs.Task;
    }
}
