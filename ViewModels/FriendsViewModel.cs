using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Social;
using FocusFlowFinal.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class FriendsViewModel : ObservableObject
{
    private readonly IFriendService _service;
    private readonly IAuthService _auth;

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isLoading;

    // Tabs
    [ObservableProperty] private bool _isFriendsTabActive = true;
    [ObservableProperty] private bool _isIncomingTabActive;
    [ObservableProperty] private bool _isOutgoingTabActive;

    public ObservableCollection<FriendProfile> SearchResults { get; } = new();
    public ObservableCollection<Friendship> Friends { get; } = new();
    public ObservableCollection<Friendship> IncomingRequests { get; } = new();
    public ObservableCollection<Friendship> OutgoingRequests { get; } = new();

    public bool IsAuthenticated => _auth.IsAuthenticated;
    public bool HasSearchResults => SearchResults.Count > 0;
    public bool FriendsEmpty => Friends.Count == 0 && !IsLoading;
    public bool IncomingEmpty => IncomingRequests.Count == 0 && !IsLoading;
    public bool OutgoingEmpty => OutgoingRequests.Count == 0 && !IsLoading;

    public FriendsViewModel(IFriendService service, IAuthService auth)
    {
        _service = service;
        _auth = auth;
        if (IsAuthenticated)
            _ = LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        var (friends, err1) = await _service.GetFriendsAsync();
        if (err1 != null) { ErrorMessage = err1; IsLoading = false; return; }

        var (incoming, err2) = await _service.GetIncomingRequestsAsync();
        var (outgoing, _)    = await _service.GetOutgoingRequestsAsync();

        Friends.Clear();
        foreach (var f in friends) Friends.Add(f);

        IncomingRequests.Clear();
        foreach (var r in incoming) IncomingRequests.Add(r);

        OutgoingRequests.Clear();
        foreach (var r in outgoing) OutgoingRequests.Add(r);

        IsLoading = false;
        RefreshCounts();
    }

    [RelayCommand]
    private async Task Search()
    {
        var q = SearchQuery.Trim();
        SearchResults.Clear();
        if (string.IsNullOrEmpty(q)) return;

        IsLoading = true;
        var (results, error) = await _service.SearchUsersAsync(q);
        IsLoading = false;

        if (error != null) { ErrorMessage = error; return; }

        foreach (var p in results) SearchResults.Add(p);
        OnPropertyChanged(nameof(HasSearchResults));
    }

    [RelayCommand]
    private async Task SendRequest(FriendProfile? profile)
    {
        if (profile == null) return;
        IsLoading = true;
        var error = await _service.SendRequestAsync(profile.UserId);
        IsLoading = false;
        if (error != null) { ErrorMessage = error; return; }
        SearchResults.Remove(profile);
        OnPropertyChanged(nameof(HasSearchResults));
        await LoadAllAsync();
    }

    [RelayCommand]
    private async Task Accept(Friendship? friendship)
    {
        if (friendship == null) return;
        var error = await _service.AcceptRequestAsync(friendship.Id);
        if (error != null) { ErrorMessage = error; return; }
        await LoadAllAsync();
    }

    [RelayCommand]
    private async Task Decline(Friendship? friendship)
    {
        if (friendship == null) return;
        var error = await _service.DeclineRequestAsync(friendship.Id);
        if (error != null) { ErrorMessage = error; return; }
        await LoadAllAsync();
    }

    [RelayCommand]
    private async Task RemoveFriend(Friendship? friendship)
    {
        if (friendship == null) return;
        var error = await _service.DeclineRequestAsync(friendship.Id);
        if (error != null) { ErrorMessage = error; return; }
        await LoadAllAsync();
    }

    [RelayCommand]
    private async Task CancelRequest(Friendship? friendship)
    {
        if (friendship == null) return;
        var error = await _service.DeclineRequestAsync(friendship.Id);
        if (error != null) { ErrorMessage = error; return; }
        await LoadAllAsync();
    }

    [RelayCommand]
    private void SelectFriendsTab()
    {
        IsFriendsTabActive  = true;
        IsIncomingTabActive = false;
        IsOutgoingTabActive = false;
    }

    [RelayCommand]
    private void SelectIncomingTab()
    {
        IsFriendsTabActive  = false;
        IsIncomingTabActive = true;
        IsOutgoingTabActive = false;
    }

    [RelayCommand]
    private void SelectOutgoingTab()
    {
        IsFriendsTabActive  = false;
        IsIncomingTabActive = false;
        IsOutgoingTabActive = true;
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        SearchResults.Clear();
        OnPropertyChanged(nameof(HasSearchResults));
    }

    private void RefreshCounts()
    {
        OnPropertyChanged(nameof(FriendsEmpty));
        OnPropertyChanged(nameof(IncomingEmpty));
        OnPropertyChanged(nameof(OutgoingEmpty));
    }
}
