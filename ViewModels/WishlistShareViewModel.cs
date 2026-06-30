using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Wishlist;
using FocusFlowFinal.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public partial class WishlistShareViewModel : ObservableObject
{
    private readonly IWishlistRepository _repo;
    private readonly WishlistItem _wishlist;

    public string WishlistName => _wishlist.Name;

    [ObservableProperty] private ObservableCollection<WishlistShare> _shares = new();
    [ObservableProperty] private string _emailInput = string.Empty;
    [ObservableProperty] private string _selectedPermission = "view";
    [ObservableProperty] private string? _errorMessage;

    public string[] Permissions { get; } = { "view", "edit" };

    public WishlistShareViewModel(WishlistItem wishlist, IWishlistRepository repo)
    {
        _wishlist = wishlist;
        _repo = repo;
        LoadShares();
    }

    private void LoadShares()
    {
        Shares.Clear();
        foreach (var s in _repo.GetShares(_wishlist.Id))
            Shares.Add(s);
    }

    [RelayCommand]
    private void AddShare()
    {
        var email = EmailInput.Trim();
        if (string.IsNullOrEmpty(email))
        {
            ErrorMessage = "Введите email";
            return;
        }
        if (Shares.Any(s => s.SharedWithEmail.Equals(email, System.StringComparison.OrdinalIgnoreCase)))
        {
            ErrorMessage = "Этот пользователь уже добавлен";
            return;
        }

        var share = new WishlistShare
        {
            WishlistId = _wishlist.Id,
            WishlistSyncId = _wishlist.SyncId,
            SharedWithEmail = email,
            Permission = SelectedPermission
        };
        _repo.AddShare(share);
        ErrorMessage = null;
        EmailInput = string.Empty;
        LoadShares();
    }

    [RelayCommand]
    private void RemoveShare(WishlistShare? share)
    {
        if (share == null) return;
        _repo.RemoveShare(share.Id);
        LoadShares();
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
