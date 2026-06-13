using FocusFlowFinal.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FocusFlowFinal.Services;

public interface ITemplateService
{
    IEnumerable<TaskTemplate> GetAllTaskTemplates();
    void UpsertTaskTemplate(TaskTemplate template);
    void DeleteTaskTemplate(int id);
    List<EventTemplate> GetEventTemplates();
    void SaveEventTemplate(EventTemplate template);
    void DeleteEventTemplate(int id);
}

public class TemplateService : ITemplateService
{
    private readonly string _dbPath;

    public TemplateService()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FocusFlow");
        Directory.CreateDirectory(folder);
        _dbPath = Path.Combine(folder, "templates.db");
    }

    private LiteDatabase GetDatabase() => new LiteDatabase(_dbPath);

    // === Задачи ===
    public IEnumerable<TaskTemplate> GetAllTaskTemplates()
    {
        using var db = GetDatabase();
        return db.GetCollection<TaskTemplate>("task_templates")
                 .FindAll()
                 .OrderBy(t => t.Name)
                 .ToList();
    }

    public void UpsertTaskTemplate(TaskTemplate template)
    {
        using var db = GetDatabase();
        var col = db.GetCollection<TaskTemplate>("task_templates");
        if (template.Id == 0)
            col.Insert(template);
        else
            col.Update(template);
    }

    public void DeleteTaskTemplate(int id)
    {
        using var db = GetDatabase();
        db.GetCollection<TaskTemplate>("task_templates").Delete(id);
    }

    // === События ===
    public List<EventTemplate> GetEventTemplates()
    {
        using var db = GetDatabase();
        return db.GetCollection<EventTemplate>("event_templates")
                 .FindAll()
                 .OrderBy(t => t.Name)
                 .ToList();
    }

    public void SaveEventTemplate(EventTemplate template)
    {
        using var db = GetDatabase();
        var col = db.GetCollection<EventTemplate>("event_templates");
        if (template.Id == 0)
            col.Insert(template);
        else
            col.Update(template);
    }

    public void DeleteEventTemplate(int id)
    {
        using var db = GetDatabase();
        db.GetCollection<EventTemplate>("event_templates").Delete(id);
    }
}