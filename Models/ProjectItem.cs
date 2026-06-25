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

    // поля синхронизации
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public string? UserId { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
}
