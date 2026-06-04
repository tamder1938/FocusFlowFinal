using System;
using LiteDB;

namespace FocusFlowFinal.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public int PlannedDurationMinutes { get; set; }
    public bool IsCompleted { get; set; }
    public int Priority { get; set; } = 1;   // 0 – высокий, 1 – средний, 2 – низкий
    public int? ProjectId { get; set; }
    public string? Color { get; set; }

    [BsonIgnore]
    public string? ProjectColor { get; set; }

    public string Project { get; set; } = "Работа"; // совместимость

    [BsonIgnore]
    public string DurationText
    {
        get
        {
            if (PlannedDurationMinutes <= 0) return string.Empty;
            if (PlannedDurationMinutes < 60)
                return $"{PlannedDurationMinutes} мин";
            int hours = PlannedDurationMinutes / 60;
            int minutes = PlannedDurationMinutes % 60;
            return minutes > 0 ? $"{hours} ч {minutes} мин" : $"{hours} ч";
        }
    }

    // Свойство для триггера has-duration
    [BsonIgnore]
    public bool HasDuration => PlannedDurationMinutes > 0;
}