using LiteDB;
using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Models.Workout;

public class Exercise
{
    [BsonId]
    public string Key { get; set; } = string.Empty;

    public string           Name             { get; set; } = string.Empty;
    public List<MuscleGroup> PrimaryMuscles  { get; set; } = new();
    public List<MuscleGroup> SecondaryMuscles { get; set; } = new();
    public List<Equipment>  Equipment        { get; set; } = new();
    public ExerciseType     Type             { get; set; } = ExerciseType.Strength;
    public ExerciseMetric   Metric           { get; set; } = ExerciseMetric.WeightReps;
    public string           Description      { get; set; } = string.Empty;
    public string           ImageEmoji       { get; set; } = "💪";
    public string           ImagePath        { get; set; } = string.Empty;
    public bool             IsBuiltin        { get; set; } = true;
    public DateTime         CreatedAt        { get; set; } = DateTime.Now;

    [BsonIgnore]
    public string PrimaryMusclesLabel =>
        PrimaryMuscles.Count == 0
            ? string.Empty
            : string.Join(", ", PrimaryMuscles.ConvertAll(MuscleGroupLabels.Get));

    [BsonIgnore]
    public string EquipmentLabel =>
        Equipment.Count == 0
            ? string.Empty
            : string.Join(", ", Equipment.ConvertAll(EquipmentLabels.Get));
}

public static class MuscleGroupLabels
{
    public static string Get(MuscleGroup g) => g switch
    {
        MuscleGroup.Chest      => "Грудь",
        MuscleGroup.Back       => "Спина",
        MuscleGroup.Legs       => "Ноги",
        MuscleGroup.Shoulders  => "Плечи",
        MuscleGroup.Biceps     => "Бицепс",
        MuscleGroup.Triceps    => "Трицепс",
        MuscleGroup.Core       => "Кор",
        MuscleGroup.Cardio     => "Кардио",
        MuscleGroup.FullBody   => "Всё тело",
        _                      => string.Empty
    };
}

public static class EquipmentLabels
{
    public static string Get(Equipment e) => e switch
    {
        Equipment.Barbell       => "Штанга",
        Equipment.Dumbbell      => "Гантели",
        Equipment.Machine       => "Тренажёр",
        Equipment.Cable         => "Кроссовер",
        Equipment.Bodyweight    => "Своё тело",
        Equipment.Kettlebell    => "Гиря",
        Equipment.CardioMachine => "Кардио-тренажёр",
        Equipment.Other         => "Другое",
        _                       => string.Empty
    };
}
