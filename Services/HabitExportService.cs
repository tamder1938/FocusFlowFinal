using ClosedXML.Excel;
using FocusFlowFinal.Models.Habits;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace FocusFlowFinal.Services;

public class HabitExportService
{
    private readonly IDatabaseService _db;

    public HabitExportService(IDatabaseService db) => _db = db;

    public string ExportToCsv()
    {
        var path   = GetPath("csv");
        var habits = _db.GetAllHabits().Where(h => !h.IsArchived).ToList();

        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("Привычка,Категория,Дата,Статус");

        foreach (var h in habits)
        {
            foreach (var c in _db.GetHabitCompletions(h.Id).OrderBy(c => c.Date))
            {
                var status = c.Status == 2 ? "Выполнено" : "Частично";
                writer.WriteLine($"\"{Escape(h.Name)}\",\"{Escape(h.Category)}\",{c.Date:yyyy-MM-dd},{status}");
            }
        }

        return path;
    }

    public string ExportToExcel()
    {
        var path   = GetPath("xlsx");
        var habits = _db.GetAllHabits().Where(h => !h.IsArchived).ToList();

        using var wb = new XLWorkbook();

        // ── Лист 1: Общая статистика ──────────────────────────────────
        var wsStat = wb.Worksheets.Add("Статистика");
        wsStat.Cell(1, 1).Value = "Привычка";
        wsStat.Cell(1, 2).Value = "Категория";
        wsStat.Cell(1, 3).Value = "Повторение";
        wsStat.Cell(1, 4).Value = "Текущая серия";
        wsStat.Cell(1, 5).Value = "Лучшая серия";
        wsStat.Cell(1, 6).Value = "Всего выполнений";
        wsStat.Row(1).Style.Font.Bold = true;
        wsStat.Row(1).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

        int statRow = 2;
        foreach (var h in habits)
        {
            wsStat.Cell(statRow, 1).Value = h.Name;
            wsStat.Cell(statRow, 2).Value = h.Category;
            wsStat.Cell(statRow, 3).Value = RepeatText(h);
            wsStat.Cell(statRow, 4).Value = h.CurrentStreak;
            wsStat.Cell(statRow, 5).Value = h.BestStreak;
            wsStat.Cell(statRow, 6).Value = h.TotalCompletions;
            statRow++;
        }
        wsStat.Columns().AdjustToContents();

        // ── Лист 2: Ежедневное выполнение ────────────────────────────
        var wsLog = wb.Worksheets.Add("Выполнение");
        wsLog.Cell(1, 1).Value = "Привычка";
        wsLog.Cell(1, 2).Value = "Дата";
        wsLog.Cell(1, 3).Value = "Статус";
        wsLog.Row(1).Style.Font.Bold = true;
        wsLog.Row(1).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

        int logRow = 2;
        foreach (var h in habits)
        {
            foreach (var c in _db.GetHabitCompletions(h.Id).OrderBy(c => c.Date))
            {
                wsLog.Cell(logRow, 1).Value = h.Name;
                wsLog.Cell(logRow, 2).Value = c.Date.ToString("yyyy-MM-dd");
                wsLog.Cell(logRow, 3).Value = c.Status == 2 ? "Выполнено" : "Частично";

                if (c.Status == 2)
                    wsLog.Cell(logRow, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#D1FAE5");
                else
                    wsLog.Cell(logRow, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");

                logRow++;
            }
        }
        wsLog.Columns().AdjustToContents();

        // ── Лист 3: HeatMap (последние 90 дней) ──────────────────────
        var wsHeat = wb.Worksheets.Add("Активность 90 дней");
        var startDate = DateTime.Today.AddDays(-89);

        wsHeat.Cell(1, 1).Value = "Привычка";
        int col = 2;
        for (var d = startDate; d <= DateTime.Today; d = d.AddDays(1))
        {
            wsHeat.Cell(1, col).Value = d.ToString("dd.MM");
            col++;
        }
        wsHeat.Row(1).Style.Font.Bold = true;

        int heatRow = 2;
        foreach (var h in habits)
        {
            wsHeat.Cell(heatRow, 1).Value = h.Name;
            var comps = _db.GetCompletionsForPeriod(startDate, DateTime.Today.AddDays(1))
                           .Where(c => c.HabitId == h.Id)
                           .ToDictionary(c => c.Date.Date, c => c.Status);
            int heatCol = 2;
            for (var d = startDate; d <= DateTime.Today; d = d.AddDays(1))
            {
                if (comps.TryGetValue(d, out int status))
                {
                    wsHeat.Cell(heatRow, heatCol).Value = status == 2 ? "✓" : "~";
                    wsHeat.Cell(heatRow, heatCol).Style.Fill.BackgroundColor =
                        status == 2 ? XLColor.FromHtml("#D1FAE5") : XLColor.FromHtml("#FEF3C7");
                }
                heatCol++;
            }
            heatRow++;
        }
        wsHeat.Column(1).AdjustToContents();

        wb.SaveAs(path);
        return path;
    }

    private static string RepeatText(Habit h) => h.RepetitionType switch
    {
        HabitRepetitionType.Daily         => "Ежедневно",
        HabitRepetitionType.WeekDays      => "По дням недели",
        HabitRepetitionType.TimesPerWeek  => $"{h.TimesPerWeek}× в неделю",
        HabitRepetitionType.TimesPerMonth => $"{h.TimesPerMonth}× в месяц",
        _                                 => ""
    };

    private static string Escape(string s) => s.Replace("\"", "\"\"");

    private static string GetPath(string ext)
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(docs, $"FocusFlow_Habits_{DateTime.Today:yyyyMMdd}.{ext}");
    }
}
