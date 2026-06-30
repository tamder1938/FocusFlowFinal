using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public partial class EventTemplateDialogViewModel : ObservableObject
{
    private readonly EventTemplate _template;
    private readonly ITemplateService _templateService;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private bool _isAllDay;
    [ObservableProperty] private int _startHour = 9;
    [ObservableProperty] private int _startMinute;
    [ObservableProperty] private int _endHour = 10;
    [ObservableProperty] private int _endMinute;

    public EventTemplateDialogViewModel(EventTemplate template, ITemplateService? templateService = null)
    {
        _template = template;
        var services = ((App)Avalonia.Application.Current!).Services!;
        _templateService = templateService ?? services.GetRequiredService<ITemplateService>();

        Name        = template.Name;
        Title       = template.Title;
        IsAllDay    = template.IsAllDay;
        StartHour   = template.StartHour;
        StartMinute = template.StartMinute;
        EndHour     = template.EndHour > 0 ? template.EndHour : 10;
        EndMinute   = template.EndMinute;
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;

        _template.Name        = Name.Trim();
        _template.Title       = Title.Trim();
        _template.IsAllDay    = IsAllDay;
        _template.StartHour   = StartHour;
        _template.StartMinute = StartMinute;
        _template.EndHour     = EndHour;
        _template.EndMinute   = EndMinute;

        _templateService.SaveEventTemplate(_template);
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
