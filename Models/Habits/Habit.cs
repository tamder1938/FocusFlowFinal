using LiteDB;
using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Models.Habits;

public enum HabitRepetitionType
{
    Daily      = 0,
    WeekDays   = 1,
    TimesPerWeek  = 2,
    TimesPerMonth = 3
}

public class Habit
{
    [BsonId]
    public int Id { get; set; }

    public string Name        { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category    { get; set; } = string.Empty;
    public string Icon        { get; set; } = "⭐";
    public string Color       { get; set; } = "#3B82F6";

    public HabitRepetitionType RepetitionType { get; set; } = HabitRepetitionType.Daily;

    /// <summary>Дни недели (0=Пн, 6=Вс) для типа WeekDays.</summary>
    public List<int> WeekDaysList { get; set; } = new();

    /// <summary>Количество раз в неделю для типа TimesPerWeek.</summary>
    public int TimesPerWeek { get; set; } = 1;

    /// <summary>Количество раз в месяц для типа TimesPerMonth.</summary>
    public int TimesPerMonth { get; set; } = 1;

    public DateTime StartDate  { get; set; } = DateTime.Today;
    public bool IsArchived     { get; set; } = false;

    /// <summary>Id задачи, выполнение которой автоматически засчитывает привычку.</summary>
    public int? LinkedTaskId { get; set; }

    public int CurrentStreak      { get; set; } = 0;
    public int BestStreak         { get; set; } = 0;
    public int TotalCompletions   { get; set; } = 0;
    public DateTime? LastCompletedDate { get; set; }
}
