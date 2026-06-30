using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public partial class TimerTemplateDialogViewModel : ObservableObject
{
    private readonly TimerTemplate _template;
    private readonly IDatabaseService _db;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private int _workMinutes = 25;
    [ObservableProperty] private int _breakMinutes = 5;
    [ObservableProperty] private int _cycles = 4;

    public TimerTemplateDialogViewModel(TimerTemplate template, IDatabaseService? db = null)
    {
        _template = template;
        var services = ((App)Avalonia.Application.Current!).Services!;
        _db = db ?? services.GetRequiredService<IDatabaseService>();

        Name         = template.Name;
        WorkMinutes  = template.WorkMinutes;
        BreakMinutes = template.BreakMinutes;
        Cycles       = template.Cycles;
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;

        _template.Name         = Name.Trim();
        _template.WorkMinutes  = WorkMinutes;
        _template.BreakMinutes = BreakMinutes;
        _template.Cycles       = Cycles;

        _db.UpsertTimerTemplate(_template);
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
