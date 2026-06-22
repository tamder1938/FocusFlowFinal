using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Models.Mood;
using FocusFlowFinal.Services;
using FocusFlowFinal.Views.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

// ─────────────────────────────────────────────────────────────────────────────
// Вспомогательные классы
// ─────────────────────────────────────────────────────────────────────────────

public partial class MoodLevelItem : ObservableObject
{
    public int    Level { get; }
    public string Name  { get; }
    public string Color { get; }
    public string Emoji { get; }

    [ObservableProperty] private bool _isSelected;

    public double CircleSize  => IsSelected ? 44 : 38;
    public IBrush TintBrush   => IsSelected
        ? MakeTint(Color)
        : Brushes.Transparent;

    private static IBrush MakeTint(string hex)
    {
        var c = Avalonia.Media.Color.Parse(hex);
        var tint = Avalonia.Media.Color.FromArgb(30, c.R, c.G, c.B);
        return new SolidColorBrush(tint);
    }

    partial void OnIsSelectedChanged(bool v)
    {
        OnPropertyChanged(nameof(CircleSize));
        OnPropertyChanged(nameof(TintBrush));
    }

    public MoodLevelItem(int level, string name, string color, string emoji)
    {
        Level = level; Name = name; Color = color; Emoji = emoji;
    }
}

public partial class ActivityDisplayItem : ObservableObject
{
    public MoodActivity Activity { get; }
    public string Name  => Activity.Name;
    public string Icon  => Activity.Icon;
    public bool IsCustom => Activity.IsCustom;

    [ObservableProperty] private bool _isSelected;

    partial void OnIsSelectedChanged(bool v)
    {
        OnPropertyChanged(nameof(SelectedBackground));
        OnPropertyChanged(nameof(IconForeground));
    }

    public IBrush SelectedBackground => IsSelected
        ? new SolidColorBrush(Color.Parse("#3B82F6"))
        : new SolidColorBrush(Color.Parse("#EFF6FF"));

    public IBrush IconForeground => IsSelected
        ? (IBrush)Brushes.White
        : new SolidColorBrush(Color.Parse("#3B82F6"));

    public ActivityDisplayItem(MoodActivity activity) => Activity = activity;
}

public partial class ActivityCategoryGroup : ObservableObject
{
    public string Category     { get; }
    public string CategoryIcon { get; }
    [ObservableProperty] private bool   _isExpanded = false;
    [ObservableProperty] private bool   _isAddingCustom = false;
    [ObservableProperty] private string _newCustomName = string.Empty;
    public ObservableCollection<ActivityDisplayItem> Items { get; } = new();

    public ActivityCategoryGroup(string category, string icon)
    {
        Category = category; CategoryIcon = icon;
    }
}

public class MoodListDisplayItem
{
    public MoodEntry Entry       { get; }
    public string DateLabel      { get; }
    public string TimeStr        { get; }
    public string LevelName      { get; }
    public string LevelColor     { get; }
    public string LevelEmoji     { get; }
    public List<string> Activities { get; }
    public List<string> PhotoPaths { get; }   // до 3 полных путей
    public int ExtraPhotos       { get; }
    public string ShortComment   { get; }
    public bool HasComment       { get; }

    private static readonly string[] Colors = ["#DC4F4F","#E08A3C","#8AB7D9","#A8D88A","#5FB87A"];
    private static readonly string[] Names  = ["ужасно", "плохо",  "так себе","хорошо","супер"];
    private static readonly string[] Emojis = ["😣",    "😞",    "😐",      "🙂",    "😄"];

    public MoodListDisplayItem(MoodEntry e, List<string> activityNames, IMoodPhotoService photoSvc)
    {
        Entry      = e;
        Activities = activityNames;
        LevelColor = e.Level is >= 1 and <= 5 ? Colors[e.Level - 1] : "#9CA3AF";
        LevelName  = e.Level is >= 1 and <= 5 ? Names[e.Level - 1]  : "—";
        LevelEmoji = e.Level is >= 1 and <= 5 ? Emojis[e.Level - 1] : "😐";

        // Дата
        var today = DateTime.Today;
        if (e.Date.Date == today)
            DateLabel = $"Сегодня, {e.Date:d MMMM}";
        else if (e.Date.Date == today.AddDays(-1))
            DateLabel = $"Вчера, {e.Date:d MMMM}";
        else
            DateLabel = e.Date.ToString("dddd, d MMMM");

        TimeStr = e.CreatedAt.ToString("HH:mm");

        // Комментарий
        HasComment   = !string.IsNullOrWhiteSpace(e.Comment);
        ShortComment = (e.Comment?.Length ?? 0) > 200
            ? e.Comment![..200] + "…"
            : e.Comment ?? string.Empty;

        // Фото
        var paths = e.PhotoFiles.Select(f => photoSvc.GetPhotoPath(f)).Where(File.Exists).ToList();
        PhotoPaths  = paths.Take(3).ToList();
        ExtraPhotos = Math.Max(0, paths.Count - 3);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// MoodViewModel
// ─────────────────────────────────────────────────────────────────────────────

public partial class MoodViewModel : ObservableObject
{
    private readonly IMoodRepository        _repo;
    private readonly IMoodPhotoService      _photoSvc;
    private readonly IMoodStatisticsService _stats;
    private readonly AppSettings            _settings;

    // ── Entry list ──────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<MoodListDisplayItem> _entryList = new();
    [ObservableProperty] private MoodListDisplayItem? _selectedListItem;
    [ObservableProperty] private int            _periodFilter  = 0;   // 0=Все 1=Месяц 2=Год 3=Период
    [ObservableProperty] private DateTimeOffset? _filterFrom   = null;
    [ObservableProperty] private DateTimeOffset? _filterTo     = null;
    [ObservableProperty] private string    _searchQuery  = string.Empty;

    // ── Form ────────────────────────────────────────────────────────
    [ObservableProperty] private bool     _isFormVisible = false;
    [ObservableProperty] private bool     _isReadOnly    = false;
    [ObservableProperty] private int      _editLevel     = 3;
    [ObservableProperty] private DateTimeOffset _editDate = DateTimeOffset.Now;
    [ObservableProperty] private string   _editComment   = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _editPhotoPaths = new();

    // ── Activities ──────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ActivityCategoryGroup> _activityGroups = new();

    // ── Analytics ───────────────────────────────────────────────────
    [ObservableProperty] private int _chartPeriod = 1;           // 0=Нед 1=Мес 2=Год
    [ObservableProperty] private ObservableCollection<MoodChartPoint> _chartPoints = new();
    [ObservableProperty] private List<MoodDistributionItem> _distribution = new();
    [ObservableProperty] private ObservableCollection<MoodYearDotItem> _yearDots = new();
    [ObservableProperty] private string _averageMoodStr  = "—";
    [ObservableProperty] private string _totalEntriesStr = "0";
    [ObservableProperty] private bool   _showYearGrid    = false;

    // ── Mood level buttons ──────────────────────────────────────────
    public List<MoodLevelItem> MoodLevels { get; } = new()
    {
        new(1, "ужасно",  "#DC4F4F", "😣"),
        new(2, "плохо",   "#E08A3C", "😞"),
        new(3, "так себе","#8AB7D9", "😐"),
        new(4, "хорошо",  "#A8D88A", "🙂"),
        new(5, "супер",   "#5FB87A", "😄"),
    };

    private MoodEntry? _currentEntry;
    private List<MoodActivity> _allActivities = new();

    public bool HasNoEntries => !EntryList.Any();
    public LocalizationService Loc => LocalizationService.Instance;

    // ── Constructor ─────────────────────────────────────────────────
    public MoodViewModel(IMoodRepository repo, IMoodPhotoService photoSvc, IMoodStatisticsService stats)
    {
        _repo     = repo;
        _photoSvc = photoSvc;
        _stats    = stats;
        _settings = AppSettings.Load();
        ShowYearGrid = _settings.ExtendedStatisticsEnabled;

        SelectMoodLevel(3);
        LoadActivities();
        LoadEntries();
        RefreshAnalytics();
    }

    // ── Filter reactions ─────────────────────────────────────────────

    partial void OnPeriodFilterChanged(int v)    => LoadEntries();
    partial void OnFilterFromChanged(DateTimeOffset? v) => LoadEntries();
    partial void OnFilterToChanged(DateTimeOffset? v)   => LoadEntries();
    partial void OnSearchQueryChanged(string v)   => LoadEntries();
    partial void OnChartPeriodChanged(int v)      => RefreshAnalytics();

    // ── Load entries ─────────────────────────────────────────────────

    private void LoadEntries()
    {
        var today = DateTime.Today;
        IEnumerable<MoodEntry> source = PeriodFilter switch
        {
            1 => _repo.GetEntriesForPeriod(new DateTime(today.Year, today.Month, 1), today),
            2 => _repo.GetEntriesForPeriod(new DateTime(today.Year, 1, 1), today),
            3 when FilterFrom.HasValue && FilterTo.HasValue =>
                _repo.GetEntriesForPeriod(FilterFrom.Value.LocalDateTime, FilterTo.Value.LocalDateTime),
            _ => _repo.GetAllEntries()
        };

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.ToLower();
            source = source.Where(e => e.Comment.ToLower().Contains(q));
        }

        var activityMap = _allActivities.ToDictionary(a => a.Id, a => a.Name);

        EntryList.Clear();
        foreach (var e in source)
        {
            var names = e.ActivityIds.Select(id => activityMap.TryGetValue(id, out var n) ? n : "").Where(n => n != "").ToList();
            EntryList.Add(new MoodListDisplayItem(e, names, _photoSvc));
        }
        OnPropertyChanged(nameof(HasNoEntries));
    }

    // ── Load activities ───────────────────────────────────────────────

    private void LoadActivities()
    {
        _allActivities = _repo.GetAllActivities().ToList();

        var categoryIcons = new Dictionary<string, string>
        {
            ["Сон"]         = "😴",
            ["Совместные"]  = "👥",
            ["Хобби"]       = "🎨",
            ["Еда"]         = "🍽️",
            ["Спорт"]       = "🏃",
            ["Работа"]      = "💼",
            ["Здоровье"]    = "❤️",
            ["Другое"]      = "⭐",
        };

        ActivityGroups.Clear();
        foreach (var grp in _allActivities.GroupBy(a => a.Category))
        {
            categoryIcons.TryGetValue(grp.Key, out var icon);
            var group = new ActivityCategoryGroup(grp.Key, icon ?? "•");
            foreach (var act in grp) group.Items.Add(new ActivityDisplayItem(act));
            ActivityGroups.Add(group);
        }
    }

    // ── Select mood level ─────────────────────────────────────────────

    [RelayCommand]
    private void SelectMoodLevel(int level)
    {
        EditLevel = level;
        foreach (var ml in MoodLevels)
            ml.IsSelected = ml.Level == level;
    }

    // ── Toggle activity ────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleActivity(ActivityDisplayItem item)
    {
        item.IsSelected = !item.IsSelected;
    }

    // ── New entry ─────────────────────────────────────────────────────

    [RelayCommand]
    private void NewEntry()
    {
        _currentEntry = null;
        EditDate      = DateTimeOffset.Now;
        EditComment   = string.Empty;
        EditPhotoPaths.Clear();
        SelectMoodLevel(3);
        foreach (var g in ActivityGroups)
            foreach (var it in g.Items) it.IsSelected = false;
        IsFormVisible = true;
        IsReadOnly    = false;
        SelectedListItem = null;
    }

    // ── Select entry from list ────────────────────────────────────────

    [RelayCommand]
    private void SelectEntry(MoodListDisplayItem item)
    {
        SelectedListItem = item;
        _currentEntry    = item.Entry;
        LoadEntryIntoForm(item.Entry, readOnly: true);
        IsFormVisible = true;
    }

    private void LoadEntryIntoForm(MoodEntry e, bool readOnly)
    {
        EditDate    = new DateTimeOffset(e.Date, DateTimeOffset.Now.Offset);
        EditComment = e.Comment ?? string.Empty;
        SelectMoodLevel(e.Level);

        var selectedIds = new HashSet<int>(e.ActivityIds);
        foreach (var g in ActivityGroups)
            foreach (var it in g.Items)
                it.IsSelected = selectedIds.Contains(it.Activity.Id);

        EditPhotoPaths.Clear();
        foreach (var f in e.PhotoFiles)
        {
            var path = _photoSvc.GetPhotoPath(f);
            if (File.Exists(path)) EditPhotoPaths.Add(path);
        }

        IsReadOnly = readOnly;
    }

    // ── Edit current entry ────────────────────────────────────────────

    [RelayCommand]
    private void EditEntry()
    {
        if (_currentEntry == null) return;
        IsReadOnly = false;
    }

    // ── Save entry ────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveEntry()
    {
        var entry = _currentEntry ?? new MoodEntry();
        entry.Date    = EditDate.Date;
        entry.Level   = EditLevel;
        entry.Comment = EditComment.Trim();

        entry.ActivityIds = ActivityGroups
            .SelectMany(g => g.Items)
            .Where(i => i.IsSelected)
            .Select(i => i.Activity.Id)
            .ToList();

        // Обрабатываем фото
        var existingFiles = _currentEntry?.PhotoFiles ?? new List<string>();
        var newPhotoPaths = EditPhotoPaths.ToList();
        var photoDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FocusFlow", "MoodPhotos");
        var existingFullPaths = existingFiles.Select(f => Path.Combine(photoDir, f)).ToHashSet();

        var resultFiles = new List<string>(existingFiles);
        foreach (var path in newPhotoPaths)
        {
            if (!existingFullPaths.Contains(path))
                resultFiles.Add(_photoSvc.CopyPhoto(path));
        }
        // Удалить файлы которые были убраны
        var removedFiles = existingFiles.Where(f =>
            !newPhotoPaths.Contains(Path.Combine(photoDir, f))).ToList();
        _photoSvc.DeletePhotos(removedFiles);
        foreach (var f in removedFiles) resultFiles.Remove(f);

        entry.PhotoFiles = resultFiles;
        entry.Id = _repo.UpsertEntry(entry);
        _currentEntry = entry;

        LoadEntries();
        RefreshAnalytics();
        IsReadOnly = true;

        await Task.CompletedTask;
    }

    // ── Cancel edit ────────────────────────────────────────────────────

    [RelayCommand]
    private void CancelEdit()
    {
        if (_currentEntry != null)
        {
            LoadEntryIntoForm(_currentEntry, readOnly: true);
        }
        else
        {
            IsFormVisible = false;
        }
    }

    // ── Delete entry ────────────────────────────────────────────────────

    [RelayCommand]
    private void DeleteEntry()
    {
        if (_currentEntry == null) return;
        _photoSvc.DeletePhotos(_currentEntry.PhotoFiles);
        _repo.DeleteEntry(_currentEntry.Id);
        _currentEntry    = null;
        SelectedListItem = null;
        IsFormVisible    = false;
        LoadEntries();
        RefreshAnalytics();
    }

    // ── Photos ──────────────────────────────────────────────────────────

    public void AddPhotoPaths(IEnumerable<string> paths)
    {
        foreach (var p in paths)
            if (EditPhotoPaths.Count < 5 && !EditPhotoPaths.Contains(p))
                EditPhotoPaths.Add(p);
    }

    [RelayCommand]
    private void RemovePhoto(string path) => EditPhotoPaths.Remove(path);

    // ── Custom activity ────────────────────────────────────────────────

    [RelayCommand]
    private void ShowAddCustomActivity(ActivityCategoryGroup group)
    {
        group.IsAddingCustom = true;
        group.NewCustomName  = string.Empty;
        group.IsExpanded     = true;
    }

    [RelayCommand]
    private void ConfirmAddCustomActivity(ActivityCategoryGroup group)
    {
        var name = group.NewCustomName.Trim();
        if (string.IsNullOrEmpty(name)) { group.IsAddingCustom = false; return; }

        var act = new MoodActivity { Category = group.Category, Name = name, Icon = "⭐", IsCustom = true };
        act.Id = _repo.UpsertActivity(act);
        _allActivities.Add(act);

        var item = new ActivityDisplayItem(act) { IsSelected = true };
        group.Items.Add(item);
        group.NewCustomName  = string.Empty;
        group.IsAddingCustom = false;
    }

    // ── Analytics ──────────────────────────────────────────────────────

    [RelayCommand]
    private void SetChartPeriod(int period) => ChartPeriod = period;

    private void RefreshAnalytics()
    {
        var today = DateTime.Today;
        DateTime from = ChartPeriod switch
        {
            0 => today.AddDays(-6),
            2 => new DateTime(today.Year, 1, 1),
            _ => new DateTime(today.Year, today.Month, 1)
        };

        var entries = _repo.GetEntriesForPeriod(from, today).ToList();

        // Линейный график
        ChartPoints.Clear();
        foreach (var e in entries.OrderBy(e => e.Date))
            ChartPoints.Add(new MoodChartPoint(e.Date, e.Level));

        // Распределение
        Distribution = _stats.GetDistribution(entries);

        // Среднее + кол-во
        double avg = _stats.GetAverageMood(entries);
        AverageMoodStr  = avg > 0 ? avg.ToString("F1") : "—";
        TotalEntriesStr = entries.Count.ToString();

        // Год в точках
        if (ShowYearGrid)
        {
            var allYear = _repo.GetEntriesForPeriod(new DateTime(today.Year, 1, 1), today).ToList();
            var dots = _stats.GetYearGrid(today.Year, allYear);
            YearDots.Clear();
            foreach (var d in dots) YearDots.Add(d);
        }
    }
}
