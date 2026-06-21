using FocusFlowFinal.Models.Notes;
using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public interface INoteRepository
{
    IEnumerable<Note> GetAll();
    IEnumerable<Note> Search(string? query, string? tag, DateTime? from, DateTime? to);
    Note? GetById(int id);
    int Upsert(Note note);
    void Delete(int id);
    HashSet<DateTime> GetDatesWithNotes(DateTime from, DateTime to);
    IEnumerable<string> GetAllTags();
}
