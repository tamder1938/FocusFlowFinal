using FocusFlowFinal.Models.Workout;
using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public class ExerciseFilter
{
    public string?      Query     { get; set; }
    public MuscleGroup? Muscle    { get; set; }
    public Equipment?   Equipment { get; set; }
    public ExerciseType? Type     { get; set; }
}

public interface IExerciseRepository
{
    IEnumerable<Exercise>   GetAll();
    IEnumerable<Exercise>   GetFiltered(ExerciseFilter filter);
    Exercise?               GetByKey(string key);
    void                    Upsert(Exercise exercise);
    void                    Delete(string key);
    bool                    IsSeeded();
    void                    BulkInsert(IEnumerable<Exercise> exercises);
}
