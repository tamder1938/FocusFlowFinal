using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Wishlist;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class WishlistListViewModel : ObservableObject
{
    private readonly IWishlistRepository _repo;
    private readonly WishlistExportService? _exportService;

    [ObservableProperty] private ObservableCollection<WishlistItem> _wishlists = new();

    public bool HasWishlists => Wishlists.Count > 0;
    public bool IsEmpty => Wishlists.Count == 0;

    public WishlistListViewModel(IWishlistRepository repo, WishlistExportService? exportService = null)
    {
        _repo = repo;
        _exportService = exportService;
        LoadWishlists();
    }

    private void LoadWishlists()
    {
        Wishlists.Clear();
        foreach (var w in _repo.GetAll())
            Wishlists.Add(w);
        OnPropertyChanged(nameof(HasWishlists));
        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    private async Task CreateWishlist()
    {
        var wishlist = new WishlistItem();
        var vm = new ColumnSetupViewModel(wishlist, _repo, loadExisting: false);
        var dialog = new ColumnSetupDialog { DataContext = vm };
        var owner = GetOwnerWindow();
        if (owner != null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();
        LoadWishlists();
    }

    [RelayCommand]
    private async Task OpenWishlist(WishlistItem wishlist)
    {
        if (wishlist == null) return;
        var vm = new WishlistEditorViewModel(wishlist, _repo, _exportService);
        var win = new WishlistEditorWindow { DataContext = vm };
        var owner = GetOwnerWindow();
        if (owner != null)
            await win.ShowDialog(owner);
        else
            win.Show();
        LoadWishlists();
    }

    [RelayCommand]
    private async Task EditWishlistColumns(WishlistItem wishlist)
    {
        if (wishlist == null) return;
        var vm = new ColumnSetupViewModel(wishlist, _repo, loadExisting: true);
        var dialog = new ColumnSetupDialog { DataContext = vm };
        var owner = GetOwnerWindow();
        if (owner != null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();
        LoadWishlists();
    }

    [RelayCommand]
    private async Task ShareWishlist(WishlistItem wishlist)
    {
        if (wishlist == null) return;
        var vm = new WishlistShareViewModel(wishlist, _repo);
        var dialog = new WishlistShareDialog { DataContext = vm };
        var owner = GetOwnerWindow();
        if (owner != null) await dialog.ShowDialog(owner);
        else dialog.Show();
    }

    [RelayCommand]
    private async Task DeleteWishlist(WishlistItem wishlist)
    {
        if (wishlist == null) return;
        var owner = GetOwnerWindow();
        if (owner != null && !await ConfirmAsync(owner)) return;
        _repo.Delete(wishlist.Id);
        LoadWishlists();
    }

    private static Window? GetOwnerWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.Windows.FirstOrDefault(w => w is WishlistWindow);
        return null;
    }

    private static async Task<bool> ConfirmAsync(Window owner)
    {
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        var dialog = new Window
        {
            Width = 300, Height = 130,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Avalonia.Media.Brushes.White,
            Title = "Подтверждение"
        };
        var panel = new Avalonia.Controls.StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 16 };
        panel.Children.Add(new TextBlock { Text = "Удалить вишлист и все его данные?", FontWeight = Avalonia.Media.FontWeight.SemiBold, FontSize = 13 });
        var btns = new Avalonia.Controls.StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        var cancelBtn = new Button { Content = "Отмена" };
        var okBtn = new Button { Content = "Удалить", Background = Avalonia.Media.Brushes.OrangeRed, Foreground = Avalonia.Media.Brushes.White };
        cancelBtn.Click += (_, _) => { tcs.TrySetResult(false); dialog.Close(); };
        okBtn.Click     += (_, _) => { tcs.TrySetResult(true);  dialog.Close(); };
        btns.Children.Add(cancelBtn);
        btns.Children.Add(okBtn);
        panel.Children.Add(btns);
        dialog.Content = panel;
        _ = dialog.ShowDialog(owner);
        return await tcs.Task;
    }
}
