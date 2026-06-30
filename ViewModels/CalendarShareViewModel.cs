using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Social;
using FocusFlowFinal.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class CalendarShareViewModel : ObservableObject
{
    private readonly ICalendarShareService _shareService;
    private readonly IFriendService _friendService;
    private readonly IAuthService _auth;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string _selectedPermission = "view";

    public ObservableCollection<CalendarShare> MyShares { get; } = new();
    public ObservableCollection<FriendProfile> Friends { get; } = new();
    public ObservableCollection<FriendProfile> AvailableFriends { get; } = new();

    public string[] Permissions { get; } = { "view", "sync" };

    [ObservableProperty] private FriendProfile? _selectedFriend;

    public bool HasShares => MyShares.Count > 0;
    public bool HasFriends => AvailableFriends.Count > 0;
    public bool IsAuthenticated => _auth.IsAuthenticated;

    public CalendarShareViewModel(ICalendarShareService shareService,
        IFriendService friendService, IAuthService auth)
    {
        _shareService = shareService;
        _friendService = friendService;
        _auth = auth;
        if (IsAuthenticated)
            _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        var (shares, err1) = await _shareService.GetMySharesAsync();
        if (err1 != null) { ErrorMessage = err1; IsLoading = false; return; }

        MyShares.Clear();
        foreach (var s in shares) MyShares.Add(s);

        // Load friends not yet shared with
        var (friends, err2) = await _friendService.GetFriendsAsync();
        var sharedIds = shares.Select(s => s.SharedWithUserId).ToHashSet();

        AvailableFriends.Clear();
        foreach (var f in friends)
        {
            var profile = f.OtherProfile(_auth.CurrentUser!.UserId);
            if (profile != null && !sharedIds.Contains(profile.UserId))
                AvailableFriends.Add(profile);
        }

        IsLoading = false;
        OnPropertyChanged(nameof(HasShares));
        OnPropertyChanged(nameof(HasFriends));
    }

    [RelayCommand]
    private async Task ShareWith()
    {
        if (SelectedFriend == null) return;
        IsLoading = true;
        var error = await _shareService.ShareCalendarAsync(SelectedFriend.UserId, SelectedPermission);
        IsLoading = false;
        if (error != null) { ErrorMessage = error; return; }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Revoke(CalendarShare? share)
    {
        if (share == null) return;
        var error = await _shareService.RevokeShareAsync(share.Id);
        if (error != null) { ErrorMessage = error; return; }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task TogglePermission(CalendarShare? share)
    {
        if (share == null) return;
        var newPerm = share.Permission == "view" ? "sync" : "view";
        var error = await _shareService.UpdatePermissionAsync(share.Id, newPerm);
        if (error != null) { ErrorMessage = error; return; }
        await LoadAsync();
    }

    [RelayCommand]
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
