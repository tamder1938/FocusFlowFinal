using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Social;
using FocusFlowFinal.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class AddSharedEventViewModel : ObservableObject
{
    private readonly ISharedCalendarService _service;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private DateTime _startDate = DateTime.Today;
    [ObservableProperty] private TimeSpan _startTime = TimeSpan.FromHours(DateTime.Now.Hour + 1);
    [ObservableProperty] private TimeSpan _duration = TimeSpan.FromHours(1);
    [ObservableProperty] private string _color = "#6366F1";
    [ObservableProperty] private string? _selectedFriendId;
    [ObservableProperty] private string? _selectedFriendName;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<(string id, string name)> SyncFriends { get; } = new();
    public bool HasSyncFriends => SyncFriends.Count > 0;

    public bool WasSaved { get; private set; }

    public AddSharedEventViewModel(ISharedCalendarService service)
    {
        _service = service;
        _ = LoadFriendsAsync();
    }

    private async Task LoadFriendsAsync()
    {
        IsLoading = true;
        var (friends, error) = await _service.GetSyncFriendsAsync();
        IsLoading = false;

        if (error != null) { ErrorMessage = error; return; }

        SyncFriends.Clear();
        foreach (var f in friends) SyncFriends.Add(f);
        OnPropertyChanged(nameof(HasSyncFriends));

        if (SyncFriends.Count > 0)
        {
            SelectedFriendId   = SyncFriends[0].id;
            SelectedFriendName = SyncFriends[0].name;
        }
    }

    [RelayCommand]
    private void SelectFriend(string? friendId)
    {
        var f = SyncFriends.FirstOrDefault(x => x.id == friendId);
        SelectedFriendId   = f.id;
        SelectedFriendName = f.name;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Title)) { ErrorMessage = "Введите название"; return; }
        if (string.IsNullOrEmpty(SelectedFriendId)) { ErrorMessage = "Выберите друга"; return; }

        var start = StartDate.Date + StartTime;
        var ev = new SharedCalendarEvent
        {
            OwnerUserId = SelectedFriendId,
            Title   = Title.Trim(),
            StartAt = start,
            EndAt   = start + Duration,
            Color   = Color
        };

        IsLoading = true;
        var error = await _service.AddEventAsync(ev);
        IsLoading = false;

        if (error != null) { ErrorMessage = error; return; }

        WasSaved = true;
        Close();
    }

    [RelayCommand]
    private void Cancel() => Close();

    private void Close()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close();
        }
    }
}
