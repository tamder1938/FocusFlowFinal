using FocusFlowFinal.Models.Workout;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FocusFlowFinal.Services;

public class ExerciseRepository : IExerciseRepository
{
    private const string Col = "exercises";
    private readonly ILiteCollection<Exercise> _col;

    public ExerciseRepository(IDatabaseService db)
    {
        _col = ((DatabaseService)db).GetCollection<Exercise>(Col);
        _col.EnsureIndex(x => x.Name);
    }

    public IEnumerable<Exercise> GetAll() =>
        _col.FindAll().OrderBy(e => e.Name);

    public IEnumerable<Exercise> GetFiltered(ExerciseFilter f)
    {
        var all = _col.FindAll().AsEnumerable();

        if (!string.IsNullOrWhiteSpace(f.Query))
        {
            var q = f.Query.ToLower();
            all = all.Where(e => e.Name.ToLower().Contains(q));
        }

        if (f.Muscle.HasValue)
            all = all.Where(e =>
                e.PrimaryMuscles.Contains(f.Muscle.Value) ||
                e.SecondaryMuscles.Contains(f.Muscle.Value));

        if (f.Equipment.HasValue)
            all = all.Where(e => e.Equipment.Contains(f.Equipment.Value));

        if (f.Type.HasValue)
            all = all.Where(e => e.Type == f.Type.Value);

        return all.OrderBy(e => e.Name);
    }

    public Exercise? GetByKey(string key) => _col.FindById(key);

    public void Upsert(Exercise exercise) => _col.Upsert(exercise);

    public void Delete(string key) => _col.Delete(key);

    public bool IsSeeded() => _col.Count() > 0;

    public void BulkInsert(IEnumerable<Exercise> exercises)
    {
        var list = exercises.ToList();
        _col.InsertBulk(list);
    }
}
