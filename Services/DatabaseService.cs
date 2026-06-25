using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using FocusFlowFinal.Models;
using FocusFlowFinal.Models.Finance;
using FocusFlowFinal.Models.Habits;
using FocusFlowFinal.Models.Mood;
using FocusFlowFinal.Models.Notes;
using FocusFlowFinal.Models.Media;
using FocusFlowFinal.Models.Sound;
using FocusFlowFinal.Services.Security;

namespace FocusFlowFinal.Services
{
    public class DatabaseService : IDatabaseService, IDisposable
    {
        private readonly LiteDatabase _db;
        private readonly ICurrentWorkspace _workspace;
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
        private const string HabitsCollection           = "habits";
        private const string HabitCompletionsCollection = "habit_completions";
        private const string HabitCategoriesCollection  = "habit_categories";
        private const string HabitTemplatesCollection   = "habit_templates";
        private const string NotesCollection             = "notes";
        private const string MoodEntriesCollection       = "mood_entries";
        private const string MoodActivitiesCollection    = "mood_activities";
        private const string UserSoundsCollection        = "userSounds";
        private const string MediaItemsCollection        = "media_items";

        public DatabaseService(ICurrentWorkspace workspace)
        {
            _workspace = workspace;

            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FocusFlow");
            Directory.CreateDirectory(folder);
            var dbPath = Path.Combine(folder, "FocusFlowFinal.db");

            var password = LiteDbEncryption.GetOrCreateDbPassword();
            _db = LiteDbEncryption.Open(dbPath, password);

            _db.GetCollection<CalendarEvent>(EventsCollection).EnsureIndex(e => e.Start);
            _db.GetCollection<TaskItem>(TasksCollection).EnsureIndex(t => t.DueDate);
            _db.GetCollection<FocusSession>(SessionsCollection).EnsureIndex(s => s.StartTime);
            _db.GetCollection<Note>(NotesCollection).EnsureIndex(n => n.Date);
            _db.GetCollection<Note>(NotesCollection).EnsureIndex(n => n.UpdatedAt);
            _db.GetCollection<MoodEntry>(MoodEntriesCollection).EnsureIndex(e => e.Date);

            var templates = _db.GetCollection<TimerTemplate>(TemplatesCollection);
            templates.EnsureIndex(t => t.Name);
            DoSeedDefaultTimerTemplates(templates);

            MigrateNullUserIds();
        }

        private void MigrateNullUserIds()
        {
            const string local = CurrentWorkspaceService.LocalOwner;

            // Tasks
            var taskCol = _db.GetCollection<TaskItem>(TasksCollection);
            foreach (var t in taskCol.FindAll().Where(t => t.UserId == null).ToList())
            { t.UserId = local; taskCol.Update(t); }

            // Calendar events
            var evCol = _db.GetCollection<CalendarEvent>(EventsCollection);
            foreach (var e in evCol.FindAll().Where(e => e.UserId == null).ToList())
            { e.UserId = local; evCol.Update(e); }

            // Projects
            var prjCol = _db.GetCollection<ProjectItem>("projects");
            foreach (var p in prjCol.FindAll().Where(p => p.UserId == null).ToList())
            { p.UserId = local; prjCol.Update(p); }

            // Focus sessions
            var sesCol = _db.GetCollection<FocusSession>(SessionsCollection);
            foreach (var s in sesCol.FindAll().Where(s => s.UserId == null).ToList())
            { s.UserId = local; sesCol.Update(s); }

            // Habits
            var habCol = _db.GetCollection<Habit>(HabitsCollection);
            foreach (var h in habCol.FindAll().Where(h => h.UserId == null).ToList())
            { h.UserId = local; habCol.Update(h); }

            // Habit completions
            var compCol = _db.GetCollection<HabitCompletion>(HabitCompletionsCollection);
            foreach (var c in compCol.FindAll().Where(c => c.UserId == null).ToList())
            { c.UserId = local; compCol.Update(c); }

            // Notes
            var noteCol = _db.GetCollection<Note>(NotesCollection);
            foreach (var n in noteCol.FindAll().Where(n => n.UserId == null).ToList())
            { n.UserId = local; noteCol.Update(n); }

            // Mood entries
            var moodCol = _db.GetCollection<MoodEntry>(MoodEntriesCollection);
            foreach (var m in moodCol.FindAll().Where(m => m.UserId == null).ToList())
            { m.UserId = local; moodCol.Update(m); }

            // Finance
            var incomeCol = _db.GetCollection<FinanceIncome>(IncomeCollection);
            foreach (var x in incomeCol.FindAll().Where(x => x.UserId == null).ToList())
            { x.UserId = local; incomeCol.Update(x); }

            var expCol = _db.GetCollection<FinanceExpense>(ExpenseCollection);
            foreach (var x in expCol.FindAll().Where(x => x.UserId == null).ToList())
            { x.UserId = local; expCol.Update(x); }

            var fsCol = _db.GetCollection<FinanceSubscriptionItem>(FinSubCollection);
            foreach (var x in fsCol.FindAll().Where(x => x.UserId == null).ToList())
            { x.UserId = local; fsCol.Update(x); }

            var loanCol = _db.GetCollection<FinanceLoan>(LoanCollection);
            foreach (var x in loanCol.FindAll().Where(x => x.UserId == null).ToList())
            { x.UserId = local; loanCol.Update(x); }

            var savCol = _db.GetCollection<SavingsAccount>(SavingsCollection);
            foreach (var x in savCol.FindAll().Where(x => x.UserId == null).ToList())
            { x.UserId = local; savCol.Update(x); }

            // Media
            var mediaCol = _db.GetCollection<MediaItem>(MediaItemsCollection);
            foreach (var x in mediaCol.FindAll().Where(x => x.UserId == null).ToList())
            { x.UserId = local; mediaCol.Update(x); }

            // User sounds
            var soundCol = _db.GetCollection<UserSound>(UserSoundsCollection);
            foreach (var x in soundCol.FindAll().Where(x => x.UserId == null).ToList())
            { x.UserId = local; soundCol.Update(x); }
        }

        // Идемпотентный сидинг 3 стандартных шаблонов таймера (проверка по имени)
        private static void DoSeedDefaultTimerTemplates(LiteDB.ILiteCollection<TimerTemplate> col)
        {
            var defaults = new[]
            {
                new TimerTemplate { Name = "Рутина 25/5",  WorkMinutes = 25, BreakMinutes = 5,  Cycles = 4, IsBuiltIn = true },
                new TimerTemplate { Name = "Учёба 50/10",  WorkMinutes = 50, BreakMinutes = 10, Cycles = 3, IsBuiltIn = true },
                new TimerTemplate { Name = "Код 90/20",    WorkMinutes = 90, BreakMinutes = 20, Cycles = 2, IsBuiltIn = true },
            };
            foreach (var d in defaults)
            {
                if (!col.Exists(t => t.Name == d.Name))
                    col.Insert(d);
            }
        }

        public void SeedDefaultTimerTemplates()
        {
            var col = _db.GetCollection<TimerTemplate>(TemplatesCollection);
            DoSeedDefaultTimerTemplates(col);
        }

        public void Dispose() => _db.Dispose();

        // ── Общий доступ к коллекции (для специализированных репозиториев) ──
        public ILiteCollection<T> GetCollection<T>(string name) =>
            _db.GetCollection<T>(name);

        // ── Календарные события ─────────────────────────────────────────

        public IEnumerable<CalendarEvent> GetEvents(DateTime date)
        {
            try
            {
                var col        = _db.GetCollection<CalendarEvent>(EventsCollection);
                var startOfDay = date.Date;
                var endOfDay   = startOfDay.AddDays(1);
                var owner      = _workspace.CurrentOwnerKey;

                var rawEvents = col.Find(e =>
                    e.UserId == owner &&
                    ((e.Start >= startOfDay && e.Start < endOfDay && e.Recurrence == RecurrenceType.None) ||
                     (e.Recurrence != RecurrenceType.None && e.Start < endOfDay))
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
            var col   = _db.GetCollection<CalendarEvent>(EventsCollection);
            var owner = _workspace.CurrentOwnerKey;
            return col.Find(e => e.UserId == owner && e.Start >= start && e.End <= end).ToList();
        }

        public int CountBaseEvents()
        {
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<CalendarEvent>(EventsCollection).Count(e => e.UserId == owner);
        }

        public void UpsertEvent(CalendarEvent ev)
        {
            var col = _db.GetCollection<CalendarEvent>(EventsCollection);
            if (ev.Id == 0)
            {
                if (ev.UserId == null) ev.UserId = _workspace.CurrentOwnerKey;
                col.Insert(ev);
            }
            else
            {
                col.Update(ev);
            }
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
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<TaskItem>(TasksCollection)
                      .Find(t => t.UserId == owner)
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
            if (task.Id == 0)
            {
                if (task.UserId == null) task.UserId = _workspace.CurrentOwnerKey;
                col.Insert(task);
            }
            else
            {
                col.Update(task);
            }
        }

        public void DeleteTask(int id)
        {
            _db.GetCollection<TaskItem>(TasksCollection).Delete(id);
            _db.GetCollection<CalendarEvent>(EventsCollection).DeleteMany(e => e.TaskId == id);
        }

        public IEnumerable<TaskItem> GetTasksByDate(DateTime date)
        {
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<TaskItem>(TasksCollection)
                      .Find(t => t.UserId == owner && t.DueDate == date.Date)
                      .ToList();
        }

        public IEnumerable<TaskItem> GetTasksForPeriod(DateTime start, DateTime end)
        {
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<TaskItem>(TasksCollection)
                      .Find(t => t.UserId == owner && t.DueDate >= start.Date && t.DueDate < end.Date)
                      .ToList();
        }

        // ── Сессии фокусировки ──────────────────────────────────────────

        public void AddFocusSession(FocusSession session)
        {
            if (session.UserId == null) session.UserId = _workspace.CurrentOwnerKey;
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
            var owner = _workspace.CurrentOwnerKey;
            var start = date.Date;
            var end   = start.AddDays(1);
            return _db.GetCollection<FocusSession>(SessionsCollection)
                      .Find(s => s.UserId == owner && s.StartTime >= start && s.StartTime < end)
                      .ToList();
        }

        public IEnumerable<FocusSession> GetSessionsForPeriod(DateTime start, DateTime end)
        {
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<FocusSession>(SessionsCollection)
                      .Find(s => s.UserId == owner && s.StartTime >= start.Date && s.StartTime < end.Date)
                      .ToList();
        }

        public FocusSession? GetActiveSession()
        {
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<FocusSession>(SessionsCollection)
                      .Find(s => s.UserId == owner && s.EndTime == null)
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
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<ProjectItem>("projects").Find(p => p.UserId == owner).ToList();
        }

        public void UpsertProject(ProjectItem project)
        {
            var col = _db.GetCollection<ProjectItem>("projects");
            if (project.Id == 0)
            {
                if (project.UserId == null) project.UserId = _workspace.CurrentOwnerKey;
                col.Insert(project);
            }
            else
            {
                col.Update(project);
            }
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
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<FinanceIncome>(IncomeCollection)
                      .Find(x => x.UserId == owner).OrderByDescending(x => x.Date).ToList();
        }

        public void UpsertIncome(FinanceIncome item)
        {
            var col = _db.GetCollection<FinanceIncome>(IncomeCollection);
            if (item.Id == 0) { if (item.UserId == null) item.UserId = _workspace.CurrentOwnerKey; col.Insert(item); }
            else col.Update(item);
        }

        public void DeleteIncome(int id)
        {
            _db.GetCollection<FinanceIncome>(IncomeCollection).Delete(id);
        }

        public IEnumerable<FinanceExpense> GetAllExpenses()
        {
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<FinanceExpense>(ExpenseCollection)
                      .Find(x => x.UserId == owner).OrderByDescending(x => x.Date).ToList();
        }

        public void UpsertExpense(FinanceExpense item)
        {
            var col = _db.GetCollection<FinanceExpense>(ExpenseCollection);
            if (item.Id == 0) { if (item.UserId == null) item.UserId = _workspace.CurrentOwnerKey; col.Insert(item); }
            else col.Update(item);
        }

        public void DeleteExpense(int id)
        {
            _db.GetCollection<FinanceExpense>(ExpenseCollection).Delete(id);
        }

        public IEnumerable<FinanceSubscriptionItem> GetAllFinanceSubscriptions()
        {
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<FinanceSubscriptionItem>(FinSubCollection)
                      .Find(x => x.UserId == owner).OrderBy(x => x.Name).ToList();
        }

        public void UpsertFinanceSubscription(FinanceSubscriptionItem item)
        {
            var col = _db.GetCollection<FinanceSubscriptionItem>(FinSubCollection);
            if (item.Id == 0) { if (item.UserId == null) item.UserId = _workspace.CurrentOwnerKey; col.Insert(item); }
            else col.Update(item);
        }

        public void DeleteFinanceSubscription(int id)
        {
            _db.GetCollection<FinanceSubscriptionItem>(FinSubCollection).Delete(id);
        }

        public IEnumerable<FinanceLoan> GetAllLoans()
        {
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<FinanceLoan>(LoanCollection)
                      .Find(x => x.UserId == owner).OrderBy(x => x.Name).ToList();
        }

        public void UpsertLoan(FinanceLoan item)
        {
            var col = _db.GetCollection<FinanceLoan>(LoanCollection);
            if (item.Id == 0) { if (item.UserId == null) item.UserId = _workspace.CurrentOwnerKey; col.Insert(item); }
            else col.Update(item);
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
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<SavingsAccount>(SavingsCollection)
                      .Find(x => x.UserId == owner).OrderBy(x => x.Name).ToList();
        }

        public void UpsertSavingsAccount(SavingsAccount account)
        {
            var col = _db.GetCollection<SavingsAccount>(SavingsCollection);
            if (account.Id == 0) { if (account.UserId == null) account.UserId = _workspace.CurrentOwnerKey; col.Insert(account); }
            else col.Update(account);
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

        // ── Привычки ────────────────────────────────────────────────────

        public IEnumerable<Habit> GetAllHabits()
        {
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<Habit>(HabitsCollection)
                      .Find(h => h.UserId == owner).OrderBy(h => h.Name).ToList();
        }

        public void UpsertHabit(Habit habit)
        {
            var col = _db.GetCollection<Habit>(HabitsCollection);
            if (habit.Id == 0)
            {
                if (habit.UserId == null) habit.UserId = _workspace.CurrentOwnerKey;
                col.Insert(habit);
            }
            else col.Update(habit);
        }

        public void DeleteHabit(int id)
        {
            _db.GetCollection<Habit>(HabitsCollection).Delete(id);
            _db.GetCollection<HabitCompletion>(HabitCompletionsCollection).DeleteMany(c => c.HabitId == id);
        }

        public IEnumerable<HabitCompletion> GetHabitCompletions(int habitId)
        {
            return _db.GetCollection<HabitCompletion>(HabitCompletionsCollection)
                      .Find(c => c.HabitId == habitId)
                      .OrderByDescending(c => c.Date).ToList();
        }

        public IEnumerable<HabitCompletion> GetCompletionsForPeriod(DateTime start, DateTime end)
        {
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<HabitCompletion>(HabitCompletionsCollection)
                      .Find(c => c.UserId == owner && c.Date >= start && c.Date <= end).ToList();
        }

        public bool HasCompletionForDate(int habitId, DateTime date)
        {
            var day = date.Date;
            var next = day.AddDays(1);
            return _db.GetCollection<HabitCompletion>(HabitCompletionsCollection)
                      .Exists(c => c.HabitId == habitId && c.Date >= day && c.Date < next);
        }

        public void UpsertHabitCompletion(HabitCompletion completion)
        {
            var col = _db.GetCollection<HabitCompletion>(HabitCompletionsCollection);
            if (completion.Id == 0)
            {
                if (completion.UserId == null) completion.UserId = _workspace.CurrentOwnerKey;
                col.Insert(completion);
            }
            else col.Update(completion);
        }

        public HabitCompletion? GetCompletionForDate(int habitId, DateTime date)
        {
            var day  = date.Date;
            var next = day.AddDays(1);
            return _db.GetCollection<HabitCompletion>(HabitCompletionsCollection)
                      .FindOne(c => c.HabitId == habitId && c.Date >= day && c.Date < next);
        }

        public void DeleteHabitCompletion(int id)
        {
            _db.GetCollection<HabitCompletion>(HabitCompletionsCollection).Delete(id);
        }

        public void AutoCompleteHabitsForTask(int taskId)
        {
            var today  = DateTime.Today;
            var habits = _db.GetCollection<Habit>(HabitsCollection)
                            .Find(h => h.LinkedTaskId == taskId && !h.IsArchived).ToList();
            foreach (var h in habits)
            {
                if (HasCompletionForDate(h.Id, today)) continue;
                var comp = new HabitCompletion { HabitId = h.Id, Date = today, Status = 2 };
                _db.GetCollection<HabitCompletion>(HabitCompletionsCollection).Insert(comp);
                h.TotalCompletions++;
                UpdateHabitStreak(h, today);
                _db.GetCollection<Habit>(HabitsCollection).Update(h);
            }
        }

        private static void UpdateHabitStreak(Habit h, DateTime today)
        {
            var last = h.LastCompletedDate?.Date;
            if (last == null || last < today.AddDays(-1)) h.CurrentStreak = 1;
            else if (last == today.AddDays(-1))           h.CurrentStreak++;
            if (h.CurrentStreak > h.BestStreak)           h.BestStreak = h.CurrentStreak;
            h.LastCompletedDate = today;
        }

        public IEnumerable<HabitCategory> GetAllHabitCategories()
        {
            return _db.GetCollection<HabitCategory>(HabitCategoriesCollection)
                      .FindAll().OrderBy(c => c.Name).ToList();
        }

        public void UpsertHabitCategory(HabitCategory category)
        {
            var col = _db.GetCollection<HabitCategory>(HabitCategoriesCollection);
            if (category.Id == 0) col.Insert(category); else col.Update(category);
        }

        public void DeleteHabitCategory(int id)
        {
            _db.GetCollection<HabitCategory>(HabitCategoriesCollection).Delete(id);
        }

        // ── Шаблоны привычек ────────────────────────────────────────────────

        public IEnumerable<HabitTemplate> GetAllHabitTemplates()
        {
            var col = _db.GetCollection<HabitTemplate>(HabitTemplatesCollection);
            if (col.Count() == 0) SeedHabitTemplates(col);
            return col.FindAll().OrderBy(t => t.IsSystem ? 0 : 1).ThenBy(t => t.Name).ToList();
        }

        public void UpsertHabitTemplate(HabitTemplate template)
        {
            var col = _db.GetCollection<HabitTemplate>(HabitTemplatesCollection);
            if (template.Id == 0) col.Insert(template); else col.Update(template);
        }

        public void DeleteHabitTemplate(int id)
        {
            _db.GetCollection<HabitTemplate>(HabitTemplatesCollection).Delete(id);
        }

        private static void SeedHabitTemplates(ILiteCollection<HabitTemplate> col)
        {
            col.InsertBulk(new[]
            {
                new HabitTemplate { Name = "Утренняя зарядка", Description = "15 минут упражнений утром",
                    Category = "Спорт",        Icon = "🏃", Color = "#F97316",
                    RepetitionType = HabitRepetitionType.Daily, IsSystem = true },
                new HabitTemplate { Name = "Чтение книги",     Description = "Читать не менее 20 страниц",
                    Category = "Саморазвитие", Icon = "📚", Color = "#8B5CF6",
                    RepetitionType = HabitRepetitionType.Daily, IsSystem = true },
                new HabitTemplate { Name = "Изучение языка",   Description = "Занятие иностранным языком",
                    Category = "Учёба",        Icon = "🎓", Color = "#3B82F6",
                    RepetitionType = HabitRepetitionType.WeekDays,
                    WeekDaysList   = new System.Collections.Generic.List<int> { 0,1,2,3,4 }, IsSystem = true },
                new HabitTemplate { Name = "Пить воду",        Description = "Выпивать 8 стаканов воды в день",
                    Category = "Здоровье",     Icon = "💧", Color = "#06B6D4",
                    RepetitionType = HabitRepetitionType.Daily, IsSystem = true },
                new HabitTemplate { Name = "Медитация",        Description = "10 минут медитации",
                    Category = "Здоровье",     Icon = "🧘", Color = "#10B981",
                    RepetitionType = HabitRepetitionType.Daily, IsSystem = true },
                new HabitTemplate { Name = "Отжимания",        Description = "Минимум 3 подхода",
                    Category = "Спорт",        Icon = "💪", Color = "#EF4444",
                    RepetitionType = HabitRepetitionType.TimesPerWeek, TimesPerWeek = 3, IsSystem = true },
            });
        }

        private static void SeedHabitCategories(ILiteCollection<HabitCategory> col)
        {
            var defaults = new[]
            {
                new HabitCategory { Name = "Здоровье",       Icon = "❤️",  Color = "#EF4444", IsSystem = true },
                new HabitCategory { Name = "Спорт",          Icon = "🏃",  Color = "#F97316", IsSystem = true },
                new HabitCategory { Name = "Саморазвитие",   Icon = "📚",  Color = "#8B5CF6", IsSystem = true },
                new HabitCategory { Name = "Учёба",          Icon = "🎓",  Color = "#3B82F6", IsSystem = true },
                new HabitCategory { Name = "Работа",         Icon = "💼",  Color = "#6B7280", IsSystem = true },
                new HabitCategory { Name = "Финансы",        Icon = "💰",  Color = "#22C55E", IsSystem = true },
                new HabitCategory { Name = "Дом",            Icon = "🏠",  Color = "#F59E0B", IsSystem = true },
                new HabitCategory { Name = "Другое",         Icon = "⭐",  Color = "#6B7280", IsSystem = true },
            };
            col.InsertBulk(defaults);
        }

        // ── Заметки и дневник ───────────────────────────────────────────

        public IEnumerable<Note> GetAllNotes()
        {
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<Note>(NotesCollection)
                      .Find(n => n.UserId == owner)
                      .OrderByDescending(n => n.UpdatedAt)
                      .ToList();
        }

        public Note? GetNoteById(int id) =>
            _db.GetCollection<Note>(NotesCollection).FindById(id);

        public int UpsertNote(Note note)
        {
            note.UpdatedAt = DateTime.Now;
            var col = _db.GetCollection<Note>(NotesCollection);
            if (note.Id == 0)
            {
                note.CreatedAt = DateTime.Now;
                if (note.UserId == null) note.UserId = _workspace.CurrentOwnerKey;
                return col.Insert(note).AsInt32;
            }
            col.Update(note);
            return note.Id;
        }

        public void DeleteNote(int id) =>
            _db.GetCollection<Note>(NotesCollection).Delete(id);

        public HashSet<DateTime> GetNoteDates(DateTime from, DateTime to)
        {
            var owner    = _workspace.CurrentOwnerKey;
            var fromDate = from.Date;
            var toDate   = to.Date;
            var dates = _db.GetCollection<Note>(NotesCollection)
                .Find(n => n.UserId == owner)
                .Where(n => n.Date.Date >= fromDate && n.Date.Date <= toDate)
                .Select(n => n.Date.Date);
            return new HashSet<DateTime>(dates);
        }

        public IEnumerable<string> GetAllNoteTags()
        {
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<Note>(NotesCollection)
                      .Find(n => n.UserId == owner)
                      .SelectMany(n => n.Tags)
                      .Distinct()
                      .OrderBy(t => t)
                      .ToList();
        }

        public IEnumerable<Note> SearchNotes(string? query, string? tag, DateTime? from, DateTime? to)
        {
            var owner = _workspace.CurrentOwnerKey;
            var all = _db.GetCollection<Note>(NotesCollection).Find(n => n.UserId == owner);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.ToLower();
                all = all.Where(n => n.Title.ToLower().Contains(q) || n.MarkdownContent.ToLower().Contains(q));
            }

            if (!string.IsNullOrWhiteSpace(tag))
                all = all.Where(n => n.Tags.Contains(tag));

            if (from.HasValue)
                all = all.Where(n => n.Date.Date >= from.Value.Date);

            if (to.HasValue)
                all = all.Where(n => n.Date.Date <= to.Value.Date);

            return all.OrderByDescending(n => n.UpdatedAt).ToList();
        }

        // ── Трекер настроения ────────────────────────────────────────────

        public IEnumerable<MoodEntry> GetAllMoodEntries()
        {
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<MoodEntry>(MoodEntriesCollection)
                      .Find(e => e.UserId == owner).OrderByDescending(e => e.Date).ToList();
        }

        public IEnumerable<MoodEntry> GetMoodEntriesForPeriod(DateTime from, DateTime to)
        {
            var owner = _workspace.CurrentOwnerKey;
            var f = from.Date; var t = to.Date;
            return _db.GetCollection<MoodEntry>(MoodEntriesCollection)
                .Find(e => e.UserId == owner)
                .Where(e => e.Date.Date >= f && e.Date.Date <= t)
                .OrderByDescending(e => e.Date).ToList();
        }

        public MoodEntry? GetMoodEntryById(int id) =>
            _db.GetCollection<MoodEntry>(MoodEntriesCollection).FindById(id);

        public int UpsertMoodEntry(MoodEntry entry)
        {
            var col = _db.GetCollection<MoodEntry>(MoodEntriesCollection);
            if (entry.Id == 0)
            {
                entry.CreatedAt = DateTime.Now;
                if (entry.UserId == null) entry.UserId = _workspace.CurrentOwnerKey;
                return col.Insert(entry).AsInt32;
            }
            col.Update(entry); return entry.Id;
        }

        public void DeleteMoodEntry(int id) =>
            _db.GetCollection<MoodEntry>(MoodEntriesCollection).Delete(id);

        public IEnumerable<MoodActivity> GetAllMoodActivities()
        {
            var col = _db.GetCollection<MoodActivity>(MoodActivitiesCollection);
            if (col.Count() == 0) SeedMoodActivities(col);
            return col.FindAll().OrderBy(a => a.Category).ThenBy(a => a.Name).ToList();
        }

        public int UpsertMoodActivity(MoodActivity activity)
        {
            var col = _db.GetCollection<MoodActivity>(MoodActivitiesCollection);
            if (activity.Id == 0) { activity.CreatedAt = DateTime.Now; return col.Insert(activity).AsInt32; }
            col.Update(activity); return activity.Id;
        }

        public void DeleteMoodActivity(int id) =>
            _db.GetCollection<MoodActivity>(MoodActivitiesCollection).Delete(id);

        private static void SeedMoodActivities(ILiteCollection<MoodActivity> col)
        {
            var seed = new[]
            {
                ("Сон",         "Долгий сон",          "😴"), ("Сон",         "Тихий вечер",       "🌙"),
                ("Сон",         "Дремота",             "💤"), ("Сон",         "Ранний подъём",      "⏰"),
                ("Совместные",  "Друзья",              "👥"), ("Совместные",  "Семья",              "👨‍👩‍👧"),
                ("Совместные",  "Свидание",            "❤️"), ("Совместные",  "Вечеринка",          "🎉"),
                ("Совместные",  "Видеозвонок",         "💻"),
                ("Хобби",       "Музыка",              "🎵"), ("Хобби",       "Кино",               "🎬"),
                ("Хобби",       "Игры",                "🎮"), ("Хобби",       "Чтение",             "📚"),
                ("Хобби",       "Рисование",           "🎨"), ("Хобби",       "Кулинария",          "👨‍🍳"),
                ("Еда",         "Домашняя еда",        "🍲"), ("Еда",         "Ресторан",           "🍽️"),
                ("Еда",         "Фастфуд",             "🍔"), ("Еда",         "Вегетарианское",     "🥗"),
                ("Спорт",       "Бег",                 "🏃"), ("Спорт",       "Зал",                "💪"),
                ("Спорт",       "Йога",                "🧘"), ("Спорт",       "Велосипед",          "🚴"),
                ("Спорт",       "Плавание",            "🏊"), ("Спорт",       "Прогулка",           "🚶"),
                ("Работа",      "Продуктивный день",   "⚡"), ("Работа",      "Дедлайн",            "⏰"),
                ("Работа",      "Встречи",             "🤝"), ("Работа",      "Удалёнка",           "🏠"),
                ("Здоровье",    "Болезнь",             "🤒"), ("Здоровье",    "Хорошее самочувствие","💪"),
                ("Здоровье",    "Врач",                "🏥"), ("Здоровье",    "Медитация",          "🧘"),
                ("Другое",      "Путешествие",         "✈️"), ("Другое",      "Покупки",            "🛍️"),
                ("Другое",      "Уборка",              "🧹"), ("Другое",      "Природа",            "🌿"),
            };
            col.InsertBulk(seed.Select(s => new MoodActivity
            {
                Category = s.Item1, Name = s.Item2, Icon = s.Item3, IsCustom = false,
                CreatedAt = DateTime.Now
            }));
        }

        // ── Пользовательские звуки ───────────────────────────────────────

        public IEnumerable<UserSound> GetAllUserSounds()
        {
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<UserSound>(UserSoundsCollection).Find(x => x.UserId == owner).ToList();
        }

        public int UpsertUserSound(UserSound sound)
        {
            var col = _db.GetCollection<UserSound>(UserSoundsCollection);
            if (sound.Id == 0)
            {
                if (sound.UserId == null) sound.UserId = _workspace.CurrentOwnerKey;
                return col.Insert(sound).AsInt32;
            }
            col.Update(sound);
            return sound.Id;
        }

        public void DeleteUserSound(int id) =>
            _db.GetCollection<UserSound>(UserSoundsCollection).Delete(id);

        // ── Трекер медиа ────────────────────────────────────────────────

        public IEnumerable<MediaItem> GetAllMediaItems()
        {
            var owner = _workspace.CurrentOwnerKey;
            return _db.GetCollection<MediaItem>(MediaItemsCollection)
                      .Find(x => x.UserId == owner)
                      .OrderByDescending(x => x.CreatedAt)
                      .ToList();
        }

        public MediaItem? GetMediaItemById(int id) =>
            _db.GetCollection<MediaItem>(MediaItemsCollection).FindById(id);

        public int UpsertMediaItem(MediaItem item)
        {
            var col = _db.GetCollection<MediaItem>(MediaItemsCollection);
            if (item.Id == 0)
            {
                if (item.UserId == null) item.UserId = _workspace.CurrentOwnerKey;
                return col.Insert(item).AsInt32;
            }
            col.Update(item);
            return item.Id;
        }

        public void DeleteMediaItem(int id) =>
            _db.GetCollection<MediaItem>(MediaItemsCollection).Delete(id);
    }
}

