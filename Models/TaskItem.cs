using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LiteDB;
using FocusFlowFinal.Services;

namespace FocusFlowFinal.Models;

public class TaskItem : ISyncableEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int PlannedDurationMinutes { get; set; }
    public bool IsCompleted { get; set; }
    public int Priority { get; set; } = 1;   // 0 – высокий, 1 – средний, 2 – низкий
    public int? ProjectId { get; set; }
    public string? Color { get; set; }

    // поля синхронизации
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    [BsonIgnore]
    public string? ProjectColor { get; set; }

    // Подзадачи (хранятся как вложенный массив в LiteDB)
    public List<Subtask> Subtasks { get; set; } = new();

    [BsonIgnore]
    public string SubtasksProgress
    {
        get
        {
            if (Subtasks.Count == 0) return string.Empty;
            return $"{Subtasks.Count(s => s.IsCompleted)}/{Subtasks.Count}";
        }
    }

    [BsonIgnore]
    public bool HasSubtasks => Subtasks.Count > 0;

    // Отображаемые подзадачи — observable-обёртки, создаются TaskListViewModel
    [BsonIgnore]
    public ObservableCollection<SubtaskViewItem>? SubtaskViewItems { get; private set; }

    /// <summary>
    /// Инициализирует observable-обёртки подзадач для отображения в списке задач.
    /// Вызывается из TaskListViewModel после загрузки задач из БД.
    /// </summary>
    public void InitSubtaskViewItems(Action onSubtaskToggled)
    {
        SubtaskViewItems = new ObservableCollection<SubtaskViewItem>(
            Subtasks.Select(s => new SubtaskViewItem(s) { OnToggled = onSubtaskToggled })
        );
    }

    [BsonIgnore]
    public string DurationText => FormatDuration(PlannedDurationMinutes);

    public static string FormatDuration(int minutes)
    {
        if (minutes <= 0) return string.Empty;

        var loc = LocalizationService.Instance;
        string minWord  = loc["DurationMin"];
        string hourWord = loc["DurationHour"];

        if (minutes < 60)
            return $"{minutes} {minWord}";

        int hours = minutes / 60;
        int rest  = minutes % 60;

        return rest > 0
            ? $"{hours} {hourWord} {rest} {minWord}"
            : $"{hours} {hourWord}";
    }

    [BsonIgnore]
    public bool HasDuration => PlannedDurationMinutes > 0;

    [BsonIgnore]
    public bool CanStartTimer => HasDuration && !IsCompleted;
}
