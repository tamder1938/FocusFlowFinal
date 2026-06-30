using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class PlaceFormViewModel : ObservableObject
{
    private readonly IYandexMapsService _maps;
    private readonly DateTime _originalCreatedAt;
    private CancellationTokenSource? _suggestCts;
    private double? _lat;
    private double? _lon;

    [ObservableProperty] private int    _originalId;
    [ObservableProperty] private string _name          = string.Empty;
    [ObservableProperty] private int    _categoryIndex = 5; // default Прочее
    [ObservableProperty] private int    _statusIndex;       // 0=WantToVisit 1=Visited 2=Favorite
    [ObservableProperty] private string _address       = string.Empty;
    [ObservableProperty] private string _notes         = string.Empty;
    [ObservableProperty] private int    _ratingIndex;       // 0=не оценено, 1-5
    [ObservableProperty] private DateTimeOffset? _visitedAt;
    [ObservableProperty] private bool   _showSuggestions;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool     _isBusy;
    [ObservableProperty] private Bitmap?  _mapPreviewBitmap;
    [ObservableProperty] private bool     _hasMapPreview;

    public static string[] Categories => new[]
        { "Еда и напитки", "Развлечения", "Шоппинг", "Путешествия", "Спорт", "Прочее" };
    public static string[] Statuses => new[]
        { "⏳ Хочу посетить", "✓ Посетил", "⭐ Избранное" };
    public static string[] Ratings => new[]
        { "— Не оценено", "★  1", "★★  2", "★★★  3", "★★★★  4", "★★★★★  5" };

    public bool IsVisited => StatusIndex == 1;
    public bool MapsAvailable => _maps.IsConfigured;

    public ObservableCollection<string> Suggestions { get; } = new();

    public bool   Saved  { get; private set; }
    public PlaceItem? Result { get; private set; }

    public PlaceFormViewModel(PlaceItem place, IYandexMapsService maps)
    {
        _maps              = maps;
        _originalId        = place.Id;
        _originalCreatedAt = place.CreatedAt == default ? DateTime.UtcNow : place.CreatedAt;

        _name          = place.Name;
        _address       = place.Address;
        _notes         = place.Notes;
        _ratingIndex   = Math.Clamp(place.Rating, 0, 5);
        _visitedAt     = place.VisitedAt.HasValue ? new DateTimeOffset(place.VisitedAt.Value) : null;
        _statusIndex   = place.Status switch { "Visited" => 1, "Favorite" => 2, _ => 0 };
        _categoryIndex = Array.IndexOf(Categories, place.Category);
        if (_categoryIndex < 0) _categoryIndex = 5;
        _lat = place.Latitude;
        _lon = place.Longitude;

        if (place.Latitude.HasValue && place.Longitude.HasValue)
            _ = LoadMapPreviewAsync(place.Latitude.Value, place.Longitude.Value);
    }

    partial void OnStatusIndexChanged(int value) => OnPropertyChanged(nameof(IsVisited));

    partial void OnAddressChanged(string value) => _ = FetchSuggestionsAsync(value);

    private async Task FetchSuggestionsAsync(string text)
    {
        _suggestCts?.Cancel();
        _suggestCts = new CancellationTokenSource();
        var ct = _suggestCts.Token;

        Suggestions.Clear();
        ShowSuggestions = false;

        if (!_maps.IsConfigured || text.Length < 3) return;

        try
        {
            await Task.Delay(400, ct);
            IsBusy = true;
            var items = await _maps.GetSuggestionsAsync(text, ct);
            foreach (var s in items) Suggestions.Add(s.FullAddress);
            ShowSuggestions = Suggestions.Count > 0;
        }
        catch (OperationCanceledException) { }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void SelectSuggestion(string suggestion)
    {
        _suggestCts?.Cancel();
        Address = suggestion;
        Suggestions.Clear();
        ShowSuggestions = false;
        _ = GeocodeAndLoadMapAsync(suggestion);
    }

    private async Task GeocodeAndLoadMapAsync(string address)
    {
        if (!_maps.IsConfigured) return;
        IsBusy = true;
        try
        {
            var geo = await _maps.GeocodeAsync(address);
            if (geo == null) return;
            _lat = geo.Latitude;
            _lon = geo.Longitude;
            await LoadMapPreviewAsync(geo.Latitude, geo.Longitude);
        }
        catch { }
        finally { IsBusy = false; }
    }

    private async Task LoadMapPreviewAsync(double lat, double lon)
    {
        var path = await _maps.DownloadStaticMapAsync(lat, lon);
        if (path == null || !File.Exists(path)) return;
        try
        {
            MapPreviewBitmap = new Bitmap(path);
            HasMapPreview    = true;
        }
        catch { }
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Введите название места"; return; }

        string status   = StatusIndex switch { 1 => "Visited", 2 => "Favorite", _ => "WantToVisit" };
        string category = CategoryIndex >= 0 && CategoryIndex < Categories.Length
            ? Categories[CategoryIndex] : "Прочее";

        Result = new PlaceItem
        {
            Id        = OriginalId,
            Name      = Name.Trim(),
            Category  = category,
            Status    = status,
            Address   = Address.Trim(),
            Latitude  = _lat,
            Longitude = _lon,
            Notes     = Notes.Trim(),
            Rating    = RatingIndex,
            VisitedAt = StatusIndex == 1 ? (VisitedAt?.DateTime ?? DateTime.Today) : null,
            CreatedAt = _originalCreatedAt
        };
        Saved = true;
        Close();
    }

    [RelayCommand]
    private void Cancel() => Close();

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
