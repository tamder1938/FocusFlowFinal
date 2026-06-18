using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using FocusFlowFinal.Models;
using FocusFlowFinal.Models.Finance;

namespace FocusFlowFinal.Services
{
    public class DatabaseService : IDatabaseService, IDisposable
    {
        private readonly LiteDatabase _db;
        private const string EventsCollection        = "events";
        private const string TasksCollection         = "tasks";
        private const string SessionsCollection      = "sessions";
        private const string TemplatesCollection     = "timer_templates";
        private const string IncomeCollection        = "finance_income";
        private const string ExpenseCollection       = "finance_expense";
        private const string FinSubCollection        = "finance_subscription";
        private const string LoanCollection          = "finance_loan";
        private const string CategoryCollection      = "finance_category";
        private const string EarlyRepaymentCollection = "finance_early_repayment";
        private const string SavingsCollection       = "savings_account";
        private const string SavingsTxCollection     = "savings_tx";

        public DatabaseService()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FocusFlow");
            Directory.CreateDirectory(folder);
            var dbPath = Path.Combine(folder, "FocusFlowFinal.db");

            _db = new LiteDatabase(dbPath);

            _db.GetCollection<CalendarEvent>(EventsCollection).EnsureIndex(e => e.Start);
            _db.GetCollection<TaskItem>(TasksCollection).EnsureIndex(t => t.DueDate);
            _db.GetCollection<FocusSession>(SessionsCollection).EnsureIndex(s => s.StartTime);

            var templates = _db.GetCollection<TimerTemplate>(TemplatesCollection);
            templates.EnsureIndex(t => t.Name);
            if (templates.Count() == 0)
            {
                templates.Insert(new TimerTemplate
                {
                    Name         = LocalizationService.Instance["PomodoroDefault"],
                    WorkMinutes  = 25,
                    BreakMinutes = 5,
                    Cycles       = 4,
                    IsBuiltIn    = true
                });
            }
        }

        public void Dispose() => _db.Dispose();

        // ── Календарные события ─────────────────────────────────────────

        public IEnumerable<CalendarEvent> GetEvents(DateTime date)
        {
            try
            {
                var col        = _db.GetCollection<CalendarEvent>(EventsCollection);
                var startOfDay = date.Date;
                var endOfDay   = startOfDay.AddDays(1);

                var rawEvents = col.Find(e =>
                    (e.Start >= startOfDay && e.Start < endOfDay && e.Recurrence == RecurrenceType.None) ||
                    (e.Recurrence != RecurrenceType.None && e.Start < endOfDay)
                ).ToList();

                var computedEvents = new List<CalendarEvent>();

                foreach (var ev in rawEvents)
                {
                    if (ev.ExceptionDates != null && ev.ExceptionDates.Any(d => d.Date == startOfDay))
                        continue;

                    if (ev.Recurrence == RecurrenceType.None)
                    {
                        computedEvents.Add(ev);
                        continue;
                    }

                    bool isMatch    = false;
                    DateTime checkDate = date.Date;

                    if (ev.Start.Date > checkDate) continue;

                    if (ev.RecurrenceEndDate.HasValue && checkDate > ev.RecurrenceEndDate.Value.Date)
                        continue;

                    switch (ev.Recurrence)
                    {
                        case RecurrenceType.Daily:
                            isMatch = true;
                            break;

                        case RecurrenceType.Weekdays:
                            isMatch = checkDate.DayOfWeek != DayOfWeek.Saturday
                                   && checkDate.DayOfWeek != DayOfWeek.Sunday;
                            break;

                        case RecurrenceType.Weekly:
                            isMatch = ev.DaysOfWeek != null && ev.DaysOfWeek.Contains(checkDate.DayOfWeek);
                            break;

                        case RecurrenceType.Monthly:
                            isMatch = checkDate.Day == ev.Start.Day;
                            break;

                        case RecurrenceType.Yearly:
                        {
                            if (ev.RecurrenceEndYear.HasValue && checkDate.Year > ev.RecurrenceEndYear.Value)
                                break;

                            int targetMonth = ev.RecurrenceStartMonth ?? ev.Start.Month;
                            int targetDay   = ev.RecurrenceStartDay   ?? ev.Start.Day;
                            if (checkDate.Month == targetMonth)
                            {
                                int effectiveDay = (targetDay == 29 && targetMonth == 2 && !DateTime.IsLeapYear(checkDate.Year))
                                    ? 28
                                    : Math.Min(targetDay, DateTime.DaysInMonth(checkDate.Year, targetMonth));
                                isMatch = checkDate.Day == effectiveDay;
                            }
                            break;
                        }

                        case RecurrenceType.Shift:
                            if (ev.CycleStartDate.HasValue && checkDate >= ev.CycleStartDate.Value.Date)
                            {
                                int working         = ev.WorkingDays ?? 1;
                                int off             = ev.OffDays ?? 1;
                                int totalCycleDays  = working + off;
                                int daysPassed      = (checkDate - ev.CycleStartDate.Value.Date).Days;
                                int positionInCycle = daysPassed % totalCycleDays;
                                isMatch = positionInCycle < working;
                            }
                            break;

                        case RecurrenceType.Custom:
                            if (ev.IntervalValue.HasValue && ev.IntervalValue > 0)
                            {
                                int val = ev.IntervalValue.Value;
                                if (ev.IntervalUnit == IntervalUnit.Days)
                                    isMatch = (checkDate - ev.Start.Date).Days % val == 0;
                                else if (ev.IntervalUnit == IntervalUnit.Weeks)
                                    isMatch = (checkDate - ev.Start.Date).Days % (val * 7) == 0;
                                else if (ev.IntervalUnit == IntervalUnit.Months)
                                {
                                    int monthsDiff = (checkDate.Year - ev.Start.Year) * 12
                                                   + checkDate.Month - ev.Start.Month;
                                    isMatch = (monthsDiff % val == 0) && (checkDate.Day == ev.Start.Day);
                                }
                            }
                            break;
                    }

                    if (isMatch)
                    {
                        computedEvents.Add(new CalendarEvent
                        {
                            Id                        = ev.Id,
                            Title                     = ev.Title,
                            Color                     = ev.Color,
                            TaskId                    = ev.TaskId,
                            IsAllDay                  = ev.IsAllDay,
                            Recurrence                = ev.Recurrence,
                            RecurrenceEndDate         = ev.RecurrenceEndDate,
                            RecurrenceEndYear         = ev.RecurrenceEndYear,
                            NotificationOffsetMinutes = ev.NotificationOffsetMinutes,
                            Start = ev.IsAllDay
                                ? checkDate
                                : checkDate.Add(ev.Start.TimeOfDay),
                            End = ev.IsAllDay
                                ? checkDate.AddDays(1).AddSeconds(-1)
                                : checkDate.Add(ev.End.TimeOfDay),
                            ExceptionDates = ev.ExceptionDates
                        });
                    }
                }

                return computedEvents.OrderBy(e => e.Start).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"DatabaseService.GetEvents error for {date:yyyy-MM-dd}: {ex.Message}");
                return new List<CalendarEvent>();
            }
        }

        public IEnumerable<CalendarEvent> GetEventsForPeriod(DateTime start, DateTime end)
        {
            var col = _db.GetCollection<CalendarEvent>(EventsCollection);
            return col.Find(e => e.Start >= start && e.End <= end).ToList();
        }

        public void UpsertEvent(CalendarEvent ev)
        {
            var col = _db.GetCollection<CalendarEvent>(EventsCollection);
            if (ev.Id == 0) col.Insert(ev); else col.Update(ev);
        }

        public void DeleteEvent(int id)
        {
            _db.GetCollection<CalendarEvent>(EventsCollection).Delete(id);
        }

        public CalendarEvent? GetEventById(int id)
        {
            return _db.GetCollection<CalendarEvent>(EventsCollection).FindById(id);
        }

        public void ExcludeDateFromEvent(int eventId, DateTime date)
        {
            var col = _db.GetCollection<CalendarEvent>(EventsCollection);
            var ev  = col.FindById(eventId);
            if (ev == null) return;

            ev.ExceptionDates ??= new List<DateTime>();
            var targetDate = date.Date;
            if (!ev.ExceptionDates.Any(d => d.Date == targetDate))
            {
                ev.ExceptionDates.Add(targetDate);
                col.Update(ev);
            }
        }

        public void ExcludeDatesFromEvent(int eventId, IEnumerable<DateTime> dates)
        {
            var col = _db.GetCollection<CalendarEvent>(EventsCollection);
            var ev  = col.FindById(eventId);
            if (ev == null) return;

            ev.ExceptionDates ??= new List<DateTime>();
            bool changed = false;
            foreach (var date in dates)
            {
                var d = date.Date;
                if (!ev.ExceptionDates.Any(x => x.Date == d))
                {
                    ev.ExceptionDates.Add(d);
                    changed = true;
                }
            }
            if (changed) col.Update(ev);
        }

        public CalendarEvent? FindOriginalSeries(CalendarEvent virtualEvent)
        {
            var col = _db.GetCollection<CalendarEvent>(EventsCollection);
            return col.Find(e =>
                e.Title == virtualEvent.Title &&
                e.Recurrence != RecurrenceType.None &&
                e.Start.Date <= virtualEvent.Start.Date
            ).OrderBy(e => e.Start).FirstOrDefault();
        }

        public void InsertEvents(IEnumerable<CalendarEvent> events)
        {
            var col = _db.GetCollection<CalendarEvent>(EventsCollection);
            foreach (var ev in events)
            {
                ev.Id = 0;
                col.Insert(ev);
            }
        }

        public void DeleteEventForTask(int taskId)
        {
            _db.GetCollection<CalendarEvent>(EventsCollection).DeleteMany(e => e.TaskId == taskId);
        }

        public IEnumerable<CalendarEvent> GetEventsForDisplay(DateTime date)
        {
            var events = GetEvents(date).ToList();

            var tasks = GetTasksByDate(date)
                .Where(t => t.StartTime.HasValue && !t.IsCompleted)
                .ToList();

            foreach (var task in tasks)
            {
                var startDt = date.Date.Add(task.StartTime!.Value);
                DateTime endDt;

                if (task.EndTime.HasValue)
                    endDt = date.Date.Add(task.EndTime.Value);
                else if (task.PlannedDurationMinutes > 0)
                    endDt = startDt.AddMinutes(task.PlannedDurationMinutes);
                else
                    endDt = startDt.AddMinutes(30);

                if (endDt <= startDt) endDt = startDt.AddMinutes(30);

                events.Add(new CalendarEvent
                {
                    Id         = 0,
                    TaskId     = task.Id,
                    Title      = task.Title,
                    Color      = task.Color ?? "#6366F1",
                    Start      = startDt,
                    End        = endDt,
                    IsAllDay   = false,
                    Recurrence = RecurrenceType.None
                });
            }

            return events.OrderBy(e => e.Start).ToList();
        }

        // ── Задачи ──────────────────────────────────────────────────────

        public IEnumerable<TaskItem> GetAllTasks()
        {
            return _db.GetCollection<TaskItem>(TasksCollection)
                      .FindAll()
                      .OrderBy(t => t.DueDate)
                      .ThenByDescending(t => t.Priority)
                      .ToList();
        }

        public TaskItem? GetTask(int id)
        {
            return _db.GetCollection<TaskItem>(TasksCollection).FindById(id);
        }

        public void UpsertTask(TaskItem task)
        {
            var col = _db.GetCollection<TaskItem>(TasksCollection);
            if (task.Id == 0) col.Insert(task); else col.Update(task);
        }

        public void DeleteTask(int id)
        {
            _db.GetCollection<TaskItem>(TasksCollection).Delete(id);
            _db.GetCollection<CalendarEvent>(EventsCollection).DeleteMany(e => e.TaskId == id);
        }

        public IEnumerable<TaskItem> GetTasksByDate(DateTime date)
        {
            return _db.GetCollection<TaskItem>(TasksCollection)
                      .Find(t => t.DueDate == date.Date)
                      .ToList();
        }

        public IEnumerable<TaskItem> GetTasksForPeriod(DateTime start, DateTime end)
        {
            return _db.GetCollection<TaskItem>(TasksCollection)
                      .Find(t => t.DueDate >= start.Date && t.DueDate < end.Date)
                      .ToList();
        }

        // ── Сессии фокусировки ──────────────────────────────────────────

        public void AddFocusSession(FocusSession session)
        {
            _db.GetCollection<FocusSession>(SessionsCollection).Insert(session);
        }

        public void UpdateFocusSession(FocusSession session)
        {
            _db.GetCollection<FocusSession>(SessionsCollection).Update(session);
        }

        public IEnumerable<FocusSession> GetSessionsForTask(int taskId)
        {
            return _db.GetCollection<FocusSession>(SessionsCollection)
                      .Find(s => s.TaskId == taskId)
                      .ToList();
        }

        public IEnumerable<FocusSession> GetSessionsForDate(DateTime date)
        {
            var start = date.Date;
            var end   = start.AddDays(1);
            return _db.GetCollection<FocusSession>(SessionsCollection)
                      .Find(s => s.StartTime >= start && s.StartTime < end)
                      .ToList();
        }

        public IEnumerable<FocusSession> GetSessionsForPeriod(DateTime start, DateTime end)
        {
            return _db.GetCollection<FocusSession>(SessionsCollection)
                      .Find(s => s.StartTime >= start.Date && s.StartTime < end.Date)
                      .ToList();
        }

        public FocusSession? GetActiveSession()
        {
            return _db.GetCollection<FocusSession>(SessionsCollection)
                      .Find(s => s.EndTime == null)
                      .FirstOrDefault();
        }

        // ── Шаблоны таймера ─────────────────────────────────────────────

        public IEnumerable<TimerTemplate> GetAllTimerTemplates()
        {
            return _db.GetCollection<TimerTemplate>(TemplatesCollection).FindAll().ToList();
        }

        public TimerTemplate? GetTimerTemplate(int id)
        {
            return _db.GetCollection<TimerTemplate>(TemplatesCollection).FindById(id);
        }

        public void UpsertTimerTemplate(TimerTemplate template)
        {
            var col = _db.GetCollection<TimerTemplate>(TemplatesCollection);
            if (template.Id == 0) col.Insert(template); else col.Update(template);
        }

        public void DeleteTimerTemplate(int id)
        {
            var col      = _db.GetCollection<TimerTemplate>(TemplatesCollection);
            var existing = col.FindById(id);
            if (existing != null && existing.IsBuiltIn) return;
            col.Delete(id);
        }

        // ── Проекты ─────────────────────────────────────────────────────

        public IEnumerable<ProjectItem> GetAllProjects()
        {
            return _db.GetCollection<ProjectItem>("projects").FindAll().ToList();
        }

        public void UpsertProject(ProjectItem project)
        {
            var col = _db.GetCollection<ProjectItem>("projects");
            if (project.Id == 0) col.Insert(project); else col.Update(project);
        }

        public void DeleteProject(int id)
        {
            _db.GetCollection<ProjectItem>("projects").Delete(id);
        }

        public void ClearAllData()
        {
            _db.DropCollection(EventsCollection);
            _db.DropCollection(TasksCollection);
            _db.DropCollection(SessionsCollection);
            _db.DropCollection(TemplatesCollection);
            _db.DropCollection("projects");

            _db.GetCollection<CalendarEvent>(EventsCollection).EnsureIndex(e => e.Start);
            _db.GetCollection<TaskItem>(TasksCollection).EnsureIndex(t => t.DueDate);
            _db.GetCollection<FocusSession>(SessionsCollection).EnsureIndex(s => s.StartTime);
        }

        // ── Финансы ─────────────────────────────────────────────────────

        public IEnumerable<FinanceIncome> GetAllIncomes()
        {
            return _db.GetCollection<FinanceIncome>(IncomeCollection)
                      .FindAll().OrderByDescending(x => x.Date).ToList();
        }

        public void UpsertIncome(FinanceIncome item)
        {
            var col = _db.GetCollection<FinanceIncome>(IncomeCollection);
            if (item.Id == 0) col.Insert(item); else col.Update(item);
        }

        public void DeleteIncome(int id)
        {
            _db.GetCollection<FinanceIncome>(IncomeCollection).Delete(id);
        }

        public IEnumerable<FinanceExpense> GetAllExpenses()
        {
            return _db.GetCollection<FinanceExpense>(ExpenseCollection)
                      .FindAll().OrderByDescending(x => x.Date).ToList();
        }

        public void UpsertExpense(FinanceExpense item)
        {
            var col = _db.GetCollection<FinanceExpense>(ExpenseCollection);
            if (item.Id == 0) col.Insert(item); else col.Update(item);
        }

        public void DeleteExpense(int id)
        {
            _db.GetCollection<FinanceExpense>(ExpenseCollection).Delete(id);
        }

        public IEnumerable<FinanceSubscriptionItem> GetAllFinanceSubscriptions()
        {
            return _db.GetCollection<FinanceSubscriptionItem>(FinSubCollection)
                      .FindAll().OrderBy(x => x.Name).ToList();
        }

        public void UpsertFinanceSubscription(FinanceSubscriptionItem item)
        {
            var col = _db.GetCollection<FinanceSubscriptionItem>(FinSubCollection);
            if (item.Id == 0) col.Insert(item); else col.Update(item);
        }

        public void DeleteFinanceSubscription(int id)
        {
            _db.GetCollection<FinanceSubscriptionItem>(FinSubCollection).Delete(id);
        }

        public IEnumerable<FinanceLoan> GetAllLoans()
        {
            return _db.GetCollection<FinanceLoan>(LoanCollection)
                      .FindAll().OrderBy(x => x.Name).ToList();
        }

        public void UpsertLoan(FinanceLoan item)
        {
            var col = _db.GetCollection<FinanceLoan>(LoanCollection);
            if (item.Id == 0) col.Insert(item); else col.Update(item);
        }

        public void DeleteLoan(int id)
        {
            _db.GetCollection<FinanceLoan>(LoanCollection).Delete(id);
        }

        public IEnumerable<FinanceCategory> GetCategoriesByType(string type)
        {
            return _db.GetCollection<FinanceCategory>(CategoryCollection)
                      .Find(x => x.Type == type)
                      .OrderBy(x => x.Name).ToList();
        }

        public void UpsertCategory(FinanceCategory category)
        {
            var col = _db.GetCollection<FinanceCategory>(CategoryCollection);
            if (category.Id == 0) col.Insert(category); else col.Update(category);
        }

        public void DeleteCategory(int id)
        {
            _db.GetCollection<FinanceCategory>(CategoryCollection).Delete(id);
        }

        public IEnumerable<LoanEarlyRepayment> GetEarlyRepayments(int loanId)
        {
            return _db.GetCollection<LoanEarlyRepayment>(EarlyRepaymentCollection)
                      .Find(x => x.LoanId == loanId)
                      .OrderByDescending(x => x.Date).ToList();
        }

        public void UpsertEarlyRepayment(LoanEarlyRepayment repayment)
        {
            var col = _db.GetCollection<LoanEarlyRepayment>(EarlyRepaymentCollection);
            if (repayment.Id == 0) col.Insert(repayment); else col.Update(repayment);
        }

        public void DeleteEarlyRepayment(int id)
        {
            _db.GetCollection<LoanEarlyRepayment>(EarlyRepaymentCollection).Delete(id);
        }

        public IEnumerable<SavingsAccount> GetAllSavingsAccounts()
        {
            return _db.GetCollection<SavingsAccount>(SavingsCollection)
                      .FindAll().OrderBy(x => x.Name).ToList();
        }

        public void UpsertSavingsAccount(SavingsAccount account)
        {
            var col = _db.GetCollection<SavingsAccount>(SavingsCollection);
            if (account.Id == 0) col.Insert(account); else col.Update(account);
        }

        public void DeleteSavingsAccount(int id)
        {
            _db.GetCollection<SavingsAccount>(SavingsCollection).Delete(id);
            _db.GetCollection<SavingsTransaction>(SavingsTxCollection).DeleteMany(x => x.AccountId == id);
        }

        public IEnumerable<SavingsTransaction> GetSavingsTransactions(int accountId)
        {
            return _db.GetCollection<SavingsTransaction>(SavingsTxCollection)
                      .Find(x => x.AccountId == accountId)
                      .OrderByDescending(x => x.Date).ToList();
        }

        public void AddSavingsTransaction(SavingsTransaction tx)
        {
            _db.GetCollection<SavingsTransaction>(SavingsTxCollection).Insert(tx);
            var accounts = _db.GetCollection<SavingsAccount>(SavingsCollection);
            var account  = accounts.FindById(tx.AccountId);
            if (account != null)
            {
                account.CurrentBalance += tx.Amount;
                accounts.Update(account);
            }
        }

        public void DeleteSavingsTransaction(int id)
        {
            var txCol = _db.GetCollection<SavingsTransaction>(SavingsTxCollection);
            var tx    = txCol.FindById(id);
            if (tx == null) return;
            txCol.Delete(id);
            var accounts = _db.GetCollection<SavingsAccount>(SavingsCollection);
            var account  = accounts.FindById(tx.AccountId);
            if (account != null)
            {
                account.CurrentBalance -= tx.Amount;
                accounts.Update(account);
            }
        }
    }
}
