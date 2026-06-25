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
    private readonly ICurrentWorkspace _workspace;

    public WorkoutRepository(IDatabaseService db, ICurrentWorkspace workspace)
    {
        var svc = (DatabaseService)db;
        _programs  = svc.GetCollection<WorkoutProgram>(ColPrograms);
        _sessions  = svc.GetCollection<WorkoutSession>(ColSessions);
        _profile   = svc.GetCollection<WorkoutUserProfile>(ColProfile);
        _workspace = workspace;

        // Миграция: проставить UserId = "local" существующим записям без него
        const string local = CurrentWorkspaceService.LocalOwner;
        foreach (var p in _programs.FindAll().Where(p => p.UserId == null).ToList())
        { p.UserId = local; _programs.Update(p); }
        foreach (var s in _sessions.FindAll().Where(s => s.UserId == null).ToList())
        { s.UserId = local; _sessions.Update(s); }
    }

    // ── Программы ──────────────────────────────────────────────────────

    public IEnumerable<WorkoutProgram> GetPrograms()
    {
        var owner = _workspace.CurrentOwnerKey;
        return _programs.Find(p => p.UserId == owner).OrderByDescending(p => p.CreatedAt);
    }

    public WorkoutProgram? GetProgram(int id) => _programs.FindById(id);

    public void UpsertProgram(WorkoutProgram program)
    {
        if (program.Id == 0 && program.UserId == null)
            program.UserId = _workspace.CurrentOwnerKey;
        _programs.Upsert(program);
    }

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

    public IEnumerable<WorkoutSession> GetSessions(int programId, int dayNumber)
    {
        var owner = _workspace.CurrentOwnerKey;
        return _sessions.Find(s => s.UserId == owner && s.ProgramId == programId && s.DayNumber == dayNumber)
                        .OrderByDescending(s => s.StartedAt);
    }

    public IEnumerable<WorkoutSession> GetRecentSessions(int count = 20)
    {
        var owner = _workspace.CurrentOwnerKey;
        return _sessions.Find(s => s.UserId == owner).OrderByDescending(s => s.StartedAt).Take(count);
    }

    public WorkoutSession? GetSession(int id) => _sessions.FindById(id);

    public void UpsertSession(WorkoutSession session)
    {
        if (session.Id == 0 && session.UserId == null)
            session.UserId = _workspace.CurrentOwnerKey;
        _sessions.Upsert(session);
    }

    public void DeleteSession(int id) => _sessions.Delete(id);

    public IEnumerable<WorkoutSession> GetSessionsForPeriod(DateTime start, DateTime end)
    {
        var owner = _workspace.CurrentOwnerKey;
        return _sessions.Find(s => s.UserId == owner)
                        .Where(s => s.StartedAt >= start && s.StartedAt < end)
                        .OrderByDescending(s => s.StartedAt);
    }

    public IEnumerable<PerformedSet> GetHistoryForExercise(string exerciseKey, int limit = 30)
    {
        var owner = _workspace.CurrentOwnerKey;
        return _sessions.Find(s => s.UserId == owner)
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
