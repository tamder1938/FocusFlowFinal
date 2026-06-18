using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using FocusFlowFinal.Models;
using FocusFlowFinal.Models.Finance;

namespace FocusFlowFinal.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly string _dbPath;
        private const string EventsCollection = "events";
        private const string TasksCollection = "tasks";
        private const string SessionsCollection = "sessions";
        private const string TemplatesCollection = "timer_templates";

        public DatabaseService()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FocusFlow");
            Directory.CreateDirectory(folder);
            _dbPath = Path.Combine(folder, "FocusFlowFinal.db");

            using var db = new LiteDatabase(_dbPath);
            var events = db.GetCollection<CalendarEvent>(EventsCollection);
            events.EnsureIndex(e => e.Start);

            var tasks = db.GetCollection<TaskItem>(TasksCollection);
            tasks.EnsureIndex(t => t.DueDate);

            var sessions = db.GetCollection<FocusSession>(SessionsCollection);
            sessions.EnsureIndex(s => s.StartTime);

            var templates = db.GetCollection<TimerTemplate>(TemplatesCollection);
            templates.EnsureIndex(t => t.Name);

            // ИСПРАВЛЕНО (4.5): если шаблонов нет — создаём встроенный "Помодоро 25/5".
            // Имя берём из текущей локализации; флаг IsBuiltIn защищает от удаления.
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

        public IEnumerable<CalendarEvent> GetEvents(DateTime date)
        {
            try
            {
                using var db = new LiteDatabase(_dbPath);
                var col = db.GetCollection<CalendarEvent>(EventsCollection);

                var startOfDay = date.Date;
                var endOfDay = startOfDay.AddDays(1);

                var rawEvents = col.Find(e =>
                    (e.Start >= startOfDay && e.Start < endOfDay && e.Recurrence == RecurrenceType.None) ||
                    (e.Recurrence != RecurrenceType.None && e.Start < endOfDay)
                ).ToList();

                var computedEvents = new List<CalendarEvent>();

                foreach (var ev in rawEvents)
                {
                    // Проверяем исключения
                    if (ev.ExceptionDates != null && ev.ExceptionDates.Any(d => d.Date == startOfDay))
                        continue;

                    if (ev.Recurrence == RecurrenceType.None)
                    {
                        computedEvents.Add(ev);
                        continue;
                    }

                    bool isMatch = false;
                    DateTime checkDate = date.Date;

                    if (ev.Start.Date > checkDate) continue;

                    // ИСПРАВЛЕНИЕ #4: Проверяем дату окончания повторений
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
                            // Проверяем год окончания
                            if (ev.RecurrenceEndYear.HasValue && checkDate.Year > ev.RecurrenceEndYear.Value)
                                break;

                            int targetMonth = ev.RecurrenceStartMonth ?? ev.Start.Month;
                            int targetDay   = ev.RecurrenceStartDay   ?? ev.Start.Day;
                            if (checkDate.Month == targetMonth)
                            {
                                // Для 29 февраля в невисокосный год показываем 28 февраля
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
                                int working = ev.WorkingDays ?? 1;
                                int off = ev.OffDays ?? 1;
                                int totalCycleDays = working + off;
                                int daysPassed = (checkDate - ev.CycleStartDate.Value.Date).Days;
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
                        var virtualEvent = new CalendarEvent
                        {
                            Id = ev.Id,
                            Title = ev.Title,
                            Color = ev.Color,
                            TaskId = ev.TaskId,
                            IsAllDay = ev.IsAllDay,
                            Recurrence = ev.Recurrence,
                            RecurrenceEndDate = ev.RecurrenceEndDate,
                            RecurrenceEndYear = ev.RecurrenceEndYear,
                            NotificationOffsetMinutes = ev.NotificationOffsetMinutes,
                            Start = ev.IsAllDay
                                ? checkDate
                                : checkDate.Add(ev.Start.TimeOfDay),
                            End = ev.IsAllDay
                                ? checkDate.AddDays(1).AddSeconds(-1)
                                : checkDate.Add(ev.End.TimeOfDay),
                            ExceptionDates = ev.ExceptionDates
                        };
                        computedEvents.Add(virtualEvent);
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
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<CalendarEvent>(EventsCollection);
            return col.Find(e => e.Start >= start && e.End <= end).ToList();
        }

        public void UpsertEvent(CalendarEvent ev)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<CalendarEvent>(EventsCollection);
            if (ev.Id == 0) col.Insert(ev); else col.Update(ev);
        }

        public void DeleteEvent(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<CalendarEvent>(EventsCollection);
            col.Delete(id);
        }

        public CalendarEvent? GetEventById(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<CalendarEvent>(EventsCollection);
            return col.FindById(id);
        }

        public void ExcludeDateFromEvent(int eventId, DateTime date)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<CalendarEvent>(EventsCollection);
            var ev = col.FindById(eventId);
            if (ev != null)
            {
                ev.ExceptionDates ??= new List<DateTime>();
                var targetDate = date.Date;
                if (!ev.ExceptionDates.Any(d => d.Date == targetDate))
                {
                    ev.ExceptionDates.Add(targetDate);
                    col.Update(ev);
                }
            }
        }

        public CalendarEvent? FindOriginalSeries(CalendarEvent virtualEvent)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<CalendarEvent>(EventsCollection);
            var candidates = col.Find(e =>
                e.Title == virtualEvent.Title &&
                e.Recurrence != RecurrenceType.None &&
                e.Start.Date <= virtualEvent.Start.Date
            ).OrderBy(e => e.Start).ToList();
            return candidates.FirstOrDefault();
        }

        public IEnumerable<TaskItem> GetAllTasks()
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<TaskItem>(TasksCollection);
            return col.FindAll().OrderBy(t => t.DueDate).ThenByDescending(t => t.Priority).ToList();
        }

        public TaskItem? GetTask(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<TaskItem>(TasksCollection);
            return col.FindById(id);
        }

        public void UpsertTask(TaskItem task)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<TaskItem>(TasksCollection);
            if (task.Id == 0) col.Insert(task); else col.Update(task);
        }

        public void DeleteTask(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            db.GetCollection<TaskItem>(TasksCollection).Delete(id);
            // Удаляем связанные календарные события
            db.GetCollection<CalendarEvent>(EventsCollection).DeleteMany(e => e.TaskId == id);
        }

        public IEnumerable<TaskItem> GetTasksByDate(DateTime date)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<TaskItem>(TasksCollection);
            return col.Find(t => t.DueDate == date.Date).ToList();
        }

        public IEnumerable<TaskItem> GetTasksForPeriod(DateTime start, DateTime end)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<TaskItem>(TasksCollection);
            return col.Find(t => t.DueDate >= start.Date && t.DueDate < end.Date).ToList();
        }

        public IEnumerable<CalendarEvent> GetEventsForDisplay(DateTime date)
        {
            var events = GetEvents(date).ToList();

            // Добавляем виртуальные события из задач с назначенным временем начала
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

                // Id=0 означает виртуальное событие (не хранится в БД)
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

        public void InsertEvents(IEnumerable<CalendarEvent> events)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<CalendarEvent>(EventsCollection);
            foreach (var ev in events)
            {
                ev.Id = 0; // сброс Id для автогенерации
                col.Insert(ev);
            }
        }

        public void DeleteEventForTask(int taskId)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<CalendarEvent>(EventsCollection);
            col.DeleteMany(e => e.TaskId == taskId);
        }

        // ── Финансы ─────────────────────────────────────────────────
        private const string IncomeCollection       = "finance_income";
        private const string ExpenseCollection      = "finance_expense";
        private const string FinSubCollection       = "finance_subscription";
        private const string LoanCollection         = "finance_loan";

        public IEnumerable<FinanceIncome> GetAllIncomes()
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<FinanceIncome>(IncomeCollection).FindAll()
                     .OrderByDescending(x => x.Date).ToList();
        }

        public void UpsertIncome(FinanceIncome item)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<FinanceIncome>(IncomeCollection);
            if (item.Id == 0) col.Insert(item); else col.Update(item);
        }

        public void DeleteIncome(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            db.GetCollection<FinanceIncome>(IncomeCollection).Delete(id);
        }

        public IEnumerable<FinanceExpense> GetAllExpenses()
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<FinanceExpense>(ExpenseCollection).FindAll()
                     .OrderByDescending(x => x.Date).ToList();
        }

        public void UpsertExpense(FinanceExpense item)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<FinanceExpense>(ExpenseCollection);
            if (item.Id == 0) col.Insert(item); else col.Update(item);
        }

        public void DeleteExpense(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            db.GetCollection<FinanceExpense>(ExpenseCollection).Delete(id);
        }

        public IEnumerable<FinanceSubscriptionItem> GetAllFinanceSubscriptions()
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<FinanceSubscriptionItem>(FinSubCollection).FindAll()
                     .OrderBy(x => x.Name).ToList();
        }

        public void UpsertFinanceSubscription(FinanceSubscriptionItem item)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<FinanceSubscriptionItem>(FinSubCollection);
            if (item.Id == 0) col.Insert(item); else col.Update(item);
        }

        public void DeleteFinanceSubscription(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            db.GetCollection<FinanceSubscriptionItem>(FinSubCollection).Delete(id);
        }

        public IEnumerable<FinanceLoan> GetAllLoans()
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<FinanceLoan>(LoanCollection).FindAll()
                     .OrderBy(x => x.Name).ToList();
        }

        public void UpsertLoan(FinanceLoan item)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<FinanceLoan>(LoanCollection);
            if (item.Id == 0) col.Insert(item); else col.Update(item);
        }

        public void DeleteLoan(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            db.GetCollection<FinanceLoan>(LoanCollection).Delete(id);
        }

        // ── Категории ────────────────────────────────────────────────
        private const string CategoryCollection = "finance_category";

        public IEnumerable<FinanceCategory> GetCategoriesByType(string type)
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<FinanceCategory>(CategoryCollection)
                     .Find(x => x.Type == type)
                     .OrderBy(x => x.Name).ToList();
        }

        public void UpsertCategory(FinanceCategory category)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<FinanceCategory>(CategoryCollection);
            if (category.Id == 0) col.Insert(category); else col.Update(category);
        }

        public void DeleteCategory(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            db.GetCollection<FinanceCategory>(CategoryCollection).Delete(id);
        }

        // ── Досрочные погашения ───────────────────────────────────────
        private const string EarlyRepaymentCollection = "finance_early_repayment";

        public IEnumerable<LoanEarlyRepayment> GetEarlyRepayments(int loanId)
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<LoanEarlyRepayment>(EarlyRepaymentCollection)
                     .Find(x => x.LoanId == loanId)
                     .OrderByDescending(x => x.Date).ToList();
        }

        public void UpsertEarlyRepayment(LoanEarlyRepayment repayment)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<LoanEarlyRepayment>(EarlyRepaymentCollection);
            if (repayment.Id == 0) col.Insert(repayment); else col.Update(repayment);
        }

        public void DeleteEarlyRepayment(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            db.GetCollection<LoanEarlyRepayment>(EarlyRepaymentCollection).Delete(id);
        }

        // ── Копилки / сберегательные счета ───────────────────────────
        private const string SavingsCollection    = "savings_account";
        private const string SavingsTxCollection  = "savings_tx";

        public IEnumerable<SavingsAccount> GetAllSavingsAccounts()
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<SavingsAccount>(SavingsCollection).FindAll()
                     .OrderBy(x => x.Name).ToList();
        }

        public void UpsertSavingsAccount(SavingsAccount account)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<SavingsAccount>(SavingsCollection);
            if (account.Id == 0) col.Insert(account); else col.Update(account);
        }

        public void DeleteSavingsAccount(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            db.GetCollection<SavingsAccount>(SavingsCollection).Delete(id);
            // Удаляем связанные транзакции
            db.GetCollection<SavingsTransaction>(SavingsTxCollection).DeleteMany(x => x.AccountId == id);
        }

        public IEnumerable<SavingsTransaction> GetSavingsTransactions(int accountId)
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<SavingsTransaction>(SavingsTxCollection)
                     .Find(x => x.AccountId == accountId)
                     .OrderByDescending(x => x.Date).ToList();
        }

        public void AddSavingsTransaction(SavingsTransaction tx)
        {
            using var db = new LiteDatabase(_dbPath);
            db.GetCollection<SavingsTransaction>(SavingsTxCollection).Insert(tx);
            // Пересчитываем баланс счёта
            var accounts = db.GetCollection<SavingsAccount>(SavingsCollection);
            var account = accounts.FindById(tx.AccountId);
            if (account != null)
            {
                account.CurrentBalance += tx.Amount;
                accounts.Update(account);
            }
        }

        public void DeleteSavingsTransaction(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            var txCol = db.GetCollection<SavingsTransaction>(SavingsTxCollection);
            var tx = txCol.FindById(id);
            if (tx == null) return;
            txCol.Delete(id);
            // Откатываем изменение баланса
            var accounts = db.GetCollection<SavingsAccount>(SavingsCollection);
            var account = accounts.FindById(tx.AccountId);
            if (account != null)
            {
                account.CurrentBalance -= tx.Amount;
                accounts.Update(account);
            }
        }

        public void AddFocusSession(FocusSession session)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<FocusSession>(SessionsCollection);
            col.Insert(session);
        }

        public void UpdateFocusSession(FocusSession session)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<FocusSession>(SessionsCollection);
            col.Update(session);
        }

        public IEnumerable<FocusSession> GetSessionsForTask(int taskId)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<FocusSession>(SessionsCollection);
            return col.Find(s => s.TaskId == taskId).ToList();
        }

        public IEnumerable<FocusSession> GetSessionsForDate(DateTime date)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<FocusSession>(SessionsCollection);
            var start = date.Date;
            var end = start.AddDays(1);
            return col.Find(s => s.StartTime >= start && s.StartTime < end).ToList();
        }

        public IEnumerable<FocusSession> GetSessionsForPeriod(DateTime start, DateTime end)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<FocusSession>(SessionsCollection);
            return col.Find(s => s.StartTime >= start.Date && s.StartTime < end.Date).ToList();
        }

        public FocusSession? GetActiveSession()
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<FocusSession>(SessionsCollection);
            return col.Find(s => s.EndTime == null).FirstOrDefault();
        }

        public IEnumerable<TimerTemplate> GetAllTimerTemplates()
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<TimerTemplate>(TemplatesCollection);
            return col.FindAll().ToList();
        }

        public TimerTemplate? GetTimerTemplate(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<TimerTemplate>(TemplatesCollection);
            return col.FindById(id);
        }

        public void UpsertTimerTemplate(TimerTemplate template)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<TimerTemplate>(TemplatesCollection);
            if (template.Id == 0) col.Insert(template); else col.Update(template);
        }

        public void DeleteTimerTemplate(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<TimerTemplate>(TemplatesCollection);

            // ИСПРАВЛЕНО (4.5): встроенный шаблон "Помодоро 25/5" нельзя удалить
            var existing = col.FindById(id);
            if (existing != null && existing.IsBuiltIn) return;

            col.Delete(id);
        }

        public IEnumerable<ProjectItem> GetAllProjects()
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<ProjectItem>("projects");
            return col.FindAll().ToList();
        }

        public void UpsertProject(ProjectItem project)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<ProjectItem>("projects");
            if (project.Id == 0) col.Insert(project); else col.Update(project);
        }

        public void DeleteProject(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<ProjectItem>("projects");
            col.Delete(id);
        }

        public void ClearAllData()
        {
            using var db = new LiteDatabase(_dbPath);
            db.DropCollection(EventsCollection);
            db.DropCollection(TasksCollection);
            db.DropCollection(SessionsCollection);
            db.DropCollection(TemplatesCollection);
            db.DropCollection("projects");

            // Пересоздаём индексы чтобы коллекции были готовы к работе
            var events   = db.GetCollection<CalendarEvent>(EventsCollection);
            events.EnsureIndex(e => e.Start);
            var tasks    = db.GetCollection<TaskItem>(TasksCollection);
            tasks.EnsureIndex(t => t.DueDate);
            var sessions = db.GetCollection<FocusSession>(SessionsCollection);
            sessions.EnsureIndex(s => s.StartTime);
        }
    }
}
