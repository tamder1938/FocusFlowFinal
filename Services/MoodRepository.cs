using FocusFlowFinal.Models.Mood;
using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public class MoodRepository : IMoodRepository
{
    private readonly IDatabaseService _db;
    public MoodRepository(IDatabaseService db) => _db = db;

    public IEnumerable<MoodEntry>   GetAllEntries()                               => _db.GetAllMoodEntries();
    public IEnumerable<MoodEntry>   GetEntriesForPeriod(DateTime from, DateTime to) => _db.GetMoodEntriesForPeriod(from, to);
    public MoodEntry?               GetEntryById(int id)                          => _db.GetMoodEntryById(id);
    public int                      UpsertEntry(MoodEntry entry)                  => _db.UpsertMoodEntry(entry);
    public void                     DeleteEntry(int id)                           => _db.DeleteMoodEntry(id);
    public IEnumerable<MoodActivity> GetAllActivities()                           => _db.GetAllMoodActivities();
    public int                      UpsertActivity(MoodActivity activity)         => _db.UpsertMoodActivity(activity);
    public void                     DeleteActivity(int id)                        => _db.DeleteMoodActivity(id);
}
