using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Wishlist;
using FocusFlowFinal.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public partial class ColumnSetupViewModel : ObservableObject
{
    private readonly IWishlistRepository _repo;
    private readonly WishlistItem _wishlist;

    [ObservableProperty] private string _wishlistName = string.Empty;
    [ObservableProperty] private string _wishlistDescription = string.Empty;
    [ObservableProperty] private ObservableCollection<ColumnEditItem> _columns = new();

    public bool IsNew => _wishlist.Id == 0;

    public static WishlistColumnType[] AllColumnTypes { get; } =
        System.Enum.GetValues<WishlistColumnType>();

    public ColumnSetupViewModel(WishlistItem wishlist, IWishlistRepository repo, bool loadExisting = false)
    {
        _wishlist = wishlist;
        _repo = repo;
        WishlistName = wishlist.Name;
        WishlistDescription = wishlist.Description;

        if (loadExisting && wishlist.Id > 0)
        {
            foreach (var c in repo.GetColumns(wishlist.Id))
                Columns.Add(new ColumnEditItem(c.Name, c.Type, c.OptionsJson));
        }
    }

    [RelayCommand]
    private void AddColumn()
    {
        var item = new ColumnEditItem("Колонка " + (Columns.Count + 1), WishlistColumnType.Text, null);
        item.RemoveCommand = new RelayCommand(() => Columns.Remove(item));
        item.MoveUpCommand = new RelayCommand(() => MoveColumn(item, -1));
        item.MoveDownCommand = new RelayCommand(() => MoveColumn(item, +1));
        Columns.Add(item);
    }

    private void MoveColumn(ColumnEditItem item, int direction)
    {
        int idx = Columns.IndexOf(item);
        int newIdx = idx + direction;
        if (newIdx < 0 || newIdx >= Columns.Count) return;
        Columns.Move(idx, newIdx);
    }

    [RelayCommand]
    private void ApplyGiftsTemplate()  => ApplyTemplate(GiftsTemplate);
    [RelayCommand]
    private void ApplyBooksTemplate()  => ApplyTemplate(BooksTemplate);
    [RelayCommand]
    private void ApplyFilmsTemplate()  => ApplyTemplate(FilmsTemplate);

    private void ApplyTemplate((string name, WishlistColumnType type)[] template)
    {
        Columns.Clear();
        foreach (var (name, type) in template)
        {
            var item = new ColumnEditItem(name, type, null);
            item.RemoveCommand = new RelayCommand(() => Columns.Remove(item));
            item.MoveUpCommand = new RelayCommand(() => MoveColumn(item, -1));
            item.MoveDownCommand = new RelayCommand(() => MoveColumn(item, +1));
            Columns.Add(item);
        }
    }

    private static readonly (string, WishlistColumnType)[] GiftsTemplate =
    {
        ("Название", WishlistColumnType.Text),
        ("Цена", WishlistColumnType.Number),
        ("Ссылка", WishlistColumnType.Link),
        ("Приоритет", WishlistColumnType.Dropdown),
        ("Фото", WishlistColumnType.Image),
        ("Куплено", WishlistColumnType.Dropdown),
    };

    private static readonly (string, WishlistColumnType)[] BooksTemplate =
    {
        ("Название", WishlistColumnType.Text),
        ("Автор", WishlistColumnType.Text),
        ("Жанр", WishlistColumnType.Dropdown),
        ("Статус", WishlistColumnType.Dropdown),
        ("Оценка", WishlistColumnType.Number),
        ("Ссылка", WishlistColumnType.Link),
    };

    private static readonly (string, WishlistColumnType)[] FilmsTemplate =
    {
        ("Название", WishlistColumnType.Text),
        ("Жанр", WishlistColumnType.Dropdown),
        ("Год", WishlistColumnType.Number),
        ("Статус", WishlistColumnType.Dropdown),
        ("Оценка", WishlistColumnType.Number),
        ("Ссылка", WishlistColumnType.Link),
    };

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(WishlistName)) return;

        _wishlist.Name = WishlistName.Trim();
        _wishlist.Description = WishlistDescription.Trim();
        int id = _repo.Upsert(_wishlist);

        var cols = Columns.Select((item, idx) => new WishlistColumn
        {
            WishlistId = id,
            Name = item.Name,
            Type = item.Type,
            OptionsJson = item.OptionsText,
            Order = idx
        });
        _repo.SaveColumns(id, cols);

        CloseDialog(true);
    }

    [RelayCommand]
    private void Cancel() => CloseDialog(false);

    private void CloseDialog(bool result)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }
}

public partial class ColumnEditItem : ObservableObject
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private WishlistColumnType _type;
    [ObservableProperty] private string? _optionsText;

    public bool IsDropdown => Type == WishlistColumnType.Dropdown;

    public System.Windows.Input.ICommand? RemoveCommand { get; set; }
    public System.Windows.Input.ICommand? MoveUpCommand { get; set; }
    public System.Windows.Input.ICommand? MoveDownCommand { get; set; }

    public ColumnEditItem(string name, WishlistColumnType type, string? optionsText)
    {
        _name = name;
        _type = type;
        _optionsText = optionsText;
    }

    partial void OnTypeChanged(WishlistColumnType value) => OnPropertyChanged(nameof(IsDropdown));
}
