using FocusFlowFinal.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FocusFlowFinal.Services;

public interface ITemplateService
{
    // Для задач
    IEnumerable<TaskTemplate> GetAllTaskTemplates();
    void UpsertTaskTemplate(TaskTemplate template);
    void DeleteTaskTemplate(int id);

    // Для событий
    List<EventTemplate> GetEventTemplates();
    void SaveEventTemplate(EventTemplate template);
    void DeleteEventTemplate(int id);
}

public class TemplateService : ITemplateService
{
    private readonly LiteDatabase _database;

    public TemplateService()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FocusFlow");
        Directory.CreateDirectory(folder);
        var dbPath = Path.Combine(folder, "templates.db");
        _database = new LiteDatabase(dbPath);
    }

    // === Задачи ===
    public IEnumerable<TaskTemplate> GetAllTaskTemplates()
    {
        return _database.GetCollection<TaskTemplate>("task_templates")
                        .FindAll()
                        .OrderBy(t => t.Name)
                        .ToList();
    }

    public void UpsertTaskTemplate(TaskTemplate template)
    {
        var col = _database.GetCollection<TaskTemplate>("task_templates");
        if (template.Id == 0)
            col.Insert(template);
        else
            col.Update(template);
    }

    public void DeleteTaskTemplate(int id)
    {
        _database.GetCollection<TaskTemplate>("task_templates").Delete(id);
    }

    // === События ===
    public List<EventTemplate> GetEventTemplates()
    {
        return _database.GetCollection<EventTemplate>("event_templates")
                        .FindAll()
                        .OrderBy(t => t.Name)
                        .ToList();
    }

    public void SaveEventTemplate(EventTemplate template)
    {
        var col = _database.GetCollection<EventTemplate>("event_templates");
        if (template.Id == 0)
            col.Insert(template);
        else
            col.Update(template);
    }

    public void DeleteEventTemplate(int id)
    {
        _database.GetCollection<EventTemplate>("event_templates").Delete(id);
    }
}