using FocusFlowFinal.Models.Workout;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FocusFlowFinal.Services;

public class WorkoutRepository : IWorkoutRepository
{
    private const string ColPrograms = "workout_programs";
    private const string ColSessions = "workout_sessions";
    private const string ColProfile  = "workout_profile";

    private readonly ILiteCollection<WorkoutProgram>    _programs;
    private readonly ILiteCollection<WorkoutSession>    _sessions;
    private readonly ILiteCollection<WorkoutUserProfile> _profile;

    public WorkoutRepository(IDatabaseService db)
    {
        var svc = (DatabaseService)db;
        _programs = svc.GetCollection<WorkoutProgram>(ColPrograms);
        _sessions = svc.GetCollection<WorkoutSession>(ColSessions);
        _profile  = svc.GetCollection<WorkoutUserProfile>(ColProfile);
    }

    // ── Программы ──────────────────────────────────────────────────────

    public IEnumerable<WorkoutProgram> GetPrograms() =>
        _programs.FindAll().OrderByDescending(p => p.CreatedAt);

    public WorkoutProgram? GetProgram(int id) => _programs.FindById(id);

    public void UpsertProgram(WorkoutProgram program) => _programs.Upsert(program);

    public void DeleteProgram(int id)
    {
        _programs.Delete(id);
        foreach (var s in _sessions.Find(x => x.ProgramId == id).ToList())
            _sessions.Delete(s.Id);
    }

    public void SetActiveProgram(int id)
    {
        foreach (var p in _programs.FindAll())
        {
            p.IsActive = p.Id == id;
            _programs.Update(p);
        }
    }

    // ── Сессии ─────────────────────────────────────────────────────────

    public IEnumerable<WorkoutSession> GetSessions(int programId, int dayNumber) =>
        _sessions.Find(s => s.ProgramId == programId && s.DayNumber == dayNumber)
                 .OrderByDescending(s => s.StartedAt);

    public IEnumerable<WorkoutSession> GetRecentSessions(int count = 20) =>
        _sessions.FindAll().OrderByDescending(s => s.StartedAt).Take(count);

    public WorkoutSession? GetSession(int id) => _sessions.FindById(id);

    public void UpsertSession(WorkoutSession session) => _sessions.Upsert(session);

    public void DeleteSession(int id) => _sessions.Delete(id);

    public IEnumerable<PerformedSet> GetHistoryForExercise(string exerciseKey, int limit = 30)
    {
        return _sessions.FindAll()
            .OrderByDescending(s => s.StartedAt)
            .SelectMany(s => s.Exercises
                .Where(e => e.ExerciseKey == exerciseKey)
                .SelectMany(e => e.Sets))
            .Take(limit);
    }

    // ── Профиль ────────────────────────────────────────────────────────

    public WorkoutUserProfile GetProfile() =>
        _profile.FindById(1) ?? new WorkoutUserProfile();

    public void SaveProfile(WorkoutUserProfile profile)
    {
        profile.UpdatedAt = DateTime.Now;
        _profile.Upsert(profile);
    }
}
