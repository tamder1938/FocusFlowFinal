using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public partial class TaskTemplateDialogViewModel : ObservableObject
{
    private readonly TaskTemplate _template;
    private readonly ITemplateService _templateService;
    private readonly IDatabaseService _db;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private int? _priority;
    [ObservableProperty] private int _priorityIndex = -1;
    [ObservableProperty] private ObservableCollection<ProjectItem> _projectsList = new();
    [ObservableProperty] private ProjectItem? _selectedProject;
    [ObservableProperty] private int _plannedDurationMinutes;
    [ObservableProperty] private bool _hasDate;
    [ObservableProperty] private bool _isTimeBound;
    [ObservableProperty] private int _startHour = 9;
    [ObservableProperty] private int _startMinute;

    public TaskTemplateDialogViewModel(TaskTemplate template, ITemplateService? templateService = null)
    {
        _template = template;
        var services = ((App)Avalonia.Application.Current!).Services!;
        _db = services.GetRequiredService<IDatabaseService>();
        _templateService = templateService ?? services.GetRequiredService<ITemplateService>();

        Name = template.Name;
        Title = template.Title;
        Description = template.Description;
        Priority = template.Priority;
        PriorityIndex = template.Priority ?? -1;
        PlannedDurationMinutes = template.PlannedDurationMinutes;
        HasDate = template.HasDate;
        IsTimeBound = template.IsTimeBound;
        StartHour = template.StartHour;
        StartMinute = template.StartMinute;

        LoadProjectsData(template.ProjectId);
    }

    private void LoadProjectsData(int? activeProjectId)
    {
        ProjectsList.Clear();
        ProjectsList.Add(new ProjectItem { Id = 0, Name = "Без проекта", Color = "#9CA3AF" });
        var userProjects = _db.GetAllProjects();
        foreach (var p in userProjects)
            ProjectsList.Add(p);

        SelectedProject = (activeProjectId.HasValue && activeProjectId.Value > 0)
            ? ProjectsList.FirstOrDefault(p => p.Id == activeProjectId.Value)
            : ProjectsList[0];
    }

    [RelayCommand]
    private void SetPriority(string priorityStr)
    {
        if (int.TryParse(priorityStr, out int p))
        {
            if (Priority == p)
            {
                Priority = null;
                PriorityIndex = -1;
            }
            else
            {
                Priority = p;
                PriorityIndex = p;
            }
        }
    }

    [RelayCommand]
    private void ClearPriority()
    {
        Priority = null;
        PriorityIndex = -1;
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return;

        _template.Name = Name.Trim();
        _template.Title = Title.Trim();
        _template.Description = Description.Trim();
        _template.Priority = PriorityIndex >= 0 ? PriorityIndex : null;
        _template.ProjectId = (SelectedProject != null && SelectedProject.Id > 0) ? SelectedProject.Id : null;
        _template.PlannedDurationMinutes = PlannedDurationMinutes;
        _template.HasDate = HasDate;
        _template.IsTimeBound = IsTimeBound;
        _template.StartHour = StartHour;
        _template.StartMinute = StartMinute;

        _templateService.UpsertTaskTemplate(_template);
        CloseDialog(true);
    }

    [RelayCommand]
    private void Cancel() => CloseDialog(false);

    private void CloseDialog(bool result)
    {
        if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }
}