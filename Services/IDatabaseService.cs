using FocusFlowFinal.Models;
using FocusFlowFinal.Models.Finance;
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
}
