using FocusFlowFinal.Models.Mood;
using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public interface IMoodRepository
{
    // Записи настроения
    IEnumerable<MoodEntry> GetAllEntries();
    IEnumerable<MoodEntry> GetEntriesForPeriod(DateTime from, DateTime to);
    MoodEntry? GetEntryById(int id);
    int UpsertEntry(MoodEntry entry);
    void DeleteEntry(int id);

    // Активности
    IEnumerable<MoodActivity> GetAllActivities();
    int UpsertActivity(MoodActivity activity);
    void DeleteActivity(int id);
}
