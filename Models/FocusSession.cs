using LiteDB;
using System;

namespace FocusFlowFinal.Models;

public class FocusSession
{
    [BsonId]
    public int Id { get; set; }
    public int? TaskId { get; set; }          // Связанная задача (может быть null)
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }    // null, если сессия ещё идёт
    public int PlannedMinutes { get; set; }   // Запланированная длительность
    public int ActualMinutes { get; set; }    // Фактическая (заполняется при завершении)
    public bool IsCompleted { get; set; }
}