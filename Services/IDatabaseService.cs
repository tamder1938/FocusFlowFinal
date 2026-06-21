using FocusFlowFinal.Models;
using FocusFlowFinal.Models.Finance;
using FocusFlowFinal.Models.Habits;
using FocusFlowFinal.Models.Mood;
using FocusFlowFinal.Models.Notes;
using FocusFlowFinal.Models.Media;
using FocusFlowFinal.Models.Sound;
using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public interface IDatabaseService
{
    // ── Календарные события ─────────────────────────────────────────
    IEnumerable<CalendarEvent> GetEvents(DateTime date);
    IEnumerable<CalendarEvent> GetEventsForPeriod(DateTime start, DateTime end);
    void UpsertEvent(CalendarEvent ev);
    void DeleteEvent(int id);
    CalendarEvent? GetEventById(int id);
    void ExcludeDateFromEvent(int eventId, DateTime date);
    void ExcludeDatesFromEvent(int eventId, IEnumerable<DateTime> dates);
    CalendarEvent? FindOriginalSeries(CalendarEvent virtualEvent);
    void InsertEvents(IEnumerable<CalendarEvent> events);

    // ── Задачи ──────────────────────────────────────────────────────
    IEnumerable<TaskItem> GetAllTasks();
    TaskItem? GetTask(int id);
    void UpsertTask(TaskItem task);
    void DeleteTask(int id);
    IEnumerable<TaskItem> GetTasksByDate(DateTime date);
    IEnumerable<TaskItem> GetTasksForPeriod(DateTime start, DateTime end);

    // ── Отображение (события + задачи) ──────────────────────────────
    IEnumerable<CalendarEvent> GetEventsForDisplay(DateTime date);

    // ── Удаление события, связанного с задачей ──────────────────────
    void DeleteEventForTask(int taskId);

    // ── Сессии фокусировки ──────────────────────────────────────────
    void AddFocusSession(FocusSession session);
    void UpdateFocusSession(FocusSession session);
    IEnumerable<FocusSession> GetSessionsForTask(int taskId);
    IEnumerable<FocusSession> GetSessionsForDate(DateTime date);
    IEnumerable<FocusSession> GetSessionsForPeriod(DateTime start, DateTime end);
    FocusSession? GetActiveSession();

    // ── Шаблоны таймера ─────────────────────────────────────────────
    IEnumerable<TimerTemplate> GetAllTimerTemplates();
    TimerTemplate? GetTimerTemplate(int id);
    void UpsertTimerTemplate(TimerTemplate template);
    void DeleteTimerTemplate(int id);

    // ── Проекты ─────────────────────────────────────────────────────
    IEnumerable<ProjectItem> GetAllProjects();
    void UpsertProject(ProjectItem project);
    void DeleteProject(int id);

    // ── Очистка всех данных (опасная зона в настройках) ─────────────
    void ClearAllData();

    // ── Финансовый модуль ───────────────────────────────────────────
    IEnumerable<FinanceIncome> GetAllIncomes();
    void UpsertIncome(FinanceIncome item);
    void DeleteIncome(int id);

    IEnumerable<FinanceExpense> GetAllExpenses();
    void UpsertExpense(FinanceExpense item);
    void DeleteExpense(int id);

    IEnumerable<FinanceSubscriptionItem> GetAllFinanceSubscriptions();
    void UpsertFinanceSubscription(FinanceSubscriptionItem item);
    void DeleteFinanceSubscription(int id);

    IEnumerable<FinanceLoan> GetAllLoans();
    void UpsertLoan(FinanceLoan item);
    void DeleteLoan(int id);

    // ── Пользовательские категории ──────────────────────────────────
    IEnumerable<FinanceCategory> GetCategoriesByType(string type);
    void UpsertCategory(FinanceCategory category);
    void DeleteCategory(int id);

    // ── Досрочные погашения кредитов ────────────────────────────────
    IEnumerable<LoanEarlyRepayment> GetEarlyRepayments(int loanId);
    void UpsertEarlyRepayment(LoanEarlyRepayment repayment);
    void DeleteEarlyRepayment(int id);

    // ── Копилки / сберегательные счета ──────────────────────────────
    IEnumerable<SavingsAccount> GetAllSavingsAccounts();
    void UpsertSavingsAccount(SavingsAccount account);
    void DeleteSavingsAccount(int id);
    IEnumerable<SavingsTransaction> GetSavingsTransactions(int accountId);
    void AddSavingsTransaction(SavingsTransaction tx);
    void DeleteSavingsTransaction(int id);

    // ── Трекер привычек ─────────────────────────────────────────────
    IEnumerable<Habit> GetAllHabits();
    void UpsertHabit(Habit habit);
    void DeleteHabit(int id);

    IEnumerable<HabitCompletion> GetHabitCompletions(int habitId);
    IEnumerable<HabitCompletion> GetCompletionsForPeriod(DateTime start, DateTime end);
    bool HasCompletionForDate(int habitId, DateTime date);
    HabitCompletion? GetCompletionForDate(int habitId, DateTime date);
    void UpsertHabitCompletion(HabitCompletion completion);
    void DeleteHabitCompletion(int id);
    void AutoCompleteHabitsForTask(int taskId);

    IEnumerable<HabitCategory> GetAllHabitCategories();
    void UpsertHabitCategory(HabitCategory category);
    void DeleteHabitCategory(int id);

    // ── Шаблоны привычек ────────────────────────────────────────────────
    IEnumerable<HabitTemplate> GetAllHabitTemplates();
    void UpsertHabitTemplate(HabitTemplate template);
    void DeleteHabitTemplate(int id);

    // ── Трекер настроения ────────────────────────────────────────────
    IEnumerable<MoodEntry>    GetAllMoodEntries();
    IEnumerable<MoodEntry>    GetMoodEntriesForPeriod(DateTime from, DateTime to);
    MoodEntry?                GetMoodEntryById(int id);
    int                       UpsertMoodEntry(MoodEntry entry);
    void                      DeleteMoodEntry(int id);
    IEnumerable<MoodActivity> GetAllMoodActivities();
    int                       UpsertMoodActivity(MoodActivity activity);
    void                      DeleteMoodActivity(int id);

    // ── Заметки и дневник ───────────────────────────────────────────
    IEnumerable<Note> GetAllNotes();
    Note? GetNoteById(int id);
    int UpsertNote(Note note);
    void DeleteNote(int id);
    HashSet<DateTime> GetNoteDates(DateTime from, DateTime to);
    IEnumerable<string> GetAllNoteTags();
    IEnumerable<Note> SearchNotes(string? query, string? tag, DateTime? from, DateTime? to);

    // ── Пользовательские звуки ───────────────────────────────────────
    IEnumerable<UserSound> GetAllUserSounds();
    int                    UpsertUserSound(UserSound sound);
    void                   DeleteUserSound(int id);

    // ── Трекер медиа ─────────────────────────────────────────────────
    IEnumerable<MediaItem> GetAllMediaItems();
    MediaItem?             GetMediaItemById(int id);
    int                    UpsertMediaItem(MediaItem item);
    void                   DeleteMediaItem(int id);
}
