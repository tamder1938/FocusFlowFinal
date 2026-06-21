using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FocusFlowFinal.Models.Workout;
using FocusFlowFinal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public class KeyLiftItem
{
    public string ExerciseKey   { get; init; } = "";
    public string ExerciseName  { get; init; } = "";
    public string Emoji         { get; init; } = "";
    public double OneRepMax     { get; init; }
    public string OneRmLabel    { get; init; } = "—";
    public string LevelLabel    { get; init; } = "Нет данных";
    public IBrush LevelBrush    { get; init; } = new SolidColorBrush(Color.Parse("#9CA3AF"));
    public double LevelProgress { get; init; }
    public bool   HasData       { get; init; }
    public string NextLevelHint { get; init; } = "";
}

public partial class WorkoutStrengthViewModel : ObservableObject
{
    private readonly IWorkoutRepository  _workouts;
    private readonly IExerciseRepository _exercises;
    private bool _refreshing;
    private List<WorkoutSession>? _cachedSessions;
    private Dictionary<string, MuscleGroup>? _muscleMap;

    private static readonly (string Key, string Name, string Emoji, double[] T)[] KeyLiftDefs =
    {
        ("bench_press",    "Жим лёжа",      "🏋️", new[] { 0.50, 0.75, 1.00, 1.25 }),
        ("squat",          "Приседания",      "🦵", new[] { 0.75, 1.25, 1.50, 2.00 }),
        ("deadlift",       "Становая тяга",  "⚡",  new[] { 1.00, 1.50, 2.00, 2.50 }),
        ("overhead_press", "Жим стоя",       "🙌", new[] { 0.35, 0.55, 0.75, 1.00 }),
    };

    private static readonly string[] LevelNames  = { "Начинающий", "Новичок", "Средний", "Продвинутый", "Элита" };
    private static readonly string[] LevelHexes  = { "#9CA3AF", "#3B82F6", "#10B981", "#F59E0B", "#8B5CF6" };

    // ── Наблюдаемые свойства ───────────────────────────────────────────
    [ObservableProperty] private decimal _bodyWeight = 80m;
    [ObservableProperty] private bool    _isEmpty    = true;
    [ObservableProperty] private string  _weekLabel  = "";

    // Цвета мышечного силуэта (IBrush — нет конвертера в AXAML)
    [ObservableProperty] private IBrush _chestBrush     = GrayBrush();
    [ObservableProperty] private IBrush _backBrush      = GrayBrush();
    [ObservableProperty] private IBrush _legsBrush      = GrayBrush();
    [ObservableProperty] private IBrush _shouldersBrush = GrayBrush();
    [ObservableProperty] private IBrush _bicepsBrush    = GrayBrush();
    [ObservableProperty] private IBrush _tricepsBrush   = GrayBrush();
    [ObservableProperty] private IBrush _coreBrush      = GrayBrush();

    // Подсказки по объёму тренировок
    [ObservableProperty] private string _chestHint     = "";
    [ObservableProperty] private string _backHint      = "";
    [ObservableProperty] private string _legsHint      = "";
    [ObservableProperty] private string _shouldersHint = "";
    [ObservableProperty] private string _bicepsHint    = "";
    [ObservableProperty] private string _tricepsHint   = "";
    [ObservableProperty] private string _coreHint      = "";

    public ObservableCollection<KeyLiftItem> KeyLifts { get; } = new();

    public WorkoutStrengthViewModel(IWorkoutRepository workouts, IExerciseRepository exercises)
    {
        _workouts  = workouts;
        _exercises = exercises;
        Refresh();
    }

    public void Refresh()
    {
        _refreshing = true;
        _cachedSessions = _workouts.GetRecentSessions(300).ToList();
        IsEmpty = !_cachedSessions.Any();

        _muscleMap = _exercises.GetAll()
            .ToDictionary(e => e.Key,
                          e => e.PrimaryMuscles.Count > 0 ? e.PrimaryMuscles[0] : MuscleGroup.FullBody);

        var profile = _workouts.GetProfile();
        BodyWeight = (decimal)profile.BodyWeight;
        _refreshing = false;

        BuildMuscleColors(_cachedSessions);
        BuildKeyLifts(_cachedSessions);
        WeekLabel = $"Активность за 7 дней · {DateTime.Now:d MMM}";
    }

    partial void OnBodyWeightChanged(decimal value)
    {
        if (_refreshing || _cachedSessions == null || value <= 0) return;
        _workouts.SaveProfile(new WorkoutUserProfile { BodyWeight = (double)value });
        BuildKeyLifts(_cachedSessions);
    }

    // ── Силуэт ─────────────────────────────────────────────────────────

    private void BuildMuscleColors(List<WorkoutSession> sessions)
    {
        var weekAgo = DateTime.Now.AddDays(-7);
        var recent  = sessions.Where(s => s.StartedAt >= weekAgo).ToList();

        SetMuscle(MuscleGroup.Chest,     recent, out var cb, out var ch);
        SetMuscle(MuscleGroup.Back,      recent, out var bb, out var bkh);
        SetMuscle(MuscleGroup.Legs,      recent, out var lb, out var lh);
        SetMuscle(MuscleGroup.Shoulders, recent, out var sb, out var sh);
        SetMuscle(MuscleGroup.Biceps,    recent, out var bib, out var bih);
        SetMuscle(MuscleGroup.Triceps,   recent, out var trb, out var trh);
        SetMuscle(MuscleGroup.Core,      recent, out var crb, out var crh);

        ChestBrush     = cb; ChestHint     = ch;
        BackBrush      = bb; BackHint      = bkh;
        LegsBrush      = lb; LegsHint      = lh;
        ShouldersBrush = sb; ShouldersHint = sh;
        BicepsBrush    = bib; BicepsHint   = bih;
        TricepsBrush   = trb; TricepsHint  = trh;
        CoreBrush      = crb; CoreHint     = crh;
    }

    private void SetMuscle(MuscleGroup group, List<WorkoutSession> recent,
                           out IBrush brush, out string hint)
    {
        int sets = CountSets(recent, group);
        brush = VolumeBrush(sets);
        hint  = VolumeHint(MuscleGroupLabels.Get(group), sets);
    }

    private int CountSets(List<WorkoutSession> sessions, MuscleGroup group) =>
        sessions.SelectMany(s => s.Exercises)
                .Where(e => _muscleMap != null &&
                            _muscleMap.TryGetValue(e.ExerciseKey, out var g) && g == group)
                .Sum(e => e.Sets.Count(s => s.IsCompleted));

    private static IBrush VolumeBrush(int sets) => sets switch
    {
        0     => new SolidColorBrush(Color.Parse("#374151")),
        <= 5  => new SolidColorBrush(Color.Parse("#1E40AF")),
        <= 15 => new SolidColorBrush(Color.Parse("#3B82F6")),
        _     => new SolidColorBrush(Color.Parse("#10B981"))
    };

    private static string VolumeHint(string name, int sets) => sets switch
    {
        0     => $"{name}: нет тренировок",
        1     => $"{name}: 1 подход",
        _     => $"{name}: {sets} подходов"
    };

    private static IBrush GrayBrush() => new SolidColorBrush(Color.Parse("#374151"));

    // ── Ключевые подъёмы / 1RM ─────────────────────────────────────────

    private void BuildKeyLifts(List<WorkoutSession> sessions)
    {
        KeyLifts.Clear();
        double bw = (double)BodyWeight;

        foreach (var (key, name, emoji, thresholds) in KeyLiftDefs)
        {
            double bestOrm = 0;
            foreach (var session in sessions)
            foreach (var ex in session.Exercises.Where(e => e.ExerciseKey == key))
            foreach (var set in ex.Sets.Where(s => s.IsCompleted && s.WeightKg > 0 && s.Reps > 0))
            {
                double orm = set.Reps == 1
                    ? set.WeightKg
                    : set.WeightKg * (1.0 + set.Reps / 30.0);
                if (orm > bestOrm) bestOrm = orm;
            }

            if (bestOrm <= 0)
            {
                KeyLifts.Add(new KeyLiftItem
                {
                    ExerciseKey = key, ExerciseName = name, Emoji = emoji,
                    HasData = false, LevelLabel = "Нет данных",
                    LevelBrush = new SolidColorBrush(Color.Parse("#6B7280")),
                    NextLevelHint = $"Выполните {name.ToLower()}"
                });
                continue;
            }

            var (label, hex, progress) = GetLevel(bestOrm, bw, thresholds);
            int lvlIdx = GetLevelIndex(bw > 0 ? bestOrm / bw : 0, thresholds);

            string nextHint;
            if (lvlIdx < thresholds.Length)
            {
                double needed = thresholds[lvlIdx] * bw;
                string nextName = LevelNames[lvlIdx + 1 < LevelNames.Length ? lvlIdx + 1 : lvlIdx];
                nextHint = $"До «{nextName}»: {needed:0.#} кг";
            }
            else
            {
                nextHint = "🏆 Максимальный уровень!";
            }

            KeyLifts.Add(new KeyLiftItem
            {
                ExerciseKey   = key,
                ExerciseName  = name,
                Emoji         = emoji,
                OneRepMax     = bestOrm,
                OneRmLabel    = $"~{bestOrm:0.#} кг",
                LevelLabel    = label,
                LevelBrush    = new SolidColorBrush(Color.Parse(hex)),
                LevelProgress = progress,
                HasData       = true,
                NextLevelHint = nextHint
            });
        }
    }

    private static (string label, string hex, double progress) GetLevel(double orm, double bw, double[] t)
    {
        double ratio = bw > 0 ? orm / bw : 0;
        int idx      = GetLevelIndex(ratio, t);   // 0..t.Length

        double from  = idx > 0        ? t[idx - 1]   : 0;
        double to    = idx < t.Length ? t[idx]        : t[^1] * 1.5;
        double within = to > from ? Math.Clamp((ratio - from) / (to - from), 0, 1) : 1.0;
        double prog  = idx >= t.Length ? 1.0 : (idx + within) / (t.Length + 1.0);

        return (LevelNames[idx], LevelHexes[idx], Math.Clamp(prog, 0, 1));
    }

    private static int GetLevelIndex(double ratio, double[] t)
    {
        for (int i = t.Length - 1; i >= 0; i--)
            if (ratio >= t[i]) return i + 1;
        return 0;
    }
}
