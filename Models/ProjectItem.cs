using CommunityToolkit.Mvvm.ComponentModel;
using LiteDB;
using System;

namespace FocusFlowFinal.Models;

public class ProjectItem : ISyncableEntity
{
    [BsonId]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B82F6";

    // ИСПРАВЛЕНО (Часть 2-3, п.3): поля синхронизации
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
}
