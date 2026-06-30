using FocusFlowFinal.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

public class PlaceExportService
{
    public async Task<string> ExportToCsvAsync(IEnumerable<PlaceItem> places)
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            $"FocusFlow_Places_{DateTime.Now:yyyyMMdd_HHmm}.csv");

        var sb = new StringBuilder();
        sb.AppendLine("Название,Категория,Статус,Адрес,Оценка,Дата посещения,Заметки");

        foreach (var p in places)
        {
            sb.AppendLine(string.Join(",",
                CsvCell(p.Name),
                CsvCell(p.Category),
                CsvCell(p.StatusLabel),
                CsvCell(p.Address),
                p.Rating == 0 ? "" : p.Rating.ToString(),
                p.VisitedAt.HasValue ? p.VisitedAt.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : "",
                CsvCell(p.Notes)));
        }

        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    private static string CsvCell(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
