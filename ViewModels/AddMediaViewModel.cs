using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Media;
using FocusFlowFinal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class AddMediaViewModel : ObservableObject
{
    private readonly IMediaRepository    _repo;
    private readonly IMediaPosterService _posters;
    private readonly IEnumerable<string> _existingGenres;

    public LocalizationService Loc => LocalizationService.Instance;

    // ── Шаг ───────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    private int _step = 1;

    public bool IsStep1 => Step == 1;
    public bool IsStep2 => Step == 2;

    // ── Тип (выбирается на шаге 1) ────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowMovieFields))]
    [NotifyPropertyChangedFor(nameof(ShowSeriesFields))]
    [NotifyPropertyChangedFor(nameof(ShowBookFields))]
    [NotifyPropertyChangedFor(nameof(ShowMangaFields))]
    [NotifyPropertyChangedFor(nameof(ScoreVisualLabel))]
    private MediaType _selectedType = MediaType.Movie;

    public bool ShowMovieFields  => SelectedType == MediaType.Movie;
    public bool ShowSeriesFields => SelectedType is MediaType.Series or MediaType.Anime;
    public bool ShowBookFields   => SelectedType == MediaType.Book;
    public bool ShowMangaFields  => SelectedType == MediaType.Manga;
    public string ScoreVisualLabel => SelectedType is MediaType.Book or MediaType.Manga
        ? Loc["Media_ScoreStyle"] : Loc["Media_ScoreVisual"];

    // ── Поля шага 2 ───────────────────────────────────────────────────

    [ObservableProperty] private string _title          = string.Empty;
    [ObservableProperty] private string _originalTitle  = string.Empty;
    [ObservableProperty] private string _description    = string.Empty;
    [ObservableProperty] private string _director       = string.Empty;
    [ObservableProperty] private string _author         = string.Empty;
    [ObservableProperty] private string _artist         = string.Empty;
    [ObservableProperty] private string _country        = string.Empty;
    [ObservableProperty] private string _genresText     = string.Empty;
    [ObservableProperty] private string _posterUrlText  = string.Empty;
    [ObservableProperty] private string _posterFileName = string.Empty;
    [ObservableProperty] private string _isbn           = string.Empty;
    [ObservableProperty] private int?   _year;
    [ObservableProperty] private int    _durationMin;
    [ObservableProperty] private int    _seasonCount    = 1;
    [ObservableProperty] private int    _episodesPerSeason = 12;
    [ObservableProperty] private int    _episodeDuration = 25;
    [ObservableProperty] private bool   _isOngoing;
    [ObservableProperty] private int    _totalPages;
    [ObservableProperty] private int    _totalVolumes;
    [ObservableProperty] private int    _totalChapters;
    [ObservableProperty] private MediaStatus _status = MediaStatus.Planned;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public int StatusIndex
    {
        get => (int)Status;
        set => Status = (MediaStatus)value;
    }

    partial void OnStatusChanged(MediaStatus v) => OnPropertyChanged(nameof(StatusIndex));

    // Жанры — автодополнение
    public ObservableCollection<string> GenreSuggestions { get; } = new();

    // Результат
    public MediaItem? Result { get; private set; }
    public bool       Saved  { get; private set; }

    public AddMediaViewModel(IMediaRepository repo, IMediaPosterService posters)
    {
        _repo           = repo;
        _posters        = posters;
        _existingGenres = repo.GetAllGenres();
    }

    // ── Шаг 1: выбор типа ─────────────────────────────────────────────

    [RelayCommand]
    private void SelectType(string typeName)
    {
        SelectedType = Enum.Parse<MediaType>(typeName);
        Step = 2;
    }

    [RelayCommand]
    private void BackToStep1() => Step = 1;

    // ── Постер ────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task PickPosterFile()
    {
        var topLevel = GetTopLevel();
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Loc["Media_PosterFile"],
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images") { Patterns = new[] { "*.jpg","*.jpeg","*.png","*.webp" } }
            }
        });

        if (files.Count == 0) return;
        var path = files[0].TryGetLocalPath();
        if (!string.IsNullOrEmpty(path))
            PosterFileName = path;
    }

    // ── Жанры: автодополнение ─────────────────────────────────────────

    partial void OnGenresTextChanged(string v)
    {
        var last = v.Split(',').LastOrDefault()?.Trim() ?? string.Empty;
        GenreSuggestions.Clear();
        if (last.Length < 2) return;
        foreach (var g in _existingGenres.Where(x => x.StartsWith(last, StringComparison.OrdinalIgnoreCase)).Take(5))
            GenreSuggestions.Add(g);
    }

    [RelayCommand]
    private void AddGenreSuggestion(string genre)
    {
        var parts = GenresText.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
        if (parts.Count > 0) parts[^1] = genre;
        else parts.Add(genre);
        GenresText = string.Join(", ", parts) + ", ";
        GenreSuggestions.Clear();
    }

    // ── Сохранение ────────────────────────────────────────────────────

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage = "Название обязательно";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var item = new MediaItem
            {
                Title          = Title,
                OriginalTitle  = OriginalTitle,
                Description    = Description,
                Type           = SelectedType,
                Status         = Status,
                Year           = Year,
                Genres         = GenresText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                           .Distinct().ToList(),
                Director       = Director,
                Author         = Author,
                Artist         = Artist,
                Country        = Country,
                ISBN           = Isbn,
                DurationMin    = DurationMin,
                TotalPages     = TotalPages,
                TotalVolumes   = TotalVolumes,
                TotalChapters  = TotalChapters,
                IsOngoing      = IsOngoing,
                CreatedAt      = DateTime.Now
            };

            // Постер — файл
            if (!string.IsNullOrEmpty(PosterFileName) && System.IO.File.Exists(PosterFileName))
                item.PosterPath = await _posters.CopyPosterAsync(PosterFileName);
            // Постер — URL
            else if (!string.IsNullOrEmpty(PosterUrlText) && PosterUrlText.StartsWith("http"))
                item.PosterPath = await _posters.DownloadPosterAsync(PosterUrlText);

            // Сезоны для сериалов/аниме
            if (ShowSeriesFields && SeasonCount > 0)
            {
                for (int s = 1; s <= SeasonCount; s++)
                {
                    var season = new MediaSeason { SeasonNumber = s, Title = $"Сезон {s}" };
                    for (int e = 1; e <= EpisodesPerSeason; e++)
                        season.Episodes.Add(new MediaEpisode
                        {
                            EpisodeNumber = e,
                            Title         = $"Эпизод {e}",
                            DurationMin   = EpisodeDuration
                        });
                    item.Seasons.Add(season);
                }
            }

            // StartedAt / CompletedAt
            if (Status == MediaStatus.InProgress) item.StartedAt   = DateTime.Today;
            if (Status == MediaStatus.Completed)  { item.StartedAt = DateTime.Today; item.CompletedAt = DateTime.Today; }

            _repo.Upsert(item);
            Result = item;
            Saved  = true;

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    public event EventHandler? CloseRequested;

    private static Avalonia.Controls.TopLevel? GetTopLevel()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desk)
            return desk.MainWindow;
        return null;
    }
}
