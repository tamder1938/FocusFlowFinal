using Avalonia;
using Avalonia.Platform;
using FocusFlowFinal.Models;
using FocusFlowFinal.Models.Sound;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

public class SoundService : ISoundService, IDisposable
{
    private readonly LibVLC _vlc;
    private readonly Dictionary<string, (Media media, MediaPlayer player)> _players = new();
    private readonly Dictionary<string, float> _volumes = new();
    private float _masterVolume = 0.7f;
    private bool _mixWithPomodoro;
    private bool _globalPaused;

    // Состояние для Помодоро
    private List<string> _pomodoroSoundIds   = new();
    private Dictionary<string, float> _pomodoroVolumes = new();
    private List<string> _normalSoundIds     = new();
    private Dictionary<string, float> _normalVolumes   = new();

    private readonly string _userSoundsDir;
    private List<SoundPreset> _presets = new();

    public IReadOnlyList<SoundPreset> Presets => _presets;
    public bool  IsPlaying     => _players.Count > 0 && !_globalPaused;
    public bool  MixWithPomodoro { get => _mixWithPomodoro; set { _mixWithPomodoro = value; SaveState(); } }
    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            _masterVolume = Math.Clamp(value, 0f, 1f);
            foreach (var id in _players.Keys.ToList())
                ApplyVolume(id);
            SaveState();
        }
    }

    public SoundService()
    {
        Core.Initialize();
        _vlc = new LibVLC("--no-video");

        _userSoundsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FocusFlow", "UserSounds");
        Directory.CreateDirectory(_userSoundsDir);

        LoadPresets();
        LoadState();
    }

    // ── Пресеты ──────────────────────────────────────────────────────────

    private void LoadPresets()
    {
        try
        {
            var uri = new Uri("avares://FocusFlowFinal/Assets/Sounds/presets.json");
            using var stream = AssetLoader.Open(uri);
            _presets = JsonSerializer.Deserialize<List<SoundPreset>>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new();
        }
        catch
        {
            _presets = new();
        }
    }

    // ── Воспроизведение ──────────────────────────────────────────────────

    public bool  IsActive(string soundId) => _players.ContainsKey(soundId);
    public float GetVolume(string soundId) => _volumes.TryGetValue(soundId, out var v) ? v : 1f;

    public void Play(string soundId, string filePath)
    {
        if (_players.ContainsKey(soundId)) return;

        try
        {
            Media media;
            if (filePath.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
            {
                using var stream = AssetLoader.Open(new Uri(filePath));
                var tmp = Path.Combine(Path.GetTempPath(), $"ffound_{soundId}{Path.GetExtension(filePath)}");
                using (var fs = File.Create(tmp))
                    stream.CopyTo(fs);
                media = new Media(_vlc, tmp);
            }
            else
            {
                media = new Media(_vlc, filePath);
            }

            media.AddOption(":input-repeat=65535");

            var player = new MediaPlayer(media);
            if (!_volumes.ContainsKey(soundId))
                _volumes[soundId] = 1f;

            _players[soundId] = (media, player);
            ApplyVolume(soundId);
            player.Play();
        }
        catch { /* файл недоступен */ }
    }

    public void Stop(string soundId)
    {
        if (!_players.TryGetValue(soundId, out var entry)) return;
        entry.player.Stop();
        entry.player.Dispose();
        entry.media.Dispose();
        _players.Remove(soundId);
    }

    public void StopAll()
    {
        foreach (var id in _players.Keys.ToList())
            Stop(id);
    }

    public void SetVolume(string soundId, float volume)
    {
        _volumes[soundId] = Math.Clamp(volume, 0f, 1f);
        ApplyVolume(soundId);
        SaveState();
    }

    private void ApplyVolume(string soundId)
    {
        if (!_players.TryGetValue(soundId, out var entry)) return;
        var ind = _volumes.TryGetValue(soundId, out var v) ? v : 1f;
        entry.player.Volume = (int)(ind * _masterVolume * 100);
    }

    public void PauseAll()
    {
        _globalPaused = true;
        foreach (var (_, (_, p)) in _players) p.Pause();
    }

    public void ResumeAll()
    {
        _globalPaused = false;
        foreach (var (_, (_, p)) in _players) p.Play();
    }

    // ── Fade ─────────────────────────────────────────────────────────────

    private void FadeOutThenStop(IEnumerable<string> ids)
    {
        var list = ids.ToList();
        var cts  = new CancellationTokenSource();
        Task.Run(async () =>
        {
            const int steps = 20;
            const int ms    = 40;
            for (int i = steps; i >= 0; i--)
            {
                float fac = (float)i / steps;
                foreach (var id in list)
                {
                    if (_players.TryGetValue(id, out var e))
                    {
                        var ind = _volumes.TryGetValue(id, out var v) ? v : 1f;
                        e.player.Volume = (int)(ind * _masterVolume * fac * 100);
                    }
                }
                await Task.Delay(ms, cts.Token);
            }
            foreach (var id in list) Stop(id);
        }, cts.Token);
    }

    private void FadeIn(IEnumerable<string> ids, Dictionary<string, float> volumes, Func<string, string> resolvePath)
    {
        var list = ids.ToList();
        // Запускаем с нулевой громкости
        foreach (var id in list)
        {
            if (!_volumes.ContainsKey(id))
                _volumes[id] = volumes.TryGetValue(id, out var v2) ? v2 : 1f;
            if (!IsActive(id))
                Play(id, resolvePath(id));
            if (_players.TryGetValue(id, out var e)) e.player.Volume = 0;
        }

        Task.Run(async () =>
        {
            const int steps = 20;
            const int ms    = 40;
            for (int i = 0; i <= steps; i++)
            {
                float fac = (float)i / steps;
                foreach (var id in list)
                {
                    if (_players.TryGetValue(id, out var e))
                    {
                        var ind = _volumes.TryGetValue(id, out var v) ? v : 1f;
                        e.player.Volume = (int)(ind * _masterVolume * fac * 100);
                    }
                }
                await Task.Delay(ms);
            }
        });
    }

    // ── Помодоро-интеграция ───────────────────────────────────────────────

    public void OnPomodoroPhaseChanged(PomodoroPhase phase)
    {
        if (!_mixWithPomodoro) return;

        if (phase == PomodoroPhase.Work)
        {
            // Затухаем нормальные, включаем помодоро
            var toStop = _players.Keys.Except(_pomodoroSoundIds).ToList();
            FadeOutThenStop(toStop);
            FadeIn(_pomodoroSoundIds, _pomodoroVolumes, id => ResolvePath(id));
        }
        else if (phase == PomodoroPhase.Break)
        {
            var toStop = _players.Keys.Intersect(_pomodoroSoundIds).ToList();
            FadeOutThenStop(toStop);
        }
        else // Stopped
        {
            // Возврат к нормальному режиму
            var toStop = _players.Keys.Except(_normalSoundIds).ToList();
            FadeOutThenStop(toStop);
            FadeIn(_normalSoundIds, _normalVolumes, id => ResolvePath(id));
        }
    }

    private string ResolvePath(string soundId)
    {
        // Сначала пресеты
        var preset = _presets.FirstOrDefault(p => p.Id == soundId);
        if (preset != null)
            return $"avares://FocusFlowFinal/Assets/Sounds/{preset.FileName}";
        // Иначе — пользовательский (id начинается с "user_")
        return soundId; // полный путь хранится в soundId для пользователей
    }

    // ── Сохранение/загрузка ──────────────────────────────────────────────

    public void SaveState()
    {
        var s = AppSettings.Load();
        s.MasterSoundVolume     = _masterVolume;
        s.MixSoundsWithPomodoro = _mixWithPomodoro;
        s.ActiveSoundIds        = _players.Keys.ToList();
        s.SoundVolumes          = new(_volumes);
        s.PomodoroSoundIds      = new(_pomodoroSoundIds);
        s.PomodoroSoundVolumes  = new(_pomodoroVolumes);
        s.Save();
    }

    public void LoadState()
    {
        var s = AppSettings.Load();
        _masterVolume    = s.MasterSoundVolume;
        _mixWithPomodoro = s.MixSoundsWithPomodoro;

        foreach (var kv in s.SoundVolumes)
            _volumes[kv.Key] = kv.Value;

        _pomodoroSoundIds   = new(s.PomodoroSoundIds);
        _pomodoroVolumes    = new(s.PomodoroSoundVolumes);
        _normalSoundIds     = new(s.ActiveSoundIds);
        _normalVolumes      = new(s.SoundVolumes);

        // Не стартуем звуки автоматически при загрузке —
        // это делает ViewModel после инициализации UI
    }

    public void Dispose()
    {
        StopAll();
        _vlc.Dispose();
    }
}
