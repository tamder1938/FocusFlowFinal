using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Notes;
using FocusFlowFinal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class NoteViewModel : ObservableObject
{
    private readonly INoteRepository   _repo;
    private readonly NoteExportService _export;
    private readonly DispatcherTimer   _autosaveTimer;
    private bool _suppressAutosave;

    // ── Список заметок ──────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<Note> _noteList = new();
    [ObservableProperty] private Note? _selectedNote;

    // ── Поля редактора (связаны с выбранной заметкой) ───────────────
    [ObservableProperty] private string _editTitle   = string.Empty;
    [ObservableProperty] private string _editContent = string.Empty;
    [ObservableProperty] private DateTimeOffset _editDate = DateTimeOffset.Now;
    [ObservableProperty] private ObservableCollection<string> _editTags = new();

    // ── Тег-инпут ───────────────────────────────────────────────────
    [ObservableProperty] private string _newTagInput = string.Empty;

    // ── Строка поиска/фильтров ──────────────────────────────────────
    [ObservableProperty] private string   _searchQuery   = string.Empty;
    [ObservableProperty] private string?  _selectedTag   = null;
    [ObservableProperty] private DateTimeOffset? _filterFrom = null;
    [ObservableProperty] private DateTimeOffset? _filterTo   = null;

    // ── Все теги (для фильтра) ───────────────────────────────────────
    [ObservableProperty] private ObservableCollection<string> _allTags = new();

    // ── Статус автосохранения ────────────────────────────────────────
    [ObservableProperty] private string _saveStatus = string.Empty;

    // ── Результат последнего экспорта (для отображения пользователю) ─
    [ObservableProperty] private string _exportResult = string.Empty;

    public LocalizationService Loc => LocalizationService.Instance;

    public bool HasSelectedNote => SelectedNote != null;
    public bool IsNoteListEmpty => !NoteList.Any();

    public NoteViewModel(INoteRepository repo, NoteExportService export)
    {
        _repo   = repo;
        _export = export;

        _autosaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _autosaveTimer.Tick += (_, _) => { _autosaveTimer.Stop(); _ = AutosaveAsync(); };

        LoadNotes();
        RefreshAllTags();
    }

    // ── Загрузка / поиск ────────────────────────────────────────────

    private void LoadNotes()
    {
        var results = _repo.Search(
            string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery,
            SelectedTag,
            FilterFrom?.LocalDateTime,
            FilterTo?.LocalDateTime);

        NoteList.Clear();
        foreach (var n in results) NoteList.Add(n);
        OnPropertyChanged(nameof(IsNoteListEmpty));
    }

    private void RefreshAllTags()
    {
        var tags = _repo.GetAllTags().ToList();
        AllTags.Clear();
        foreach (var t in tags) AllTags.Add(t);
    }

    // ── Реакция на изменение фильтров ────────────────────────────────

    partial void OnSearchQueryChanged(string value)   => LoadNotes();
    partial void OnSelectedTagChanged(string? value)  => LoadNotes();
    partial void OnFilterFromChanged(DateTimeOffset? value) => LoadNotes();
    partial void OnFilterToChanged(DateTimeOffset? value)   => LoadNotes();

    // ── Выбор заметки ───────────────────────────────────────────────

    partial void OnSelectedNoteChanged(Note? value)
    {
        _suppressAutosave = true;
        _autosaveTimer.Stop();

        if (value == null)
        {
            EditTitle   = string.Empty;
            EditContent = string.Empty;
            EditDate    = DateTimeOffset.Now;
            EditTags.Clear();
        }
        else
        {
            EditTitle   = value.Title ?? string.Empty;
            EditContent = value.MarkdownContent ?? string.Empty;
            EditDate    = new DateTimeOffset(value.Date, DateTimeOffset.Now.Offset);
            EditTags.Clear();
            foreach (var t in value.Tags) EditTags.Add(t);
        }

        SaveStatus = string.Empty;
        ExportResult = string.Empty;
        OnPropertyChanged(nameof(HasSelectedNote));
        _suppressAutosave = false;
    }

    // ── Изменения в редакторе → планируем автосохранение ────────────

    partial void OnEditTitleChanged(string value)   => ScheduleAutosave();
    partial void OnEditContentChanged(string value) => ScheduleAutosave();
    partial void OnEditDateChanged(DateTimeOffset value) => ScheduleAutosave();

    private void ScheduleAutosave()
    {
        if (_suppressAutosave || SelectedNote == null) return;
        SaveStatus = Loc["Notes_UnsavedStatus"];
        _autosaveTimer.Stop();
        _autosaveTimer.Start();
    }

    private async Task AutosaveAsync()
    {
        if (SelectedNote == null) return;
        SaveStatus = Loc["Notes_SavingStatus"];
        await Task.Run(() => CommitToModel());
        SaveStatus = Loc["Notes_SavedStatus"];
        RefreshNoteInList();
    }

    private void CommitToModel()
    {
        if (SelectedNote == null) return;
        SelectedNote.Title           = EditTitle.Trim();
        SelectedNote.MarkdownContent = EditContent;
        SelectedNote.Date            = EditDate.Date;
        SelectedNote.Tags            = EditTags.ToList();
        SelectedNote.Id              = _repo.Upsert(SelectedNote);
    }

    private void RefreshNoteInList()
    {
        var idx = NoteList.IndexOf(SelectedNote!);
        if (idx >= 0) NoteList[idx] = SelectedNote!;
        RefreshAllTags();
    }

    // ── Команды ─────────────────────────────────────────────────────

    [RelayCommand]
    private void NewNote()
    {
        var note = new Note { Date = DateTime.Today, Title = "Новая заметка" };
        _repo.Upsert(note);
        LoadNotes();
        RefreshAllTags();
        SelectedNote = NoteList.FirstOrDefault(n => n.Id == note.Id) ?? NoteList.FirstOrDefault();
        OnPropertyChanged(nameof(IsNoteListEmpty));
    }

    [RelayCommand]
    private async Task DeleteNote()
    {
        if (SelectedNote == null) return;
        _autosaveTimer.Stop();

        _repo.Delete(SelectedNote.Id);
        NoteList.Remove(SelectedNote);
        SelectedNote = NoteList.FirstOrDefault();
        OnPropertyChanged(nameof(IsNoteListEmpty));
        RefreshAllTags();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void AddTag()
    {
        var tag = NewTagInput.Trim();
        if (string.IsNullOrEmpty(tag) || EditTags.Contains(tag)) return;
        EditTags.Add(tag);
        NewTagInput = string.Empty;
        // Make the tag visible in the filter list immediately (before autosave hits the db)
        if (!AllTags.Contains(tag))
            AllTags.Add(tag);
        ScheduleAutosave();
    }

    [RelayCommand]
    private void RemoveTag(string tag)
    {
        EditTags.Remove(tag);
        ScheduleAutosave();
    }

    [RelayCommand]
    private void ClearFilter()
    {
        SearchQuery  = string.Empty;
        SelectedTag  = null;
        FilterFrom   = null;
        FilterTo     = null;
    }

    [RelayCommand]
    private async Task SaveNow()
    {
        _autosaveTimer.Stop();
        await AutosaveAsync();
    }

    // ── Экспорт (вызывается из code-behind с уже выбранным путём) ───

    public void ExportAsMd(string path)
    {
        if (SelectedNote == null) return;
        CommitToModel();
        try
        {
            _export.ExportMarkdown(SelectedNote, path);
            ExportResult = $"{Loc["Notes_ExportDone"]} {System.IO.Path.GetFileName(path)}";
        }
        catch (Exception ex) { ExportResult = $"{Loc["Notes_ExportError"]}: {ex.Message}"; }
    }

    public void ExportAsHtml(string path)
    {
        if (SelectedNote == null) return;
        CommitToModel();
        try
        {
            _export.ExportHtml(SelectedNote, path);
            ExportResult = $"{Loc["Notes_ExportDone"]} {System.IO.Path.GetFileName(path)}";
        }
        catch (Exception ex) { ExportResult = $"{Loc["Notes_ExportError"]}: {ex.Message}"; }
    }

    public void ExportAsTxt(string path)
    {
        if (SelectedNote == null) return;
        CommitToModel();
        try
        {
            _export.ExportText(SelectedNote, path);
            ExportResult = $"{Loc["Notes_ExportDone"]} {System.IO.Path.GetFileName(path)}";
        }
        catch (Exception ex) { ExportResult = $"{Loc["Notes_ExportError"]}: {ex.Message}"; }
    }
}
