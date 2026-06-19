using LiteDB;
using System.Collections.Generic;

namespace FocusFlowFinal.Models.Habits;

public class HabitTemplate
{
    [BsonId]
    public int Id { get; set; }

    public string Name        { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category    { get; set; } = string.Empty;
    public string Icon        { get; set; } = "⭐";
    public string Color       { get; set; } = "#3B82F6";

    public HabitRepetitionType RepetitionType { get; set; } = HabitRepetitionType.Daily;
    public List<int> WeekDaysList  { get; set; } = new();
    public int TimesPerWeek        { get; set; } = 3;
    public int TimesPerMonth       { get; set; } = 10;

    /// <summary>Предустановленный шаблон — нельзя удалить.</summary>
    public bool IsSystem { get; set; }
}
