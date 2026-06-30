using FocusFlowFinal.Models.Wishlist;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FocusFlowFinal.Services;

public class WishlistRepository : IWishlistRepository
{
    private readonly string _dbPath;

    public WishlistRepository()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FocusFlow");
        Directory.CreateDirectory(folder);
        _dbPath = Path.Combine(folder, "wishlists.db");
    }

    private LiteDatabase Open() => new LiteDatabase(_dbPath);

    // ── Wishlists ─────────────────────────────────────────────────────────

    public IEnumerable<WishlistItem> GetAll()
    {
        using var db = Open();
        return db.GetCollection<WishlistItem>("wishlists")
                 .Find(w => !w.IsDeleted)
                 .OrderBy(w => w.CreatedAt)
                 .ToList();
    }

    public IEnumerable<WishlistItem> GetAllForSync()
    {
        using var db = Open();
        return db.GetCollection<WishlistItem>("wishlists").FindAll().ToList();
    }

    public WishlistItem? GetById(int id)
    {
        using var db = Open();
        return db.GetCollection<WishlistItem>("wishlists").FindById(id);
    }

    public WishlistItem? GetBySyncId(Guid syncId)
    {
        using var db = Open();
        return db.GetCollection<WishlistItem>("wishlists").FindOne(w => w.SyncId == syncId);
    }

    public int Upsert(WishlistItem wishlist)
    {
        using var db = Open();
        var col = db.GetCollection<WishlistItem>("wishlists");
        wishlist.UpdatedAt = DateTime.UtcNow;
        if (wishlist.SyncId == Guid.Empty) wishlist.SyncId = Guid.NewGuid();
        if (wishlist.Id == 0)
        {
            wishlist.CreatedAt = DateTime.UtcNow;
            return col.Insert(wishlist).AsInt32;
        }
        col.Update(wishlist);
        return wishlist.Id;
    }

    public void Delete(int id)
    {
        using var db = Open();
        var col = db.GetCollection<WishlistItem>("wishlists");
        var item = col.FindById(id);
        if (item == null) return;
        item.IsDeleted = true;
        item.UpdatedAt = DateTime.UtcNow;
        col.Update(item);
        // Soft-delete columns, rows, cells so sync can propagate removals
        SoftDeleteColumnsForWishlist(db, id);
        SoftDeleteRowsForWishlist(db, id);
    }

    private static void SoftDeleteColumnsForWishlist(LiteDatabase db, int wishlistId)
    {
        var col = db.GetCollection<WishlistColumn>("columns");
        var cols = col.Find(c => c.WishlistId == wishlistId).ToList();
        var now = DateTime.UtcNow;
        foreach (var c in cols) { c.IsDeleted = true; c.UpdatedAt = now; col.Update(c); }
    }

    private static void SoftDeleteRowsForWishlist(LiteDatabase db, int wishlistId)
    {
        var rows = db.GetCollection<WishlistRow>("rows");
        var cells = db.GetCollection<WishlistCell>("cells");
        var now = DateTime.UtcNow;
        foreach (var r in rows.Find(r => r.WishlistId == wishlistId).ToList())
        {
            r.IsDeleted = true; r.UpdatedAt = now; rows.Update(r);
            foreach (var c in cells.Find(c => c.RowId == r.Id).ToList())
            { c.UpdatedAt = now; cells.Update(c); }
        }
    }

    public void UpsertFromSync(WishlistItem wishlist)
    {
        using var db = Open();
        var col = db.GetCollection<WishlistItem>("wishlists");
        var existing = col.FindOne(w => w.SyncId == wishlist.SyncId);
        if (existing != null)
        {
            if (wishlist.UpdatedAt <= existing.UpdatedAt) return;
            wishlist.Id = existing.Id;
            col.Update(wishlist);
        }
        else
        {
            wishlist.Id = 0;
            col.Insert(wishlist);
        }
    }

    // ── Columns ──────────────────────────────────────────────────────────

    public List<WishlistColumn> GetColumns(int wishlistId)
    {
        using var db = Open();
        return db.GetCollection<WishlistColumn>("columns")
                 .Find(c => c.WishlistId == wishlistId && !c.IsDeleted)
                 .OrderBy(c => c.Order)
                 .ToList();
    }

    public IEnumerable<WishlistColumn> GetColumnsForSync(int wishlistId)
    {
        using var db = Open();
        return db.GetCollection<WishlistColumn>("columns")
                 .Find(c => c.WishlistId == wishlistId)
                 .ToList();
    }

    public WishlistColumn? GetColumnBySyncId(Guid syncId)
    {
        using var db = Open();
        return db.GetCollection<WishlistColumn>("columns").FindOne(c => c.SyncId == syncId);
    }

    public void SaveColumns(int wishlistId, IEnumerable<WishlistColumn> columns)
    {
        using var db = Open();
        var col = db.GetCollection<WishlistColumn>("columns");
        var existing = col.Find(c => c.WishlistId == wishlistId).ToDictionary(c => c.Name);

        // Soft-delete all current columns; reactivate or insert below
        var now = DateTime.UtcNow;
        foreach (var c in existing.Values) { c.IsDeleted = true; c.UpdatedAt = now; col.Update(c); }

        int order = 0;
        foreach (var c in columns)
        {
            c.WishlistId = wishlistId;
            c.Order = order++;
            c.UpdatedAt = now;
            c.IsDeleted = false;
            if (existing.TryGetValue(c.Name, out var prev))
            {
                // Reuse existing row + preserve SyncId
                c.Id = prev.Id;
                c.SyncId = prev.SyncId;
                col.Update(c);
            }
            else
            {
                c.Id = 0;
                if (c.SyncId == Guid.Empty) c.SyncId = Guid.NewGuid();
                col.Insert(c);
            }
        }
    }

    public void UpsertColumnFromSync(WishlistColumn col)
    {
        using var db = Open();
        var collection = db.GetCollection<WishlistColumn>("columns");
        var existing = collection.FindOne(c => c.SyncId == col.SyncId);
        if (existing != null)
        {
            if (col.UpdatedAt <= existing.UpdatedAt) return;
            col.Id = existing.Id;
            collection.Update(col);
        }
        else
        {
            col.Id = 0;
            collection.Insert(col);
        }
    }

    // ── Rows ─────────────────────────────────────────────────────────────

    public List<WishlistRow> GetRows(int wishlistId)
    {
        using var db = Open();
        return db.GetCollection<WishlistRow>("rows")
                 .Find(r => r.WishlistId == wishlistId && !r.IsDeleted)
                 .OrderBy(r => r.Order)
                 .ToList();
    }

    public IEnumerable<WishlistRow> GetRowsForSync(int wishlistId)
    {
        using var db = Open();
        return db.GetCollection<WishlistRow>("rows")
                 .Find(r => r.WishlistId == wishlistId)
                 .ToList();
    }

    public WishlistRow? GetRowBySyncId(Guid syncId)
    {
        using var db = Open();
        return db.GetCollection<WishlistRow>("rows").FindOne(r => r.SyncId == syncId);
    }

    public int UpsertRow(WishlistRow row)
    {
        using var db = Open();
        var col = db.GetCollection<WishlistRow>("rows");
        row.UpdatedAt = DateTime.UtcNow;
        if (row.SyncId == Guid.Empty) row.SyncId = Guid.NewGuid();
        if (row.Id == 0) return col.Insert(row).AsInt32;
        col.Update(row);
        return row.Id;
    }

    public void DeleteRow(int id)
    {
        using var db = Open();
        var rows = db.GetCollection<WishlistRow>("rows");
        var row = rows.FindById(id);
        if (row == null) return;
        row.IsDeleted = true;
        row.UpdatedAt = DateTime.UtcNow;
        rows.Update(row);
        // Cells: just update timestamps so sync picks them up
        var cells = db.GetCollection<WishlistCell>("cells");
        var now = DateTime.UtcNow;
        foreach (var c in cells.Find(c => c.RowId == id).ToList())
        { c.UpdatedAt = now; cells.Update(c); }
    }

    public void UpsertRowFromSync(WishlistRow row)
    {
        using var db = Open();
        var col = db.GetCollection<WishlistRow>("rows");
        var existing = col.FindOne(r => r.SyncId == row.SyncId);
        if (existing != null)
        {
            if (row.UpdatedAt <= existing.UpdatedAt) return;
            row.Id = existing.Id;
            col.Update(row);
        }
        else
        {
            row.Id = 0;
            col.Insert(row);
        }
    }

    // ── Cells ─────────────────────────────────────────────────────────────

    public List<WishlistCell> GetCells(int rowId)
    {
        using var db = Open();
        return db.GetCollection<WishlistCell>("cells").Find(c => c.RowId == rowId).ToList();
    }

    public IEnumerable<WishlistCell> GetAllCellsForSync()
    {
        using var db = Open();
        return db.GetCollection<WishlistCell>("cells").FindAll().ToList();
    }

    public WishlistCell? GetCellBySyncId(Guid syncId)
    {
        using var db = Open();
        return db.GetCollection<WishlistCell>("cells").FindOne(c => c.SyncId == syncId);
    }

    public void UpsertCell(WishlistCell cell)
    {
        using var db = Open();
        var col = db.GetCollection<WishlistCell>("cells");
        cell.UpdatedAt = DateTime.UtcNow;
        if (cell.SyncId == Guid.Empty) cell.SyncId = Guid.NewGuid();
        var existing = col.FindOne(c => c.RowId == cell.RowId && c.ColumnId == cell.ColumnId);
        if (existing != null)
        {
            cell.Id = existing.Id;
            cell.SyncId = existing.SyncId; // preserve SyncId
            col.Update(cell);
        }
        else
        {
            col.Insert(cell);
        }
    }

    public void UpsertCellFromSync(WishlistCell cell)
    {
        using var db = Open();
        var col = db.GetCollection<WishlistCell>("cells");
        var existing = col.FindOne(c => c.SyncId == cell.SyncId);
        if (existing != null)
        {
            if (cell.UpdatedAt <= existing.UpdatedAt) return;
            cell.Id = existing.Id;
            col.Update(cell);
        }
        else
        {
            cell.Id = 0;
            col.Insert(cell);
        }
    }

    // ── Saved filters ─────────────────────────────────────────────────────

    public List<WishlistSavedFilter> GetSavedFilters(int wishlistId)
    {
        using var db = Open();
        return db.GetCollection<WishlistSavedFilter>("saved_filters")
                 .Find(f => f.WishlistId == wishlistId)
                 .OrderByDescending(f => f.CreatedAt)
                 .ToList();
    }

    public int SaveFilter(WishlistSavedFilter filter)
    {
        using var db = Open();
        var col = db.GetCollection<WishlistSavedFilter>("saved_filters");
        if (filter.Id == 0) return col.Insert(filter).AsInt32;
        col.Update(filter);
        return filter.Id;
    }

    public void DeleteSavedFilter(int id)
    {
        using var db = Open();
        db.GetCollection<WishlistSavedFilter>("saved_filters").Delete(id);
    }

    // ── Conditional formatting ────────────────────────────────────────────

    public List<WishlistConditionalFormat> GetConditionalFormats(int wishlistId)
    {
        using var db = Open();
        return db.GetCollection<WishlistConditionalFormat>("cond_formats")
                 .Find(f => f.WishlistId == wishlistId)
                 .OrderBy(f => f.Priority)
                 .ToList();
    }

    public int SaveConditionalFormat(WishlistConditionalFormat fmt)
    {
        using var db = Open();
        var col = db.GetCollection<WishlistConditionalFormat>("cond_formats");
        if (fmt.Id == 0) return col.Insert(fmt).AsInt32;
        col.Update(fmt);
        return fmt.Id;
    }

    public void DeleteConditionalFormat(int id)
    {
        using var db = Open();
        db.GetCollection<WishlistConditionalFormat>("cond_formats").Delete(id);
    }

    public void UpdateConditionalFormatPriorities(IEnumerable<WishlistConditionalFormat> formats)
    {
        using var db = Open();
        var col = db.GetCollection<WishlistConditionalFormat>("cond_formats");
        foreach (var fmt in formats) col.Update(fmt);
    }

    // ── Shares ───────────────────────────────────────────────────────────

    public List<WishlistShare> GetShares(int wishlistId)
    {
        using var db = Open();
        return db.GetCollection<WishlistShare>("shares")
                 .Find(s => s.WishlistId == wishlistId && !s.IsDeleted)
                 .ToList();
    }

    public void AddShare(WishlistShare share)
    {
        using var db = Open();
        var col = db.GetCollection<WishlistShare>("shares");
        if (share.SyncId == Guid.Empty) share.SyncId = Guid.NewGuid();
        col.Insert(share);
    }

    public void RemoveShare(int id)
    {
        using var db = Open();
        var col = db.GetCollection<WishlistShare>("shares");
        var s = col.FindById(id);
        if (s == null) return;
        s.IsDeleted = true;
        col.Update(s);
    }

    public IEnumerable<WishlistShare> GetAllSharesForSync()
    {
        using var db = Open();
        return db.GetCollection<WishlistShare>("shares").FindAll().ToList();
    }

    public void UpsertShareFromSync(WishlistShare share)
    {
        using var db = Open();
        var col = db.GetCollection<WishlistShare>("shares");
        var existing = col.FindOne(s => s.SyncId == share.SyncId);
        if (existing != null)
        {
            share.Id = existing.Id;
            col.Update(share);
        }
        else
        {
            share.Id = 0;
            col.Insert(share);
        }
    }
}
