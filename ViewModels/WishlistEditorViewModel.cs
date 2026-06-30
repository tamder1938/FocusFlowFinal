using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Wishlist;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class WishlistEditorViewModel : ObservableObject
{
    private readonly IWishlistRepository _repo;
    private readonly WishlistExportService? _exportService;

    public WishlistItem Wishlist { get; }
    public string Title => Wishlist.Name;

    public ObservableCollection<WishlistColumn> Columns { get; } = new();
    public ObservableCollection<WishlistRowViewModel> Rows { get; } = new();

    // ── Filter state ──────────────────────────────────────────────────────
    private List<WishlistFilterRule> _activeFilterRules = new();

    public bool HasActiveFilters => _activeFilterRules.Any(r => r.Condition != WishlistFilterCondition.None);
    public int ActiveFilterCount => _activeFilterRules.Count(r => r.Condition != WishlistFilterCondition.None);

    public event EventHandler? FiltersChanged;

    public WishlistEditorViewModel(WishlistItem wishlist, IWishlistRepository repo,
        WishlistExportService? exportService = null)
    {
        Wishlist = wishlist;
        _repo = repo;
        _exportService = exportService;
        LoadData();
        LoadConditionalFormats();
    }

    private void LoadData()
    {
        Columns.Clear();
        Rows.Clear();

        var cols = _repo.GetColumns(Wishlist.Id);
        foreach (var c in cols) Columns.Add(c);

        var rows = _repo.GetRows(Wishlist.Id);
        foreach (var row in rows)
        {
            var rowVm = new WishlistRowViewModel(Wishlist.Id, row.Id, row.Order);
            var cells = _repo.GetCells(row.Id);
            foreach (var col in cols)
            {
                var cellModel = cells.FirstOrDefault(c => c.ColumnId == col.Id);
                var cellVm = new WishlistCellViewModel(col.Id, col.Type, col.OptionsJson,
                    cellModel?.Value, cellModel?.Extra);
                rowVm.SetCell(cellVm);
            }
            Rows.Add(rowVm);
        }
    }

    [RelayCommand]
    private void AddRow()
    {
        var row = new WishlistRow { WishlistId = Wishlist.Id, Order = Rows.Count };
        int rowId = _repo.UpsertRow(row);
        var rowVm = new WishlistRowViewModel(Wishlist.Id, rowId, row.Order);
        foreach (var col in Columns)
            rowVm.SetCell(new WishlistCellViewModel(col.Id, col.Type, col.OptionsJson, null, null));
        Rows.Add(rowVm);
    }

    [RelayCommand]
    private void DeleteRow(WishlistRowViewModel? rowVm)
    {
        if (rowVm == null) return;
        _repo.DeleteRow(rowVm.RowId);
        Rows.Remove(rowVm);
    }

    public void SaveCell(WishlistCellViewModel cell, int rowId) =>
        _repo.UpsertCell(cell.ToModel(rowId));

    // ── Filter commands ───────────────────────────────────────────────────

    public IEnumerable<WishlistRowViewModel> GetDisplayRows() =>
        HasActiveFilters ? Rows.Where(r => MatchesFilters(r, _activeFilterRules)) : Rows;

    public void ApplyFilters(List<WishlistFilterRule> rules)
    {
        _activeFilterRules = rules;
        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(ActiveFilterCount));
        FiltersChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ClearFilters()
    {
        _activeFilterRules.Clear();
        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(ActiveFilterCount));
        FiltersChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task OpenFilters()
    {
        var savedFilters = _repo.GetSavedFilters(Wishlist.Id);
        var vm = new WishlistFilterViewModel(Columns.ToList(), _activeFilterRules, savedFilters, _repo, Wishlist.Id);
        var dialog = new WishlistFilterDialog { DataContext = vm };
        var owner = GetOwnerWindow();
        if (owner != null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();

        if (vm.WasApplied)
            ApplyFilters(vm.GetActiveRules());
    }

    private static bool MatchesFilters(WishlistRowViewModel row, List<WishlistFilterRule> rules)
    {
        foreach (var rule in rules)
        {
            if (rule.Condition == WishlistFilterCondition.None) continue;
            var cell = row.GetCell(rule.ColumnId);
            var value = cell?.Value ?? string.Empty;

            bool matches = rule.Condition switch
            {
                WishlistFilterCondition.Contains  => value.Contains(rule.Value ?? "", StringComparison.OrdinalIgnoreCase),
                WishlistFilterCondition.Equals    => value.Equals(rule.Value ?? "", StringComparison.OrdinalIgnoreCase),
                WishlistFilterCondition.NotEquals => !value.Equals(rule.Value ?? "", StringComparison.OrdinalIgnoreCase),
                WishlistFilterCondition.GreaterThan =>
                    double.TryParse(value, out var v1) && double.TryParse(rule.Value, out var r1) && v1 > r1,
                WishlistFilterCondition.LessThan =>
                    double.TryParse(value, out var v2) && double.TryParse(rule.Value, out var r2) && v2 < r2,
                WishlistFilterCondition.Between =>
                    double.TryParse(value, out var vb) &&
                    double.TryParse(rule.Value, out var lo) &&
                    double.TryParse(rule.Value2, out var hi) &&
                    vb >= lo && vb <= hi,
                WishlistFilterCondition.In =>
                    rule.SelectedOptions.Count == 0 || rule.SelectedOptions.Contains(value),
                _ => true
            };
            if (!matches) return false;
        }
        return true;
    }

    // ── Conditional formatting ────────────────────────────────────────────
    private List<WishlistConditionalFormat> _conditionalFormats = new();

    public event EventHandler? CondFormatsChanged;

    private void LoadConditionalFormats()
    {
        _conditionalFormats = _repo.GetConditionalFormats(Wishlist.Id);
    }

    /// <summary>Returns the effective background color for a cell, respecting manual override.</summary>
    public string? GetCellBackground(WishlistCellViewModel cell, WishlistColumn col)
    {
        // Manual color on Color-type column always wins
        if (col.Type == WishlistColumnType.Color && !string.IsNullOrWhiteSpace(cell.Value))
            return cell.Value;

        // Check conditional formats in priority order
        foreach (var fmt in _conditionalFormats)
        {
            if (fmt.ColumnId != col.Id) continue;
            if (CellMatchesCondition(cell, fmt))
                return fmt.BackgroundColor;
        }
        return null;
    }

    private static bool CellMatchesCondition(WishlistCellViewModel cell, WishlistConditionalFormat fmt)
    {
        var value = cell.Value ?? string.Empty;
        return fmt.Condition switch
        {
            WishlistFilterCondition.Contains   => value.Contains(fmt.Value ?? "", StringComparison.OrdinalIgnoreCase),
            WishlistFilterCondition.Equals     => value.Equals(fmt.Value ?? "", StringComparison.OrdinalIgnoreCase),
            WishlistFilterCondition.NotEquals  => !value.Equals(fmt.Value ?? "", StringComparison.OrdinalIgnoreCase),
            WishlistFilterCondition.GreaterThan =>
                double.TryParse(value, out var v1) && double.TryParse(fmt.Value, out var r1) && v1 > r1,
            WishlistFilterCondition.LessThan =>
                double.TryParse(value, out var v2) && double.TryParse(fmt.Value, out var r2) && v2 < r2,
            WishlistFilterCondition.Between =>
                double.TryParse(value, out var vb) &&
                double.TryParse(fmt.Value, out var lo) &&
                double.TryParse(fmt.Value2, out var hi) &&
                vb >= lo && vb <= hi,
            _ => false
        };
    }

    [RelayCommand]
    private async Task OpenCondFormats()
    {
        var existing = _repo.GetConditionalFormats(Wishlist.Id);
        var vm = new WishlistCondFormatViewModel(Wishlist.Id, Columns.ToList(), existing, _repo);
        var dialog = new WishlistCondFormatDialog { DataContext = vm };
        var owner = GetOwnerWindow();
        if (owner != null) await dialog.ShowDialog(owner);
        else dialog.Show();

        if (vm.WasSaved)
        {
            LoadConditionalFormats();
            CondFormatsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private Avalonia.Controls.Window? GetOwnerWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.Windows.FirstOrDefault(w => w.DataContext == this);
        return null;
    }

    // ── Export / Import ───────────────────────────────────────────────────

    [RelayCommand]
    private async Task ExportCsv()
    {
        if (_exportService == null) return;
        var path = await PickSaveFile("CSV", "csv");
        if (path == null) return;
        _exportService.ExportCsv(Wishlist, path);
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        if (_exportService == null) return;
        var path = await PickSaveFile("Excel", "xlsx");
        if (path == null) return;
        _exportService.ExportExcel(Wishlist, path);
    }

    [RelayCommand]
    private async Task ExportJson()
    {
        if (_exportService == null) return;
        var path = await PickSaveFile("JSON", "json");
        if (path == null) return;
        _exportService.ExportJson(Wishlist, path);
    }

    [RelayCommand]
    private async Task ImportCsv()
    {
        if (_exportService == null) return;
        var owner = GetOwnerWindow();
        if (owner == null) return;
        var files = await Avalonia.Controls.TopLevel.GetTopLevel(owner)!
            .StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Импорт CSV",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } }
                }
            });
        if (files.Count == 0) return;

        var (_, error) = _exportService.ImportCsv(Wishlist, files[0].Path.LocalPath);
        if (error == null)
            LoadData();
    }

    private async Task<string?> PickSaveFile(string typeName, string extension)
    {
        var owner = GetOwnerWindow();
        if (owner == null) return null;
        var file = await Avalonia.Controls.TopLevel.GetTopLevel(owner)!
            .StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = $"Экспорт {typeName}",
                SuggestedFileName = Wishlist.Name,
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType(typeName)
                        { Patterns = new[] { $"*.{extension}" } }
                }
            });
        return file?.Path.LocalPath;
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
