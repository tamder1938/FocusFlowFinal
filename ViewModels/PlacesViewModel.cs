using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class PlacesViewModel : ObservableObject
{
    private readonly IPlaceRepository    _repo;
    private readonly IYandexMapsService  _maps;
    private readonly PlaceExportService  _export;

    [ObservableProperty] private string  _searchText        = string.Empty;
    [ObservableProperty] private string? _exportMessage;
    [ObservableProperty] private int     _statusIndex;        // 0=Все 1=Хочу 2=Посетил 3=Избранное
    [ObservableProperty] private int     _categoryComboIndex; // 0=Все, 1-6=category
    [ObservableProperty] private int     _totalCount;
    [ObservableProperty] private bool    _isEmpty = true;

    public ObservableCollection<PlaceItem> Places  { get; } = new();
    public string[] CategoryLabels { get; } =
        { "Все категории", "Еда и напитки", "Развлечения", "Шоппинг", "Путешествия", "Спорт", "Прочее" };
    private static readonly string?[] CategoryValues =
        { null, "Еда и напитки", "Развлечения", "Шоппинг", "Путешествия", "Спорт", "Прочее" };
    public string[] StatusOptions { get; } =
        { "Все", "⏳ Хочу посетить", "✓ Посетил", "⭐ Избранное" };

    private List<PlaceItem> _all = new();

    public PlacesViewModel(IPlaceRepository repo, IYandexMapsService maps, PlaceExportService export)
    {
        _repo   = repo;
        _maps   = maps;
        _export = export;
        LoadAll();
    }

    private void LoadAll()
    {
        _all = _repo.GetAll();
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var q = _all.AsEnumerable();

        q = StatusIndex switch
        {
            1 => q.Where(p => p.Status == "WantToVisit"),
            2 => q.Where(p => p.Status == "Visited"),
            3 => q.Where(p => p.Status == "Favorite"),
            _ => q
        };

        var cat = CategoryValues[CategoryComboIndex];
        if (cat != null)
            q = q.Where(p => p.Category == cat);

        if (!string.IsNullOrWhiteSpace(SearchText))
            q = q.Where(p =>
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                p.Address.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        Places.Clear();
        foreach (var p in q) Places.Add(p);
        TotalCount = Places.Count;
        IsEmpty    = Places.Count == 0;
    }

    partial void OnSearchTextChanged(string value)       => ApplyFilters();
    partial void OnStatusIndexChanged(int value)         => ApplyFilters();
    partial void OnCategoryComboIndexChanged(int value)  => ApplyFilters();

    // ── Commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task AddPlace()
    {
        var vm     = new PlaceFormViewModel(new PlaceItem(), _maps);
        var dialog = new PlaceFormDialog { DataContext = vm };
        await ShowDialogAsync(dialog);
        if (vm.Saved && vm.Result != null)
        {
            _repo.Upsert(vm.Result);
            LoadAll();
        }
    }

    [RelayCommand]
    private async Task EditPlace(PlaceItem place)
    {
        var copy = Clone(place);
        var vm   = new PlaceFormViewModel(copy, _maps);
        var dialog = new PlaceFormDialog { DataContext = vm };
        await ShowDialogAsync(dialog);
        if (vm.Saved && vm.Result != null)
        {
            _repo.Upsert(vm.Result);
            LoadAll();
        }
    }

    [RelayCommand]
    private void DeletePlace(PlaceItem place)
    {
        _repo.Delete(place.Id);
        LoadAll();
    }

    [RelayCommand]
    private void MarkVisited(PlaceItem place)
    {
        place.Status    = "Visited";
        place.VisitedAt = DateTime.Today;
        _repo.Upsert(place);
        LoadAll();
    }

    [RelayCommand]
    private void ToggleFavorite(PlaceItem place)
    {
        place.Status = place.Status == "Favorite" ? "WantToVisit" : "Favorite";
        _repo.Upsert(place);
        LoadAll();
    }

    [RelayCommand]
    private async Task ExportCsv()
    {
        if (_all.Count == 0) { ExportMessage = "Нет мест для экспорта"; return; }
        try
        {
            var path = await _export.ExportToCsvAsync(_all);
            ExportMessage = $"Сохранено: {System.IO.Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            ExportMessage = $"Ошибка: {ex.Message}";
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task ShowDialogAsync(Avalonia.Controls.Window dialog)
    {
        var desktop = Avalonia.Application.Current?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var owner = desktop?.Windows.OfType<PlacesWindow>().FirstOrDefault();
        if (owner != null) await dialog.ShowDialog(owner);
        else               dialog.Show();
    }

    private static PlaceItem Clone(PlaceItem src) => new()
    {
        Id = src.Id, UserId = src.UserId, Name = src.Name,
        Category = src.Category, Status = src.Status,
        Address = src.Address, Latitude = src.Latitude, Longitude = src.Longitude,
        Notes = src.Notes, Rating = src.Rating,
        CreatedAt = src.CreatedAt, VisitedAt = src.VisitedAt
    };
}
