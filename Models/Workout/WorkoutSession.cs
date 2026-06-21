using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FocusFlowFinal.Models.Workout;

public class WorkoutSession
{
    [BsonId] public int    Id                 { get; set; }
    public int             ProgramId          { get; set; }
    public int             DayNumber          { get; set; }
    public string          DayName            { get; set; } = string.Empty;
    public DateTime        StartedAt          { get; set; } = DateTime.Now;
    public DateTime?       FinishedAt         { get; set; }
    public string          Notes              { get; set; } = string.Empty;
    public List<PerformedExercise> Exercises  { get; set; } = new();

    [BsonIgnore]
    public TimeSpan Duration => FinishedAt.HasValue
        ? FinishedAt.Value - StartedAt
        : TimeSpan.Zero;

    [BsonIgnore]
    public string DurationLabel =>
        Duration.TotalMinutes < 1 ? "< 1 мин"
        : Duration.TotalHours >= 1
            ? $"{(int)Duration.TotalHours} ч {Duration.Minutes} мин"
            : $"{(int)Duration.TotalMinutes} мин";

    [BsonIgnore]
    public double TotalTonnage =>
        Exercises.Sum(e => e.Sets.Sum(s => s.WeightKg * s.Reps));
}

public class PerformedExercise
{
    public string              ExerciseKey { get; set; } = string.Empty;
    public string              ExerciseName { get; set; } = string.Empty;
    public List<PerformedSet>  Sets        { get; set; } = new();
}

public class PerformedSet
{
    public int      SetNumber   { get; set; }
    public double   WeightKg    { get; set; }
    public int      Reps        { get; set; }
    public int      DurationSec { get; set; }
    public double   RPE         { get; set; }
    public bool     IsCompleted { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.Now;
}
