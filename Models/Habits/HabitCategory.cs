using LiteDB;

namespace FocusFlowFinal.Models.Habits;

public class HabitCategory
{
    [BsonId]
    public int Id { get; set; }

    public string Name     { get; set; } = string.Empty;
    public string Icon     { get; set; } = "📁";
    public string Color    { get; set; } = "#6B7280";
    public bool IsSystem   { get; set; } = false;
}
