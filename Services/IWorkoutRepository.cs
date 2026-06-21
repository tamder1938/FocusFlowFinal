using FocusFlowFinal.Models.Workout;
using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public interface IWorkoutRepository
{
    // Программы
    IEnumerable<WorkoutProgram> GetPrograms();
    WorkoutProgram?             GetProgram(int id);
    void                        UpsertProgram(WorkoutProgram program);
    void                        DeleteProgram(int id);
    void                        SetActiveProgram(int id);

    // Сессии
    IEnumerable<WorkoutSession> GetSessions(int programId, int dayNumber);
    IEnumerable<WorkoutSession> GetRecentSessions(int count = 20);
    WorkoutSession?             GetSession(int id);
    void                        UpsertSession(WorkoutSession session);
    void                        DeleteSession(int id);

    // Сессии за период
    IEnumerable<WorkoutSession> GetSessionsForPeriod(DateTime start, DateTime end);

    // История по упражнению
    IEnumerable<PerformedSet>   GetHistoryForExercise(string exerciseKey, int limit = 30);

    // Профиль
    WorkoutUserProfile          GetProfile();
    void                        SaveProfile(WorkoutUserProfile profile);
}
