using FocusFlowFinal.Models.Wishlist;
using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public interface IWishlistRepository
{
    // ── Core CRUD ─────────────────────────────────────────────────────────
    IEnumerable<WishlistItem> GetAll();
    WishlistItem? GetById(int id);
    int Upsert(WishlistItem wishlist);
    void Delete(int id);

    List<WishlistColumn> GetColumns(int wishlistId);
    void SaveColumns(int wishlistId, IEnumerable<WishlistColumn> columns);

    List<WishlistRow> GetRows(int wishlistId);
    int UpsertRow(WishlistRow row);
    void DeleteRow(int id);

    List<WishlistCell> GetCells(int rowId);
    void UpsertCell(WishlistCell cell);

    List<WishlistSavedFilter> GetSavedFilters(int wishlistId);
    int SaveFilter(WishlistSavedFilter filter);
    void DeleteSavedFilter(int id);

    List<WishlistConditionalFormat> GetConditionalFormats(int wishlistId);
    int SaveConditionalFormat(WishlistConditionalFormat fmt);
    void DeleteConditionalFormat(int id);
    void UpdateConditionalFormatPriorities(IEnumerable<WishlistConditionalFormat> formats);

    // ── Sync support ─────────────────────────────────────────────────────
    IEnumerable<WishlistItem> GetAllForSync();
    IEnumerable<WishlistColumn> GetColumnsForSync(int wishlistId);
    IEnumerable<WishlistRow> GetRowsForSync(int wishlistId);
    IEnumerable<WishlistCell> GetAllCellsForSync();

    WishlistItem? GetBySyncId(Guid syncId);
    WishlistColumn? GetColumnBySyncId(Guid syncId);
    WishlistRow? GetRowBySyncId(Guid syncId);
    WishlistCell? GetCellBySyncId(Guid syncId);

    void UpsertFromSync(WishlistItem wishlist);
    void UpsertColumnFromSync(WishlistColumn col);
    void UpsertRowFromSync(WishlistRow row);
    void UpsertCellFromSync(WishlistCell cell);

    // ── Shares ───────────────────────────────────────────────────────────
    List<WishlistShare> GetShares(int wishlistId);
    void AddShare(WishlistShare share);
    void RemoveShare(int id);
    IEnumerable<WishlistShare> GetAllSharesForSync();
    void UpsertShareFromSync(WishlistShare share);
}
