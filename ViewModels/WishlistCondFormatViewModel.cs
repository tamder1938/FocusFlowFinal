using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Wishlist;
using FocusFlowFinal.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

// ── One editable rule ────────────────────────────────────────────────────────
public partial class CondFormatRuleItem : ObservableObject
{
    public int Id { get; set; }
    public int WishlistId { get; }
    public int Priority { get; set; }

    public List<WishlistColumn> Columns { get; }

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private WishlistColumn? _selectedColumn;
    [ObservableProperty] private int _selectedConditionIndex;
    [ObservableProperty] private string _filterValue = string.Empty;
    [ObservableProperty] private string _filterValue2 = string.Empty;
    [ObservableProperty] private string _backgroundColor = string.Empty;
    [ObservableProperty] private string _textColor = string.Empty;
    [ObservableProperty] private bool _isBold;
    [ObservableProperty] private bool _isItalic;

    public WishlistFilterCondition[] AvailableConditions { get; } =
    {
        WishlistFilterCondition.None,
        WishlistFilterCondition.Contains,
        WishlistFilterCondition.Equals,
        WishlistFilterCondition.NotEquals,
        WishlistFilterCondition.GreaterThan,
        WishlistFilterCondition.LessThan,
        WishlistFilterCondition.Between
    };

    public string[] ConditionLabels { get; } =
    {
        "(нет)", "Содержит", "Равно", "Не равно", "Больше", "Меньше", "Между"
    };

    public bool IsBetween => AvailableConditions[SelectedConditionIndex] == WishlistFilterCondition.Between;

    partial void OnSelectedConditionIndexChanged(int value) => OnPropertyChanged(nameof(IsBetween));

    public CondFormatRuleItem(int wishlistId, List<WishlistColumn> columns, WishlistConditionalFormat? src = null)
    {
        WishlistId = wishlistId;
        Columns = columns;

        if (src != null)
        {
            Id = src.Id;
            Priority = src.Priority;
            Name = src.Name;
            SelectedColumn = columns.FirstOrDefault(c => c.Id == src.ColumnId);
            SelectedConditionIndex = System.Array.IndexOf(AvailableConditions, src.Condition);
            if (SelectedConditionIndex < 0) SelectedConditionIndex = 0;
            FilterValue = src.Value ?? string.Empty;
            FilterValue2 = src.Value2 ?? string.Empty;
            BackgroundColor = src.BackgroundColor ?? string.Empty;
            TextColor = src.TextColor ?? string.Empty;
            IsBold = src.IsBold;
            IsItalic = src.IsItalic;
        }
    }

    public WishlistConditionalFormat ToModel() => new WishlistConditionalFormat
    {
        Id = Id,
        WishlistId = WishlistId,
        Name = Name,
        ColumnId = SelectedColumn?.Id ?? 0,
        Condition = AvailableConditions[SelectedConditionIndex],
        Value = FilterValue,
        Value2 = FilterValue2,
        BackgroundColor = string.IsNullOrWhiteSpace(BackgroundColor) ? null : BackgroundColor,
        TextColor = string.IsNullOrWhiteSpace(TextColor) ? null : TextColor,
        IsBold = IsBold,
        IsItalic = IsItalic,
        Priority = Priority
    };
}

// ── Dialog ViewModel ─────────────────────────────────────────────────────────
public partial class WishlistCondFormatViewModel : ObservableObject
{
    private readonly IWishlistRepository _repo;
    private readonly int _wishlistId;
    private readonly List<WishlistColumn> _columns;

    public ObservableCollection<CondFormatRuleItem> Rules { get; } = new();

    public bool WasSaved { get; private set; }

    public WishlistCondFormatViewModel(int wishlistId, List<WishlistColumn> columns,
        List<WishlistConditionalFormat> existing, IWishlistRepository repo)
    {
        _repo = repo;
        _wishlistId = wishlistId;
        _columns = columns;

        foreach (var f in existing)
            Rules.Add(new CondFormatRuleItem(wishlistId, columns, f));
    }

    [RelayCommand]
    private void AddRule()
    {
        var item = new CondFormatRuleItem(_wishlistId, _columns) { Priority = Rules.Count };
        Rules.Add(item);
    }

    [RelayCommand]
    private void DeleteRule(CondFormatRuleItem? item)
    {
        if (item == null) return;
        if (item.Id > 0) _repo.DeleteConditionalFormat(item.Id);
        Rules.Remove(item);
    }

    [RelayCommand]
    private void MoveUp(CondFormatRuleItem? item)
    {
        if (item == null) return;
        int idx = Rules.IndexOf(item);
        if (idx <= 0) return;
        Rules.Move(idx, idx - 1);
        RefreshPriorities();
    }

    [RelayCommand]
    private void MoveDown(CondFormatRuleItem? item)
    {
        if (item == null) return;
        int idx = Rules.IndexOf(item);
        if (idx < 0 || idx >= Rules.Count - 1) return;
        Rules.Move(idx, idx + 1);
        RefreshPriorities();
    }

    private void RefreshPriorities()
    {
        for (int i = 0; i < Rules.Count; i++)
            Rules[i].Priority = i;
    }

    [RelayCommand]
    private void Save()
    {
        RefreshPriorities();
        foreach (var item in Rules)
        {
            if (item.SelectedColumn == null || string.IsNullOrWhiteSpace(item.Name)) continue;
            var model = item.ToModel();
            item.Id = _repo.SaveConditionalFormat(model);
        }
        WasSaved = true;
        Close();
    }

    [RelayCommand]
    private void Cancel() => Close();

    public List<WishlistConditionalFormat> GetRules() =>
        Rules.Select(r => r.ToModel()).Where(r => r.ColumnId > 0).ToList();

    private void Close()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var w = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            w?.Close();
        }
    }
}
