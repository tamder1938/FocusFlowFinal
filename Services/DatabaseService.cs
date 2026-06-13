using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using FocusFlowFinal.Models;

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
                            isMatch = checkDate.Day == ev.Start.Day && checkDate.Month == ev.Start.Month;
                            break;

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

        public void DeleteEventForTask(int taskId)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<CalendarEvent>(EventsCollection);
            col.DeleteMany(e => e.TaskId == taskId);
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
