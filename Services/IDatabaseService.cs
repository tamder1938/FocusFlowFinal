using FocusFlowFinal.Models;
using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public interface IDatabaseService
{
    // События календаря
    IEnumerable<CalendarEvent> GetEvents(DateTime date);
    IEnumerable<CalendarEvent> GetEventsForPeriod(DateTime start, DateTime end);
    void UpsertEvent(CalendarEvent ev);
    void DeleteEvent(int id);
    CalendarEvent? GetEventById(int id);
    void ExcludeDateFromEvent(int eventId, DateTime date);
    CalendarEvent? FindOriginalSeries(CalendarEvent virtualEvent); // добавлено

    // Задачи
    IEnumerable<TaskItem> GetAllTasks();
    TaskItem? GetTask(int id);
    void UpsertTask(TaskItem task);
    void DeleteTask(int id);
    IEnumerable<TaskItem> GetTasksByDate(DateTime date);
    IEnumerable<TaskItem> GetTasksForPeriod(DateTime start, DateTime end);

    // Отображение (события + задачи)
    IEnumerable<CalendarEvent> GetEventsForDisplay(DateTime date);

    // Удаление события, связанного с задачей
    void DeleteEventForTask(int taskId);

    // Сессии фокусировки
    void AddFocusSession(FocusSession session);
    void UpdateFocusSession(FocusSession session);
    IEnumerable<FocusSession> GetSessionsForTask(int taskId);
    IEnumerable<FocusSession> GetSessionsForDate(DateTime date);
    IEnumerable<FocusSession> GetSessionsForPeriod(DateTime start, DateTime end);
    FocusSession? GetActiveSession();

    // Шаблоны таймера
    IEnumerable<TimerTemplate> GetAllTimerTemplates();
    TimerTemplate? GetTimerTemplate(int id);
    void UpsertTimerTemplate(TimerTemplate template);
    void DeleteTimerTemplate(int id);

    // Проекты
    IEnumerable<ProjectItem> GetAllProjects();
    void UpsertProject(ProjectItem project);
    void DeleteProject(int id);
}