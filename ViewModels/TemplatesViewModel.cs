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

    [ObservableProperty]
    private ObservableCollection<TaskTemplate> _taskTemplates = new();

    [ObservableProperty]
    private ObservableCollection<EventTemplate> _eventTemplates = new();

    public TemplatesViewModel(ITemplateService templateService)
    {
        _templateService = templateService;
        LoadAllTemplates();
    }

    private void LoadAllTemplates()
    {
        LoadTaskTemplates();
        LoadEventTemplates();
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

    [RelayCommand]
    private void DeleteTaskTemplate(TaskTemplate template)
    {
        if (template == null) return;
        _templateService.DeleteTaskTemplate(template.Id);
        LoadTaskTemplates();
    }

    [RelayCommand]
    private void DeleteEventTemplate(EventTemplate template)
    {
        if (template == null) return;
        _templateService.DeleteEventTemplate(template.Id);
        LoadEventTemplates();
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
            Priority = template.Priority ?? 1, // приоритет по умолчанию: средний (1)
            ProjectId = template.ProjectId,
            DueDate = template.HasDate ? System.DateTime.Today : null,
            StartTime = template.IsTimeBound ? new System.TimeSpan(template.StartHour, template.StartMinute, 0) : null
        };
        var dialogVm = new TaskDialogViewModel(task);
        var dialog = new TaskDialog { DataContext = dialogVm };
        var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var owner = desktop?.MainWindow;
        if (owner == null)
            dialog.Show();
        else
            await dialog.ShowDialog<bool?>(owner);
        LoadTaskTemplates();
    }

    [RelayCommand]
    private async Task EditTaskTemplate(TaskTemplate template)
    {
        if (template == null) return;
        var dialogVm = new TaskTemplateDialogViewModel(template);
        var dialog = new TaskTemplateDialog { DataContext = dialogVm };
        var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var owner = desktop?.MainWindow;
        if (owner == null)
            dialog.Show();
        else
            await dialog.ShowDialog<bool?>(owner);
        LoadTaskTemplates();
    }
}