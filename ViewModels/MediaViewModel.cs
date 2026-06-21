using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Media;
using FocusFlowFinal.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class MediaViewModel : ObservableObject
{
    private readonly IMediaRepository    _repo;
    private readonly IMediaPosterService _posters;

    public LocalizationService Loc => LocalizationService.Instance;

    // ── Список ────────────────────────────────────────────────────────

    public ObservableCollection<MediaItem> Items { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedItem))]
    [NotifyPropertyChangedFor(nameof(SelectedItemVm))]
    private MediaItem? _selectedItem;

    public bool HasSelectedItem => SelectedItem != null;

    // ── Фильтры ───────────────────────────────────────────────────────

    [ObservableProperty] private int    _typeTabIndex = 0;  // 0=All,1=Movie,2=Series,3=Anime,4=Book,5=Manga
    [ObservableProperty] private string _searchQuery  = string.Empty;
    [ObservableProperty] private int    _sortIndex    = 0;

    // Статусные пилюли (множественный выбор)
    public ObservableCollection<StatusPill> StatusPills { get; } = new()
    {
        new StatusPill("Все",          null),
        new StatusPill("Запланировано",MediaStatus.Planned),
        new StatusPill("Смотрю/Читаю", MediaStatus.InProgress),
        new StatusPill("Завершено",    MediaStatus.Completed),
        new StatusPill("Брошено",      MediaStatus.Dropped),
    };

    // ── Детальный VM ──────────────────────────────────────────────────

    private MediaItemViewModel? _selectedItemVm;
    public  MediaItemViewModel? SelectedItemVm
    {
        get => _selectedItemVm;
        private set
        {
            if (_selectedItemVm != null)
                _selectedItemVm.AutoCompleteRequested -= OnAutoCompleteRequested;
            SetProperty(ref _selectedItemVm, value);
            if (_selectedItemVm != null)
                _selectedItemVm.AutoCompleteRequested += OnAutoCompleteRequested;
        }
    }

    // ── Статистика (правая колонка) ───────────────────────────────────

    [ObservableProperty] private MediaStatistics? _statistics;

    public MediaViewModel(IMediaRepository repo, IMediaPosterService posters)
    {
        _repo    = repo;
        _posters = posters;

        foreach (var pill in StatusPills) pill.SelectionChanged += (_, _) => ApplyFilters();

        LoadItems();
        RefreshStatistics();
    }

    // ── Загрузка / фильтрация ─────────────────────────────────────────

    private void LoadItems()
    {
        var all = _repo.GetAll().ToList();
        ApplyFilters(all);
    }

    private void ApplyFilters(System.Collections.Generic.List<MediaItem>? source = null)
    {
        var all = source ?? _repo.GetAll().ToList();

        // Фильтр типа
        MediaType? typeFilter = TypeTabIndex switch
        {
            1 => MediaType.Movie,
            2 => MediaType.Series,
            3 => MediaType.Anime,
            4 => MediaType.Book,
            5 => MediaType.Manga,
            _ => null
        };
        if (typeFilter.HasValue)
            all = all.Where(x => x.Type == typeFilter.Value).ToList();

        // Статусные пилюли
        var activeStatuses = StatusPills
            .Where(p => p.IsSelected && p.Status.HasValue)
            .Select(p => p.Status!.Value)
            .ToList();
        if (activeStatuses.Count > 0)
            all = all.Where(x => activeStatuses.Contains(x.Status)).ToList();

        // Поиск
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.ToLower();
            all = all.Where(x => x.Title.ToLower().Contains(q) || x.OriginalTitle.ToLower().Contains(q)).ToList();
        }

        // Сортировка
        all = SortIndex switch
        {
            1 => all.OrderByDescending(x => x.CompletedAt).ToList(),
            2 => all.OrderByDescending(x => x.OverallScore).ToList(),
            3 => all.OrderBy(x => x.Title).ToList(),
            4 => all.OrderByDescending(x => x.Year).ToList(),
            _ => all.OrderByDescending(x => x.CreatedAt).ToList()
        };

        Items.Clear();
        foreach (var it in all) Items.Add(it);
    }

    [RelayCommand]
    private void SetTypeTab(string tab)
    {
        if (int.TryParse(tab, out int idx))
            TypeTabIndex = idx;
    }

    // Реакции на изменение фильтров
    partial void OnTypeTabIndexChanged(int v) => ApplyFilters();
    partial void OnSearchQueryChanged(string v) => ApplyFilters();
    partial void OnSortIndexChanged(int v) => ApplyFilters();

    partial void OnSelectedItemChanged(MediaItem? v)
    {
        if (v == null) { SelectedItemVm = null; return; }
        SelectedItemVm = new MediaItemViewModel(v, _repo, _posters);
    }

    // ── Автозавершение ────────────────────────────────────────────────

    private async void OnAutoCompleteRequested(object? sender, EventArgs e)
    {
        if (sender is not MediaItemViewModel vm) return;
        var confirm = await ShowAutoCompleteConfirmAsync();
        if (!confirm) return;
        vm.Status = MediaStatus.Completed;
        vm.SaveToModel();
        RefreshStatistics();
    }

    private Task<bool> ShowAutoCompleteConfirmAsync()
        => Task.FromResult(true); // Диалог показывается в code-behind через событие

    // ── Команды ───────────────────────────────────────────────────────

    [RelayCommand]
    private async Task AddMedia()
    {
        var vm  = new AddMediaViewModel(_repo, _posters);
        var dlg = new Views.AddMediaDialog { DataContext = vm };
        vm.CloseRequested += (_, _) => dlg.Close();

        var owner = GetMainWindow();
        if (owner != null) await dlg.ShowDialog(owner);
        else dlg.Show();

        if (vm.Saved)
        {
            ApplyFilters();
            RefreshStatistics();
            if (vm.Result != null)
                SelectedItem = Items.FirstOrDefault(x => x.Id == vm.Result.Id);
        }
    }

    [RelayCommand]
    private async Task EditMedia()
    {
        if (SelectedItemVm == null) return;
        SelectedItemVm.SaveToModel();
        ApplyFilters();
        RefreshStatistics();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void SaveCurrent()
    {
        SelectedItemVm?.SaveToModel();
        RefreshStatistics();

        // Обновляем элемент в списке
        if (SelectedItem != null)
        {
            var idx = Items.IndexOf(SelectedItem);
            if (idx >= 0) Items[idx] = SelectedItem;
        }
        ApplyFilters();
    }

    [RelayCommand]
    private void DeleteMedia()
    {
        if (SelectedItem == null) return;
        _repo.Delete(SelectedItem.Id);
        _posters.DeletePoster(SelectedItem.PosterPath);
        SelectedItem = null;
        ApplyFilters();
        RefreshStatistics();
    }

    private void RefreshStatistics()
    {
        Statistics = _repo.GetStatistics();
    }

    private static Avalonia.Controls.Window? GetMainWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desk)
            return desk.MainWindow;
        return null;
    }
}

// ── Пилюля статуса ──────────────────────────────────────────────────────

public partial class StatusPill : ObservableObject
{
    public string        Label  { get; }
    public MediaStatus?  Status { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Background))]
    [NotifyPropertyChangedFor(nameof(Foreground))]
    private bool _isSelected;

    public string Background => IsSelected ? "#3B82F6" : "Transparent";
    public string Foreground => IsSelected ? "#FFFFFF"  : "#888888";

    public event EventHandler? SelectionChanged;

    public StatusPill(string label, MediaStatus? status)
    {
        Label  = label;
        Status = status;
    }

    partial void OnIsSelectedChanged(bool v) => SelectionChanged?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void Toggle() => IsSelected = !IsSelected;
}
