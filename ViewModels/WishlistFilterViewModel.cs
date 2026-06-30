using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Wishlist;
using FocusFlowFinal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

// ── Per-column checkbox item (for Dropdown multi-select) ──────────────────────
public partial class CheckBoxFilterItem : ObservableObject
{
    public string Option { get; }
    [ObservableProperty] private bool _isChecked;

    public CheckBoxFilterItem(string option, bool isChecked = false)
    {
        Option = option;
        _isChecked = isChecked;
    }
}

// ── One filter rule bound to a column ────────────────────────────────────────
public class FilterRuleEditItem : ObservableObject
{
    public WishlistColumn Column { get; }

    public WishlistFilterCondition[] AvailableConditions { get; }
    public string[] ConditionLabels { get; }
    public ObservableCollection<CheckBoxFilterItem> CheckBoxOptions { get; }

    private WishlistFilterCondition _condition;
    public WishlistFilterCondition Condition
    {
        get => _condition;
        set
        {
            if (SetProperty(ref _condition, value))
            {
                OnPropertyChanged(nameof(SelectedConditionIndex));
                OnPropertyChanged(nameof(IsBetween));
                OnPropertyChanged(nameof(HasTextValue));
                OnPropertyChanged(nameof(IsIn));
                OnPropertyChanged(nameof(HasAnyCondition));
            }
        }
    }

    public int SelectedConditionIndex
    {
        get => Array.IndexOf(AvailableConditions, Condition);
        set
        {
            if (value >= 0 && value < AvailableConditions.Length)
                Condition = AvailableConditions[value];
        }
    }

    private string _filterValue = string.Empty;
    public string FilterValue
    {
        get => _filterValue;
        set => SetProperty(ref _filterValue, value);
    }

    private string _filterValue2 = string.Empty;
    public string FilterValue2
    {
        get => _filterValue2;
        set => SetProperty(ref _filterValue2, value);
    }

    public bool IsBetween    => Condition == WishlistFilterCondition.Between;
    public bool IsIn         => Condition == WishlistFilterCondition.In;
    public bool HasTextValue => Condition != WishlistFilterCondition.None && !IsIn;
    public bool HasAnyCondition => Condition != WishlistFilterCondition.None;

    public FilterRuleEditItem(WishlistColumn col, WishlistFilterRule? existing)
    {
        Column = col;

        AvailableConditions = col.Type switch
        {
            WishlistColumnType.Number or WishlistColumnType.Date =>
                new[] { WishlistFilterCondition.None, WishlistFilterCondition.Equals,
                        WishlistFilterCondition.GreaterThan, WishlistFilterCondition.LessThan,
                        WishlistFilterCondition.Between },
            WishlistColumnType.Dropdown =>
                new[] { WishlistFilterCondition.None, WishlistFilterCondition.In },
            _ =>
                new[] { WishlistFilterCondition.None, WishlistFilterCondition.Contains,
                        WishlistFilterCondition.Equals, WishlistFilterCondition.NotEquals }
        };

        ConditionLabels = AvailableConditions.Select(ConditionName).ToArray();

        var opts = col.OptionsJson?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   ?? Array.Empty<string>();
        CheckBoxOptions = new ObservableCollection<CheckBoxFilterItem>(opts.Select(o => new CheckBoxFilterItem(o)));

        if (existing != null) LoadFrom(existing);
    }

    public void LoadFrom(WishlistFilterRule? rule)
    {
        if (rule == null) { Condition = WishlistFilterCondition.None; return; }
        Condition = rule.Condition;
        FilterValue = rule.Value ?? string.Empty;
        FilterValue2 = rule.Value2 ?? string.Empty;
        foreach (var cb in CheckBoxOptions)
            cb.IsChecked = rule.SelectedOptions.Contains(cb.Option);
    }

    public WishlistFilterRule ToRule() => new()
    {
        ColumnId = Column.Id,
        ColumnName = Column.Name,
        ColumnType = Column.Type,
        Condition = Condition,
        Value = FilterValue,
        Value2 = FilterValue2,
        SelectedOptions = CheckBoxOptions.Where(cb => cb.IsChecked).Select(cb => cb.Option).ToList()
    };

    private static string ConditionName(WishlistFilterCondition c) => c switch
    {
        WishlistFilterCondition.None       => "(нет фильтра)",
        WishlistFilterCondition.Contains   => "Содержит",
        WishlistFilterCondition.Equals     => "Равно",
        WishlistFilterCondition.NotEquals  => "Не равно",
        WishlistFilterCondition.GreaterThan => "Больше",
        WishlistFilterCondition.LessThan   => "Меньше",
        WishlistFilterCondition.Between    => "Между",
        WishlistFilterCondition.In         => "Одно из",
        _ => c.ToString()
    };
}

// ── Dialog ViewModel ─────────────────────────────────────────────────────────
public partial class WishlistFilterViewModel : ObservableObject
{
    private readonly IWishlistRepository _repo;
    private readonly int _wishlistId;

    public ObservableCollection<FilterRuleEditItem> Rules { get; } = new();
    public ObservableCollection<WishlistSavedFilter> SavedFilters { get; } = new();

    public bool WasApplied { get; private set; }

    [ObservableProperty] private string _saveFilterName = string.Empty;
    [ObservableProperty] private bool _isSavingFilter;

    public WishlistFilterViewModel(List<WishlistColumn> columns,
        List<WishlistFilterRule> activeRules,
        List<WishlistSavedFilter> savedFilters,
        IWishlistRepository repo, int wishlistId)
    {
        _repo = repo;
        _wishlistId = wishlistId;

        foreach (var col in columns)
        {
            var existing = activeRules.FirstOrDefault(r => r.ColumnId == col.Id);
            Rules.Add(new FilterRuleEditItem(col, existing));
        }

        foreach (var sf in savedFilters)
            SavedFilters.Add(sf);
    }

    public List<WishlistFilterRule> GetActiveRules() =>
        Rules.Where(r => r.HasAnyCondition).Select(r => r.ToRule()).ToList();

    [RelayCommand]
    private void Apply()
    {
        WasApplied = true;
        Close();
    }

    [RelayCommand]
    private void Reset()
    {
        foreach (var r in Rules)
            r.Condition = WishlistFilterCondition.None;
        WasApplied = true;
        Close();
    }

    [RelayCommand]
    private void Cancel() => Close();

    [RelayCommand]
    private void BeginSaveFilter() => IsSavingFilter = true;

    [RelayCommand]
    private void ConfirmSaveFilter()
    {
        if (string.IsNullOrWhiteSpace(SaveFilterName)) return;
        var filter = new WishlistSavedFilter
        {
            WishlistId = _wishlistId,
            Name = SaveFilterName.Trim(),
            Rules = GetActiveRules(),
            CreatedAt = DateTime.Now
        };
        filter.Id = _repo.SaveFilter(filter);
        SavedFilters.Insert(0, filter);
        IsSavingFilter = false;
        SaveFilterName = string.Empty;
    }

    [RelayCommand]
    private void CancelSaveFilter() => IsSavingFilter = false;

    [RelayCommand]
    private void LoadSavedFilter(WishlistSavedFilter? filter)
    {
        if (filter == null) return;
        foreach (var item in Rules)
        {
            var rule = filter.Rules.FirstOrDefault(r => r.ColumnId == item.Column.Id);
            item.LoadFrom(rule);
        }
    }

    [RelayCommand]
    private void DeleteSavedFilter(WishlistSavedFilter? filter)
    {
        if (filter == null) return;
        _repo.DeleteSavedFilter(filter.Id);
        SavedFilters.Remove(filter);
    }

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
