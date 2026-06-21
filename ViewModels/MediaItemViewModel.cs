using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Media;
using FocusFlowFinal.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

// ── Обёртка эпизода для списка ──────────────────────────────────────────

public partial class EpisodeViewModel : ObservableObject
{
    private readonly MediaEpisode _ep;
    private readonly MediaItemViewModel _parent;

    public int    EpisodeNumber => _ep.EpisodeNumber;
    public string Title         => _ep.Title;
    public int    DurationMin   => _ep.DurationMin;
    public string WatchedDateStr => _ep.WatchedAt.HasValue
        ? _ep.WatchedAt.Value.ToString("dd.MM.yyyy") : string.Empty;

    [ObservableProperty] private bool _isWatched;

    public EpisodeViewModel(MediaEpisode ep, MediaItemViewModel parent)
    {
        _ep = ep; _parent = parent;
        _isWatched = ep.IsWatched;
    }

    partial void OnIsWatchedChanged(bool v)
    {
        _ep.IsWatched = v;
        _ep.WatchedAt = v ? DateTime.Today : null;
        _parent.OnEpisodeChanged();
    }

    public MediaEpisode Source => _ep;
}

// ── Обёртка сезона ──────────────────────────────────────────────────────

public partial class SeasonViewModel : ObservableObject
{
    private readonly MediaSeason _season;
    public int    SeasonNumber => _season.SeasonNumber;
    public string Title        => string.IsNullOrEmpty(_season.Title)
        ? $"Сезон {_season.SeasonNumber}" : _season.Title;

    public ObservableCollection<EpisodeViewModel> Episodes { get; } = new();

    public SeasonViewModel(MediaSeason season, MediaItemViewModel parent)
    {
        _season = season;
        foreach (var ep in season.Episodes.OrderBy(e => e.EpisodeNumber))
            Episodes.Add(new EpisodeViewModel(ep, parent));
    }

    public int WatchedCount => Episodes.Count(e => e.IsWatched);
    public int TotalCount   => Episodes.Count;
    public string ProgressLabel => $"{WatchedCount} / {TotalCount} эп.";
    public double ProgressFraction => TotalCount > 0 ? (double)WatchedCount / TotalCount : 0;

    public void RefreshProgress()
    {
        OnPropertyChanged(nameof(WatchedCount));
        OnPropertyChanged(nameof(ProgressLabel));
        OnPropertyChanged(nameof(ProgressFraction));
    }

    public MediaSeason Source => _season;
}

// ── Главный VM детальной панели ─────────────────────────────────────────

public partial class MediaItemViewModel : ObservableObject
{
    private readonly IMediaRepository   _repo;
    private readonly IMediaPosterService _posters;
    private MediaItem _item;
    private bool _suppressScoreRecalc;

    public LocalizationService Loc => LocalizationService.Instance;
    public MediaItem Source => _item;

    // ── Основные поля ─────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TypeYearLabel))]
    [NotifyPropertyChangedFor(nameof(TypeLabel))]
    private string _title = string.Empty;

    [ObservableProperty] private string _originalTitle = string.Empty;
    [ObservableProperty] private string _description   = string.Empty;
    [ObservableProperty] private string _director      = string.Empty;
    [ObservableProperty] private string _author        = string.Empty;
    [ObservableProperty] private string _country       = string.Empty;
    [ObservableProperty] private string _viewingSource  = string.Empty;
    [ObservableProperty] private string _personalNote  = string.Empty;
    [ObservableProperty] private string _genresDisplay = string.Empty;
    [ObservableProperty] private int?   _year;
    [ObservableProperty] private int    _durationMin;
    [ObservableProperty] private int    _totalPages;
    [ObservableProperty] private int    _currentPage;
    [ObservableProperty] private bool   _isOngoing;
    [ObservableProperty] private bool   _descExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    [NotifyPropertyChangedFor(nameof(IsCompleted))]
    [NotifyPropertyChangedFor(nameof(ShowEpisodes))]
    [NotifyPropertyChangedFor(nameof(ShowChapters))]
    [NotifyPropertyChangedFor(nameof(ShowReadingProgress))]
    [NotifyPropertyChangedFor(nameof(ShowRatings))]
    private MediaStatus _status;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEpisodes))]
    [NotifyPropertyChangedFor(nameof(ShowChapters))]
    [NotifyPropertyChangedFor(nameof(ShowReadingProgress))]
    [NotifyPropertyChangedFor(nameof(ScoreVisualLabel))]
    private MediaType _type;

    // ── Оценки ────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OverallScoreDisplay))]
    private double _scorePlot;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OverallScoreDisplay))]
    private double _scoreVisual;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OverallScoreDisplay))]
    private double _scorePersonal;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OverallScoreDisplay))]
    [NotifyPropertyChangedFor(nameof(StarsDisplay))]
    private double _overallScore;

    [ObservableProperty] private bool   _isOverallManual;
    [ObservableProperty] private bool   _isEditingOverall;
    [ObservableProperty] private DateTime? _startedAt;
    [ObservableProperty] private DateTime? _completedAt;

    // ── Постер ────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPoster))]
    private Bitmap? _posterBitmap;

    public bool HasPoster => PosterBitmap != null;

    // ── Сезоны / эпизоды ──────────────────────────────────────────────

    public ObservableCollection<SeasonViewModel> Seasons { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentSeasonEpisodes))]
    [NotifyPropertyChangedFor(nameof(SeasonProgressLabel))]
    [NotifyPropertyChangedFor(nameof(SeasonProgressFraction))]
    private SeasonViewModel? _selectedSeason;

    public ObservableCollection<EpisodeViewModel>? CurrentSeasonEpisodes =>
        SelectedSeason?.Episodes;

    public string SeasonProgressLabel    => SelectedSeason?.ProgressLabel    ?? string.Empty;
    public double SeasonProgressFraction => SelectedSeason?.ProgressFraction ?? 0;

    // ── Вычисляемые ───────────────────────────────────────────────────

    public string TypeLabel    => _item.TypeLabel;
    public string TypeYearLabel => _item.TypeYearLabel;

    public string StatusColor => Status switch
    {
        MediaStatus.Planned    => "#8B8B9E",
        MediaStatus.InProgress => "#3B82F6",
        MediaStatus.Completed  => "#22C55E",
        MediaStatus.Dropped    => "#F59E0B",
        _                      => "#8B8B9E"
    };

    public bool IsCompleted          => Status == MediaStatus.Completed;
    public bool ShowEpisodes         => Type is MediaType.Series or MediaType.Anime && Status != MediaStatus.Planned;
    public bool ShowChapters         => Type == MediaType.Manga && Status != MediaStatus.Planned;
    public bool ShowReadingProgress  => Type == MediaType.Book  && Status != MediaStatus.Planned;
    public bool ShowRatings          => Status == MediaStatus.Completed;

    public string ScoreVisualLabel   => Type is MediaType.Book or MediaType.Manga
        ? Loc["Media_ScoreStyle"] : Loc["Media_ScoreVisual"];

    public string OverallScoreDisplay => OverallScore > 0
        ? OverallScore.ToString("F1") : "—";

    public string StarsDisplay       => _item.StarsDisplay;

    public string TotalEpisodesLabel =>
        $"{_item.WatchedEpisodes} / {_item.TotalEpisodes} {Loc["Media_EpShort"]}";

    public string ReadingProgressLabel =>
        $"{CurrentPage} / {TotalPages} {Loc["Media_PageShort"]}";

    public double ReadingProgressFraction =>
        TotalPages > 0 ? (double)CurrentPage / TotalPages : 0;

    // ── Источники ─────────────────────────────────────────────────────

    public ObservableCollection<string> SourceOptions { get; } = new(new[]
    {
        "Netflix","Кинопоиск","Amediateka","Бумажная книга",
        "Электронная","Аудио","Кинотеатр","Другое"
    });

    // ── Конструктор ───────────────────────────────────────────────────

    public MediaItemViewModel(MediaItem item, IMediaRepository repo, IMediaPosterService posters)
    {
        _item    = item;
        _repo    = repo;
        _posters = posters;

        LoadFromModel();
    }

    private void LoadFromModel()
    {
        _suppressScoreRecalc = true;

        Title          = _item.Title;
        OriginalTitle  = _item.OriginalTitle;
        Description    = _item.Description;
        Director       = _item.Director;
        Author         = _item.Author;
        Country        = _item.Country;
        ViewingSource  = _item.Source;
        PersonalNote   = _item.PersonalNote;
        GenresDisplay  = string.Join(", ", _item.Genres);
        Year           = _item.Year;
        DurationMin    = _item.DurationMin;
        TotalPages     = _item.TotalPages;
        CurrentPage    = _item.CurrentPage;
        IsOngoing      = _item.IsOngoing;
        Status         = _item.Status;
        Type           = _item.Type;
        ScorePlot      = _item.ScorePlot;
        ScoreVisual    = _item.ScoreVisual;
        ScorePersonal  = _item.ScorePersonal;
        OverallScore   = _item.OverallScore;
        IsOverallManual = _item.IsOverallManual;
        StartedAt      = _item.StartedAt;
        CompletedAt    = _item.CompletedAt;

        _suppressScoreRecalc = false;

        Seasons.Clear();
        foreach (var s in _item.Seasons.OrderBy(x => x.SeasonNumber))
        {
            var svm = new SeasonViewModel(s, this);
            Seasons.Add(svm);
        }
        SelectedSeason = Seasons.FirstOrDefault();

        LoadPoster();
    }

    private void LoadPoster()
    {
        if (string.IsNullOrEmpty(_item.PosterPath)) { PosterBitmap = null; return; }
        var path = _posters.GetPosterPath(_item.PosterPath);
        if (!File.Exists(path)) { PosterBitmap = null; return; }
        try
        {
            using var fs = File.OpenRead(path);
            PosterBitmap = Bitmap.DecodeToWidth(fs, 200);
        }
        catch { PosterBitmap = null; }
    }

    // ── Автоматизация оценок ──────────────────────────────────────────

    partial void OnScorePlotChanged(double v)    => RecalcOverall();
    partial void OnScoreVisualChanged(double v)  => RecalcOverall();
    partial void OnScorePersonalChanged(double v) => RecalcOverall();

    private void RecalcOverall()
    {
        if (_suppressScoreRecalc || IsOverallManual) return;
        var scores = new[] { ScorePlot, ScoreVisual, ScorePersonal }.Where(s => s > 0).ToList();
        if (scores.Count == 0) return;
        OverallScore = Math.Round(scores.Average(), 1);
    }

    // ── Автоматизация статуса ─────────────────────────────────────────

    partial void OnStatusChanged(MediaStatus v)
    {
        if (v == MediaStatus.InProgress && _item.StartedAt == null)
            StartedAt = DateTime.Today;
        if (v == MediaStatus.Completed  && _item.CompletedAt == null)
            CompletedAt = DateTime.Today;
    }

    // ── Команды ───────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleOverallEdit()
    {
        IsEditingOverall = !IsEditingOverall;
        if (IsEditingOverall) IsOverallManual = true;
    }

    [RelayCommand]
    private void MarkAllEpisodes()
    {
        if (SelectedSeason == null) return;
        foreach (var ep in SelectedSeason.Episodes) ep.IsWatched = true;
        SelectedSeason.RefreshProgress();
        RefreshEpisodeProgress();
    }

    [RelayCommand]
    private void MarkEpisodesUpTo(EpisodeViewModel ep)
    {
        if (SelectedSeason == null) return;
        foreach (var e in SelectedSeason.Episodes.Where(e => e.EpisodeNumber <= ep.EpisodeNumber))
            e.IsWatched = true;
        SelectedSeason.RefreshProgress();
        RefreshEpisodeProgress();
    }

    [RelayCommand]
    private void AddSeason()
    {
        var num    = _item.Seasons.Count + 1;
        var season = new MediaSeason { SeasonNumber = num, Title = $"Сезон {num}" };
        _item.Seasons.Add(season);
        var svm = new SeasonViewModel(season, this);
        Seasons.Add(svm);
        SelectedSeason = svm;
    }

    [RelayCommand]
    private void FinishBook()
    {
        CurrentPage = TotalPages;
        Status = MediaStatus.Completed;
    }

    // ── Сохранение ────────────────────────────────────────────────────

    public void SaveToModel()
    {
        _item.Title          = Title;
        _item.OriginalTitle  = OriginalTitle;
        _item.Description    = Description;
        _item.Director       = Director;
        _item.Author         = Author;
        _item.Country        = Country;
        _item.Source         = ViewingSource;
        _item.PersonalNote   = PersonalNote;
        _item.Genres         = GenresDisplay
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct().ToList();
        _item.Year           = Year;
        _item.DurationMin    = DurationMin;
        _item.TotalPages     = TotalPages;
        _item.CurrentPage    = CurrentPage;
        _item.IsOngoing      = IsOngoing;
        _item.Status         = Status;
        _item.Type           = Type;
        _item.ScorePlot      = ScorePlot;
        _item.ScoreVisual    = ScoreVisual;
        _item.ScorePersonal  = ScorePersonal;
        _item.OverallScore   = OverallScore;
        _item.IsOverallManual = IsOverallManual;
        _item.StartedAt      = StartedAt;
        _item.CompletedAt    = CompletedAt;
        _item.Seasons        = Seasons.Select(s => s.Source).ToList();

        _repo.Upsert(_item);
    }

    // ── Уведомление из дочерних VM ────────────────────────────────────

    internal void OnEpisodeChanged()
    {
        SelectedSeason?.RefreshProgress();
        RefreshEpisodeProgress();
        CheckAutoComplete();
    }

    private void RefreshEpisodeProgress()
    {
        OnPropertyChanged(nameof(TotalEpisodesLabel));
        OnPropertyChanged(nameof(CurrentSeasonEpisodes));
    }

    private void CheckAutoComplete()
    {
        if (_item.AllEpisodesWatched && !IsOngoing && Status != MediaStatus.Completed)
            AutoCompleteRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? AutoCompleteRequested;
}
