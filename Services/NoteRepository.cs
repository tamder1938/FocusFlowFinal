using FocusFlowFinal.Models.Notes;
using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public class NoteRepository : INoteRepository
{
    private readonly IDatabaseService _db;

    public NoteRepository(IDatabaseService db) => _db = db;

    public IEnumerable<Note> GetAll() => _db.GetAllNotes();
    public IEnumerable<Note> Search(string? query, string? tag, DateTime? from, DateTime? to) =>
        _db.SearchNotes(query, tag, from, to);
    public Note? GetById(int id) => _db.GetNoteById(id);
    public int Upsert(Note note) => _db.UpsertNote(note);
    public void Delete(int id) => _db.DeleteNote(id);
    public HashSet<DateTime> GetDatesWithNotes(DateTime from, DateTime to) => _db.GetNoteDates(from, to);
    public IEnumerable<string> GetAllTags() => _db.GetAllNoteTags();
}
