using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FocusFlowFinal.Models.Workout;

public class WorkoutProgram
{
    [BsonId] public int    Id        { get; set; }
    public string?         UserId    { get; set; }
    public string          Name      { get; set; } = string.Empty;
    public string          Icon      { get; set; } = "🏋️";
    public string          Color     { get; set; } = "#3B82F6";
    public string          Notes     { get; set; } = string.Empty;
    public bool            IsActive  { get; set; }
    public List<WorkoutDay> Days     { get; set; } = new();
    public DateTime        CreatedAt { get; set; } = DateTime.Now;

    [BsonIgnore]
    public int ExerciseCount => Days.Sum(d => d.PlannedExercises.Count);

    [BsonIgnore]
    public string DaysLabel => $"{Days.Count} дн. · {ExerciseCount} упр.";
}

public class WorkoutDay
{
    public int                       DayNumber         { get; set; }
    public string                    Name              { get; set; } = string.Empty;
    public List<MuscleGroup>         TargetMuscles     { get; set; } = new();
    public List<PlannedExercise>     PlannedExercises  { get; set; } = new();

    [BsonIgnore]
    public string MusclesLabel =>
        TargetMuscles.Count > 0
            ? string.Join(", ", TargetMuscles.Select(MuscleGroupLabels.Get))
            : "Все группы";

    [BsonIgnore]
    public string ExerciseCountLabel =>
        PlannedExercises.Count > 0 ? $"{PlannedExercises.Count} упр." : "Упражнения не добавлены";
}

public class PlannedExercise
{
    public string  ExerciseKey  { get; set; } = string.Empty;
    public int     Sets         { get; set; } = 3;
    public int     Reps         { get; set; } = 10;
    public int     DurationSec  { get; set; }
    public double  RPE          { get; set; }
    public string  Notes        { get; set; } = string.Empty;
    public int     Order        { get; set; }
    public int     RestSeconds  { get; set; } = 90;
}
