using ClosedXML.Excel;
using FocusFlowFinal.Models.Wishlist;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace FocusFlowFinal.Services;

public class WishlistExportService
{
    private readonly IWishlistRepository _repo;

    public WishlistExportService(IWishlistRepository repo) => _repo = repo;

    // ── Export ────────────────────────────────────────────────────────────

    public void ExportCsv(WishlistItem wishlist, string path)
    {
        var (cols, rows, cells) = LoadAll(wishlist.Id);
        using var sw = new StreamWriter(path, false, Encoding.UTF8);

        // Header
        sw.WriteLine(string.Join(",", cols.Select(c => CsvEscape(c.Name))));

        // Rows
        foreach (var row in rows)
        {
            var rowCells = cells.Where(c => c.RowId == row.Id).ToDictionary(c => c.ColumnId);
            var values = cols.Select(col =>
                rowCells.TryGetValue(col.Id, out var cell) ? CsvEscape(cell.Value ?? "") : "");
            sw.WriteLine(string.Join(",", values));
        }
    }

    public void ExportExcel(WishlistItem wishlist, string path)
    {
        var (cols, rows, cells) = LoadAll(wishlist.Id);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(wishlist.Name.Length > 31 ? wishlist.Name[..31] : wishlist.Name);

        // Header row
        for (int c = 0; c < cols.Count; c++)
        {
            ws.Cell(1, c + 1).Value = cols[c].Name;
            ws.Cell(1, c + 1).Style.Font.Bold = true;
        }

        // Data rows
        for (int r = 0; r < rows.Count; r++)
        {
            var rowCells = cells.Where(c => c.RowId == rows[r].Id).ToDictionary(c => c.ColumnId);
            for (int c = 0; c < cols.Count; c++)
            {
                if (rowCells.TryGetValue(cols[c].Id, out var cell))
                    ws.Cell(r + 2, c + 1).Value = cell.Value ?? string.Empty;
            }
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(path);
    }

    public void ExportJson(WishlistItem wishlist, string path)
    {
        var (cols, rows, cells) = LoadAll(wishlist.Id);
        var data = new
        {
            name = wishlist.Name,
            description = wishlist.Description,
            columns = cols.Select(c => new { c.Name, type = c.Type.ToString(), options = c.OptionsJson }),
            rows = rows.Select(row =>
            {
                var rowCells = cells.Where(c => c.RowId == row.Id).ToDictionary(c => c.ColumnId);
                return cols.ToDictionary(
                    col => col.Name,
                    col => (object?)(rowCells.TryGetValue(col.Id, out var cell) ? cell.Value : null));
            })
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json, Encoding.UTF8);
    }

    // ── Import ────────────────────────────────────────────────────────────

    public (int rowsImported, string? error) ImportCsv(WishlistItem wishlist, string path)
    {
        try
        {
            var cols = _repo.GetColumns(wishlist.Id);
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length < 2) return (0, "Файл пустой или не содержит данных");

            var headers = ParseCsvLine(lines[0]);
            int nextOrder = _repo.GetRows(wishlist.Id).Count;
            int imported = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var values = ParseCsvLine(lines[i]);

                var row = new WishlistRow { WishlistId = wishlist.Id, Order = nextOrder++ };
                int rowId = _repo.UpsertRow(row);

                for (int c = 0; c < headers.Length && c < values.Length; c++)
                {
                    var col = cols.FirstOrDefault(col => col.Name.Equals(headers[c], StringComparison.OrdinalIgnoreCase));
                    if (col == null) continue;
                    _repo.UpsertCell(new WishlistCell
                    {
                        RowId = rowId, ColumnId = col.Id, Value = values[c]
                    });
                }
                imported++;
            }
            return (imported, null);
        }
        catch (Exception ex)
        {
            return (0, ex.Message);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private (List<WishlistColumn> cols, List<WishlistRow> rows, List<WishlistCell> cells) LoadAll(int wishlistId)
    {
        var cols = _repo.GetColumns(wishlistId);
        var rows = _repo.GetRows(wishlistId);
        var cells = rows.SelectMany(r => _repo.GetCells(r.Id)).ToList();
        return (cols, rows, cells);
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }
}
