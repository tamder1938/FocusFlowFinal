using CommunityToolkit.Mvvm.ComponentModel;
using FocusFlowFinal.Models.Wishlist;
using System.Collections.Generic;

namespace FocusFlowFinal.ViewModels;

public partial class WishlistRowViewModel : ObservableObject
{
    public int RowId { get; set; }
    public int WishlistId { get; }
    public int Order { get; set; }

    private readonly Dictionary<int, WishlistCellViewModel> _cells = new();

    public WishlistRowViewModel(int wishlistId, int rowId, int order)
    {
        WishlistId = wishlistId;
        RowId = rowId;
        Order = order;
    }

    public void SetCell(WishlistCellViewModel cell) => _cells[cell.ColumnId] = cell;

    public WishlistCellViewModel? GetCell(int columnId) =>
        _cells.TryGetValue(columnId, out var cell) ? cell : null;

    public IEnumerable<WishlistCellViewModel> GetAllCells() => _cells.Values;

    public WishlistRow ToModel() => new WishlistRow
    {
        Id = RowId,
        WishlistId = WishlistId,
        Order = Order
    };
}
