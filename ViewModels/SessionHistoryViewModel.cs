using CommunityToolkit.Mvvm.ComponentModel;
using FocusFlowFinal.Models.Workout;
using FocusFlowFinal.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public partial class SessionHistoryViewModel : ObservableObject
{
    private readonly IWorkoutRepository _repo;

    public ObservableCollection<SessionHistoryItemViewModel> Sessions { get; } = new();

    [ObservableProperty] private int    _totalSessions;
    [ObservableProperty] private string _totalTonnageLabel   = "0 кг";
    [ObservableProperty] private string _totalDurationLabel  = "0 мин";
    [ObservableProperty] private bool   _isEmpty = true;

    public SessionHistoryViewModel(IWorkoutRepository repo)
    {
        _repo = repo;
        Refresh();
    }

    public void Refresh()
    {
        Sessions.Clear();

        var all = _repo.GetRecentSessions(100).ToList();
        foreach (var s in all)
        {
            var vm = new SessionHistoryItemViewModel(s);
            vm.DeleteRequested += OnDeleteRequested;
            Sessions.Add(vm);
        }

        TotalSessions = all.Count;
        IsEmpty       = TotalSessions == 0;

        var totalT = all.Sum(s => s.TotalTonnage);
        TotalTonnageLabel = totalT >= 1000
            ? $"{totalT / 1000:0.#} т"
            : $"{totalT:0.#} кг";

        var totalMin = (int)all
            .Where(s => s.FinishedAt.HasValue)
            .Sum(s => (s.FinishedAt!.Value - s.StartedAt).TotalMinutes);

        TotalDurationLabel = totalMin >= 60
            ? $"{totalMin / 60} ч {totalMin % 60} мин"
            : $"{totalMin} мин";
    }

    private void OnDeleteRequested(object? sender, WorkoutSession session)
    {
        _repo.DeleteSession(session.Id);
        Refresh();
    }
}
