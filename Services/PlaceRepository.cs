using FocusFlowFinal.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FocusFlowFinal.Services;

public class PlaceRepository : IPlaceRepository
{
    private readonly string _dbPath;

    public PlaceRepository()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FocusFlow");
        Directory.CreateDirectory(folder);
        _dbPath = Path.Combine(folder, "places.db");
    }

    private LiteDatabase Open() => new LiteDatabase(_dbPath);

    public List<PlaceItem> GetAll()
    {
        using var db = Open();
        return db.GetCollection<PlaceItem>("places")
            .Find(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToList();
    }

    public PlaceItem? Get(int id)
    {
        using var db = Open();
        return db.GetCollection<PlaceItem>("places").FindById(id);
    }

    public void Upsert(PlaceItem place)
    {
        if (place.SyncId == Guid.Empty) place.SyncId = Guid.NewGuid();
        place.UpdatedAt = DateTime.UtcNow;
        using var db = Open();
        db.GetCollection<PlaceItem>("places").Upsert(place);
    }

    public void Delete(int id)
    {
        using var db = Open();
        var col  = db.GetCollection<PlaceItem>("places");
        var item = col.FindById(id);
        if (item == null) return;
        item.IsDeleted = true;
        col.Update(item);
    }
}
