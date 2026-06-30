using CommunityToolkit.Mvvm.ComponentModel;
using FocusFlowFinal.Models.Wishlist;
using System.Diagnostics;

namespace FocusFlowFinal.ViewModels;

public partial class WishlistCellViewModel : ObservableObject
{
    public int ColumnId { get; }
    public WishlistColumnType ColumnType { get; }
    public string? OptionsJson { get; }

    [ObservableProperty] private string? _value;
    [ObservableProperty] private string? _extra;

    public bool IsText     => ColumnType == WishlistColumnType.Text;
    public bool IsNumber   => ColumnType == WishlistColumnType.Number;
    public bool IsDate     => ColumnType == WishlistColumnType.Date;
    public bool IsLink     => ColumnType == WishlistColumnType.Link;
    public bool IsDropdown => ColumnType == WishlistColumnType.Dropdown;
    public bool IsImage    => ColumnType == WishlistColumnType.Image;
    public bool IsColor    => ColumnType == WishlistColumnType.Color;

    // Dropdown options parsed from OptionsJson (comma-separated)
    public string[] DropdownOptions { get; }

    // Link display
    public string LinkCaption => string.IsNullOrWhiteSpace(Extra) ? (Value ?? string.Empty) : Extra;
    public bool HasLink => !string.IsNullOrWhiteSpace(Value) &&
                           (Value.StartsWith("http://") || Value.StartsWith("https://"));

    // Color background
    public string CellBackground => (ColumnType == WishlistColumnType.Color && !string.IsNullOrWhiteSpace(Value))
        ? Value! : "Transparent";

    public WishlistCellViewModel(int columnId, WishlistColumnType columnType, string? optionsJson,
        string? value, string? extra)
    {
        ColumnId = columnId;
        ColumnType = columnType;
        OptionsJson = optionsJson;
        _value = value;
        _extra = extra;
        DropdownOptions = ParseOptions(optionsJson);
    }

    private static string[] ParseOptions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return System.Array.Empty<string>();
        return json.Split(',', System.StringSplitOptions.RemoveEmptyEntries
                             | System.StringSplitOptions.TrimEntries);
    }

    public void OpenLink()
    {
        if (!HasLink) return;
        try { Process.Start(new ProcessStartInfo(Value!) { UseShellExecute = true }); }
        catch { }
    }

    public WishlistCell ToModel(int rowId) => new WishlistCell
    {
        RowId = rowId,
        ColumnId = ColumnId,
        Value = Value,
        Extra = Extra
    };
}
