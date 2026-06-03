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

    public string Project { get; set; } = "Работа"; // совместимость
}