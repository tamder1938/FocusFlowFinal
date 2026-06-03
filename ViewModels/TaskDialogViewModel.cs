using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class TaskDialogViewModel : ObservableObject
{
    private readonly TaskItem _task;
    private readonly IDatabaseService _db;
    private readonly ITemplateService _templateService;

    // Флаг редактирования (задача уже существует)
    public bool IsEditMode => _task.Id > 0;

    public bool IsHighSelected => PriorityIndex == 0;
    public bool IsMediumSelected => PriorityIndex == 1;
    public bool IsLowSelected => PriorityIndex == 2;

    public string HighButtonColor => PriorityIndex == 0 ? "#EF4444" : "#E5E7EB";
    public string MediumButtonColor => PriorityIndex == 1 ? "#F59E0B" : "#E5E7EB";
    public string LowButtonColor => PriorityIndex == 2 ? "#10B981" : "#E5E7EB";

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private DateTimeOffset _dueDate = DateTimeOffset.Now;

    [ObservableProperty] private bool _hasDate;
    [ObservableProperty] private bool _isTimeBound;
    [ObservableProperty] private bool _isDurationSet;

    [ObservableProperty] private int _startHour = 9;
    [ObservableProperty] private int _startMinute = 0;
    [ObservableProperty] private int _durationMinutes = 30;

    [ObservableProperty] private int _priority = 1;
    [ObservableProperty] private int _priorityIndex = 1;

    [ObservableProperty] private ObservableCollection<ProjectItem> _projectsList = new();
    [ObservableProperty] private ProjectItem? _selectedProject;

    [ObservableProperty] private bool _saveAsTemplate;
    [ObservableProperty] private string _templateName = string.Empty;

    [ObservableProperty] private ObservableCollection<TaskTemplate> _taskTemplates = new();
    [ObservableProperty] private TaskTemplate? _selectedTaskTemplate;

    // Событие для оповещения об удалении задачи
    public event Action<int>? TaskDeleted;

    partial void OnPriorityIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsHighSelected));
        OnPropertyChanged(nameof(IsMediumSelected));
        OnPropertyChanged(nameof(IsLowSelected));
        OnPropertyChanged(nameof(HighButtonColor));
        OnPropertyChanged(nameof(MediumButtonColor));
        OnPropertyChanged(nameof(LowButtonColor));
    }

    public TaskDialogViewModel(TaskItem task, ITemplateService? templateService = null)
    {
        _task = task;
        var services = ((App)Avalonia.Application.Current!).Services!;
        _db = services.GetRequiredService<IDatabaseService>();
        _templateService = templateService ?? services.GetRequiredService<ITemplateService>();

        Title = task.Title;
        Description = task.Description ?? string.Empty;

        Priority = task.Priority;
        PriorityIndex = task.Priority;

        LoadProjectsData(task.ProjectId);

        HasDate = task.DueDate.HasValue;
        if (task.DueDate.HasValue)
            DueDate = new DateTimeOffset(task.DueDate.Value, DateTimeOffset.Now.Offset);
        else
            DueDate = DateTimeOffset.Now;

        IsTimeBound = task.StartTime.HasValue;
        if (task.StartTime.HasValue)
        {
            StartHour = task.StartTime.Value.Hours;
            StartMinute = task.StartTime.Value.Minutes;
        }

        IsDurationSet = task.PlannedDurationMinutes > 0;
        DurationMinutes = IsDurationSet ? task.PlannedDurationMinutes : 30;

        LoadTaskTemplates();
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

    private void LoadTaskTemplates()
    {
        TaskTemplates.Clear();
        var templates = _templateService.GetAllTaskTemplates();
        foreach (var t in templates)
            TaskTemplates.Add(t);
    }

    partial void OnSelectedTaskTemplateChanged(TaskTemplate? value)
    {
        if (value != null)
        {
            Title = value.Title;
            Description = value.Description;
            Priority = value.Priority ?? 1;
            PriorityIndex = Priority;
            DurationMinutes = value.PlannedDurationMinutes;
            IsDurationSet = value.PlannedDurationMinutes > 0;
            HasDate = value.HasDate;
            IsTimeBound = value.IsTimeBound;
            StartHour = value.StartHour;
            StartMinute = value.StartMinute;
        }
    }

    [RelayCommand]
    private void SetPriority(string priorityStr)
    {
        if (int.TryParse(priorityStr, out int p) && p >= 0 && p <= 2)
        {
            Priority = p;
            PriorityIndex = p;
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Title))
            return;

        _task.Title = Title.Trim();
        _task.Description = Description.Trim();
        _task.Priority = PriorityIndex;
        _task.ProjectId = (SelectedProject != null && SelectedProject.Id > 0) ? SelectedProject.Id : null;
        _task.DueDate = HasDate ? DueDate.LocalDateTime.Date : null;
        _task.StartTime = IsTimeBound ? new TimeSpan(StartHour, StartMinute, 0) : null;
        _task.PlannedDurationMinutes = IsDurationSet ? DurationMinutes : 0;
        _task.Color = _task.Priority switch
        {
            0 => "#EF4444",
            1 => "#F59E0B",
            2 => "#10B981",
            _ => "#9CA3AF"
        };

        if (SaveAsTemplate && !string.IsNullOrWhiteSpace(TemplateName))
        {
            var taskTemplate = new TaskTemplate
            {
                Name = TemplateName.Trim(),
                Title = _task.Title,
                Description = _task.Description ?? string.Empty,
                PlannedDurationMinutes = _task.PlannedDurationMinutes,
                Priority = _task.Priority,
                ProjectId = _task.ProjectId,
                HasDate = HasDate,
                IsTimeBound = IsTimeBound,
                StartHour = StartHour,
                StartMinute = StartMinute
            };
            _templateService.UpsertTaskTemplate(taskTemplate);
            LoadTaskTemplates();
        }

        CloseDialog(true);
    }

    [RelayCommand]
    private void Cancel() => CloseDialog(false);

    [RelayCommand]
    private async Task DeleteAsync()
    {
        // Создаём простое окно подтверждения (без внешних пакетов)
        var owner = GetOwnerWindow();
        var confirmWindow = new Window
        {
            Title = "Подтверждение удаления",
            Width = 300,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = owner?.Background ?? new SolidColorBrush(Colors.White),
            Content = new Grid
            {
                RowDefinitions = new RowDefinitions("*, Auto"),
                Children =
                {
                    new TextBlock
                    {
                        Text = $"Вы действительно хотите удалить задачу «{_task.Title}»?",
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(10),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Spacing = 10,
                        Margin = new Thickness(10),
                        Children =
                        {
                            new Button { Content = "Да", Width = 80, Background = new SolidColorBrush(Color.Parse("#EF4444")), Foreground = Brushes.White, Tag = true },
                            new Button { Content = "Нет", Width = 80, Tag = false }
                        }
                    }
                }
            }
        };

        Button? yesButton = null, noButton = null;
        foreach (var child in ((StackPanel)((Grid)confirmWindow.Content).Children[1]).Children)
        {
            if (child is Button btn)
            {
                if (btn.Content?.ToString() == "Да") yesButton = btn;
                if (btn.Content?.ToString() == "Нет") noButton = btn;
            }
        }

        var tcs = new TaskCompletionSource<bool>();
        yesButton!.Click += (_, _) => { tcs.SetResult(true); confirmWindow.Close(); };
        noButton!.Click += (_, _) => { tcs.SetResult(false); confirmWindow.Close(); };

        await confirmWindow.ShowDialog(owner ?? confirmWindow);
        var result = await tcs.Task;

        if (result)
        {
            _db.DeleteTask(_task.Id);
            TaskDeleted?.Invoke(_task.Id);
            CloseDialog(false); // закрываем окно задачи, не сохраняя
        }
    }

    private Window? GetOwnerWindow()
    {
        if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.Windows.FirstOrDefault(w => w.DataContext == this);
        return null;
    }

    private void CloseDialog(bool result)
    {
        if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }

    [RelayCommand]
    private async void OpenProjectsManagement()
    {
        var managementVm = new ProjectsManagementViewModel();
        var window = new ProjectsManagementWindow { DataContext = managementVm };
        var desktop = App.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var owner = desktop?.MainWindow;
        if (owner != null && owner.IsVisible)
            await window.ShowDialog(owner);
        else
            window.Show();

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            int? currentSelectedId = SelectedProject?.Id;
            LoadProjectsData(currentSelectedId);
        });
    }
}