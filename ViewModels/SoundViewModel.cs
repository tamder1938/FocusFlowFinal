using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Sound;
using FocusFlowFinal.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

// ── Чип одного звука в сетке ──────────────────────────────────────────────

public partial class SoundChipItem : ObservableObject
{
    public string  SoundId   { get; init; } = string.Empty;
    public string  Icon      { get; init; } = "🎵";
    public string  Color     { get; init; } = "#5C8FD9";
    public string  Label     { get; init; } = string.Empty;
    public string  FilePath  { get; init; } = string.Empty;
    public bool    IsPreset  { get; init; } = true;
    public int     UserSoundId { get; init; }

    [ObservableProperty] private bool  _isActive;
    [ObservableProperty] private bool  _isAvailable = true;
    [ObservableProperty] private float _volume = 1f;

    public double AvailableOpacity => IsAvailable ? 1.0 : 0.4;

    partial void OnIsAvailableChanged(bool v) => OnPropertyChanged(nameof(AvailableOpacity));

    public IBrush ChipBackground => IsActive
        ? new SolidColorBrush(Avalonia.Media.Color.Parse(Color))
        : new SolidColorBrush(Avalonia.Media.Color.FromArgb(30, 120, 120, 140));

    public IBrush IconForeground => IsActive
        ? Brushes.White
        : new SolidColorBrush(Avalonia.Media.Color.FromRgb(140, 140, 160));

    public double PulseOpacity => IsActive ? 0.25 : 0;

    partial void OnIsActiveChanged(bool v)
    {
        OnPropertyChanged(nameof(ChipBackground));
        OnPropertyChanged(nameof(IconForeground));
        OnPropertyChanged(nameof(PulseOpacity));
    }
}

// ── ViewModel виджета ─────────────────────────────────────────────────────

public partial class SoundViewModel : ObservableObject
{
    private readonly ISoundService     _svc;
    private readonly ISoundRepository  _repo;
    private readonly string            _userSoundsDir;

    public LocalizationService Loc => LocalizationService.Instance;

    public ObservableCollection<SoundChipItem> Chips       { get; } = new();
    public ObservableCollection<SoundChipItem> ActiveChips => new(Chips.Where(c => c.IsActive));
    public bool HasActiveChips => Chips.Any(c => c.IsActive);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseIcon))]
    private bool _isPlaying;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MasterVolumeDisplay))]
    private float _masterVolume = 0.7f;

    [ObservableProperty] private bool  _mixWithPomodoro;
    [ObservableProperty] private bool  _showIndividualVolumes;

    public string PlayPauseIcon     => IsPlaying ? "⏸" : "▶";
    public string MasterVolumeDisplay => $"{(int)(_masterVolume * 100)}%";

    private readonly DispatcherTimer _pulseTimer;
    [ObservableProperty] private double _pulseScale = 1.0;

    // Иконки для выбора при добавлении своего звука
    public string[] IconOptions { get; } =
        { "🎵", "🎶", "🌀", "💧", "🌿", "🍃", "🌬", "🔔", "🎸", "🎹", "🥁", "🎻" };

    public SoundViewModel(ISoundService svc, ISoundRepository repo)
    {
        _svc  = svc;
        _repo = repo;
        _userSoundsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FocusFlow", "UserSounds");
        Directory.CreateDirectory(_userSoundsDir);

        _masterVolume    = svc.MasterVolume;
        _mixWithPomodoro = svc.MixWithPomodoro;

        BuildChips();

        _pulseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        _pulseTimer.Tick += (_, _) =>
        {
            PulseScale = PulseScale > 1.05 ? 1.0 : 1.12;
        };
    }

    // ── Построение сетки чипов ────────────────────────────────────────────

    private void BuildChips()
    {
        Chips.Clear();

        // Встроенные пресеты
        foreach (var p in _svc.Presets)
        {
            var filePath = $"avares://FocusFlowFinal/Assets/Sounds/{p.FileName}";
            bool available = AssetExists(filePath);
            Chips.Add(new SoundChipItem
            {
                SoundId     = p.Id,
                Icon        = p.Icon,
                Color       = p.Color,
                Label       = Loc[p.NameKey],
                FilePath    = filePath,
                IsPreset    = true,
                IsActive    = _svc.IsActive(p.Id),
                IsAvailable = available,
                Volume      = _svc.GetVolume(p.Id)
            });
        }

        // Пользовательские
        foreach (var u in _repo.GetAll().OrderBy(x => x.CreatedAt))
        {
            bool available = File.Exists(u.FilePath);
            Chips.Add(new SoundChipItem
            {
                SoundId     = $"user_{u.Id}",
                Icon        = u.Icon,
                Color       = u.Color,
                Label       = u.DisplayName,
                FilePath    = u.FilePath,
                IsPreset    = false,
                UserSoundId = u.Id,
                IsActive    = _svc.IsActive($"user_{u.Id}"),
                IsAvailable = available,
                Volume      = _svc.GetVolume($"user_{u.Id}")
            });
        }
    }

    private static bool AssetExists(string avares)
    {
        try { Avalonia.Platform.AssetLoader.Open(new Uri(avares)); return true; }
        catch { return false; }
    }

    // ── Команды чипа ─────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleSound(SoundChipItem chip)
    {
        if (!chip.IsAvailable) return;

        if (chip.IsActive)
        {
            _svc.Stop(chip.SoundId);
            chip.IsActive = false;
        }
        else
        {
            _svc.Play(chip.SoundId, chip.FilePath);
            chip.IsActive = true;
        }

        UpdatePlayingState();
        OnPropertyChanged(nameof(ActiveChips));
        _svc.SaveState();
    }

    [RelayCommand]
    private void SetChipVolume(SoundChipItem chip)
    {
        _svc.SetVolume(chip.SoundId, chip.Volume);
    }

    // ── Play / Pause всех ────────────────────────────────────────────────

    [RelayCommand]
    private void PlayPause()
    {
        if (IsPlaying)
        {
            _svc.PauseAll();
            IsPlaying = false;
            _pulseTimer.Stop();
        }
        else
        {
            _svc.ResumeAll();
            IsPlaying = true;
            if (Chips.Any(c => c.IsActive)) _pulseTimer.Start();
        }
    }

    // ── Мастер громкость ─────────────────────────────────────────────────

    partial void OnMasterVolumeChanged(float v)
    {
        _svc.MasterVolume = v;
    }

    partial void OnMixWithPomodoroChanged(bool v)
    {
        _svc.MixWithPomodoro = v;
    }

    // ── Добавить свой звук ───────────────────────────────────────────────

    [RelayCommand]
    private async Task AddUserSound()
    {
        var topLevel = GetTopLevel();
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title         = Loc["Sound_AddOwn"],
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Audio")
                {
                    Patterns = new[] { "*.mp3", "*.ogg", "*.wav", "*.flac", "*.m4a" }
                }
            }
        });

        foreach (var f in files)
        {
            var srcPath = f.TryGetLocalPath();
            if (string.IsNullOrEmpty(srcPath) || !File.Exists(srcPath)) continue;

            var ext      = Path.GetExtension(srcPath);
            var destName = $"{Path.GetFileNameWithoutExtension(srcPath)}_{Guid.NewGuid():N}{ext}";
            var destPath = Path.Combine(_userSoundsDir, destName);
            File.Copy(srcPath, destPath, overwrite: true);

            var displayName = Path.GetFileNameWithoutExtension(srcPath);
            var sound = new UserSound
            {
                DisplayName = displayName,
                FilePath    = destPath,
                Icon        = "🎵",
                Color       = "#8B8B9E"
            };
            _repo.Upsert(sound);
        }

        BuildChips();
    }

    // ── Удалить пользовательский звук ────────────────────────────────────

    [RelayCommand]
    private void DeleteUserSound(SoundChipItem chip)
    {
        if (chip.IsPreset) return;
        if (chip.IsActive) _svc.Stop(chip.SoundId);

        try { if (File.Exists(chip.FilePath)) File.Delete(chip.FilePath); }
        catch { /* файл заблокирован */ }

        _repo.Delete(chip.UserSoundId);
        BuildChips();
        OnPropertyChanged(nameof(ActiveChips));
    }

    // ── Вспомогательное ──────────────────────────────────────────────────

    private void UpdatePlayingState()
    {
        var hasActive = Chips.Any(c => c.IsActive);
        IsPlaying = hasActive && _svc.IsPlaying;
        if (hasActive)
            _pulseTimer.Start();
        else
        {
            _pulseTimer.Stop();
            PulseScale = 1.0;
        }
    }

    private static Avalonia.Controls.TopLevel? GetTopLevel()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desk)
            return desk.MainWindow;
        return null;
    }
}
