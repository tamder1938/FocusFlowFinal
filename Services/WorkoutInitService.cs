using Avalonia.Platform;
using FocusFlowFinal.Models.Workout;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

public class WorkoutInitService : IWorkoutInitService
{
    private readonly IExerciseRepository _exercises;

    public WorkoutInitService(IExerciseRepository exercises)
    {
        _exercises = exercises;
    }

    public async Task EnsureSeededAsync()
    {
        if (_exercises.IsSeeded()) return;

        await Task.Run(() =>
        {
            try
            {
                var uri    = new Uri("avares://FocusFlowFinal/Assets/Data/exercises.json");
                using var stream = AssetLoader.Open(uri);

                var opts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                var list = JsonSerializer.Deserialize<List<ExerciseDto>>(stream, opts);
                if (list == null) return;

                var exercises = list.ConvertAll(dto => new Exercise
                {
                    Key              = dto.Key,
                    Name             = dto.Name,
                    PrimaryMuscles   = dto.PrimaryMuscles,
                    SecondaryMuscles = dto.SecondaryMuscles,
                    Equipment        = dto.Equipment,
                    Type             = dto.Type,
                    Metric           = dto.Type switch
                    {
                        ExerciseType.Cardio     => ExerciseMetric.TimeOnly,
                        ExerciseType.Stretching => ExerciseMetric.TimeOnly,
                        _                       => ExerciseMetric.WeightReps
                    },
                    Description      = dto.Description,
                    ImageEmoji       = dto.ImageEmoji,
                    IsBuiltin        = true,
                    CreatedAt        = DateTime.Now
                });

                _exercises.BulkInsert(exercises);
            }
            catch { /* не блокируем запуск при ошибке инициализации */ }
        });
    }

    private class ExerciseDto
    {
        public string           Key              { get; set; } = string.Empty;
        public string           Name             { get; set; } = string.Empty;
        public List<MuscleGroup> PrimaryMuscles  { get; set; } = new();
        public List<MuscleGroup> SecondaryMuscles { get; set; } = new();
        public List<Equipment>  Equipment        { get; set; } = new();
        public ExerciseType     Type             { get; set; } = ExerciseType.Strength;
        public string           Description      { get; set; } = string.Empty;
        public string           ImageEmoji       { get; set; } = "💪";
    }
}
