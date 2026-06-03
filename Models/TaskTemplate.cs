using LiteDB;

namespace FocusFlowFinal.Models;

public class TaskTemplate
{
    [BsonId]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? Priority { get; set; }
    public int? ProjectId { get; set; }
    public int PlannedDurationMinutes { get; set; }
    public bool HasDate { get; set; }
    public bool IsTimeBound { get; set; }
    public int StartHour { get; set; } = 9;
    public int StartMinute { get; set; }
}