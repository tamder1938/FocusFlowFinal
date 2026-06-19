using LiteDB;
using System;

namespace FocusFlowFinal.Models.Habits;

public class HabitCompletion
{
    [BsonId]
    public int Id { get; set; }

    public int      HabitId { get; set; }
    public DateTime Date    { get; set; }
    public string   Note    { get; set; } = string.Empty;

    /// <summary>0=нет записи (не хранится), 1=частично, 2=выполнено (по умолчанию).</summary>
    public int Status { get; set; } = 2;

    [BsonIgnore]
    public string StatusText => Status == 1 ? "~ Частично" : "✓ Выполнено";

    [BsonIgnore]
    public string StatusColor => Status == 1 ? "#F59E0B" : "#22C55E";
}
