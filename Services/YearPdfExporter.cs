using FocusFlowFinal.Models.YearStats;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;

namespace FocusFlowFinal.Services;

public static class YearPdfExporter
{
    static YearPdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static void Export(Stream output, YearSummaryData data)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader(data.Year));
                page.Content().Element(ComposeContent(data));
                page.Footer().AlignCenter()
                    .Text($"Сгенерировано FocusFlow · {DateTime.Now:dd.MM.yyyy}");
            });
        }).GeneratePdf(output);
    }

    private static Action<IContainer> ComposeHeader(int year) => c =>
        c.PaddingBottom(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
         .Row(row =>
         {
             row.RelativeItem()
                .Text($"FocusFlow — Итоги {year} года")
                .Bold().FontSize(16);
         });

    private static Action<IContainer> ComposeContent(YearSummaryData d) => c =>
        c.Column(col =>
        {
            col.Spacing(14);

            // ── Задачи ────────────────────────────────────────────────
            col.Item().Element(Section("📋 Задачи", new[]
            {
                $"Создано:   {d.Tasks.TotalCreated}",
                $"Выполнено: {d.Tasks.TotalCompleted}  ({Math.Round(d.Tasks.CompletionPct, 1)}%)",
                "Топ проекты: " + string.Join(", ", d.Tasks.Top3Projects.Select(p => $"{p.Name} ({p.Count})"))
            }));

            // ── Фокус ─────────────────────────────────────────────────
            col.Item().Element(Section("⏱ Фокус", new[]
            {
                $"Всего часов:        {d.Focus.TotalHours} ч",
                $"Среднее в день:     {d.Focus.AvgHoursPerDay} ч/д",
                $"Лучший день:        {d.Focus.MostProductiveDay?.ToString("dd.MM.yyyy") ?? "—"} · {d.Focus.MostProductiveDayHours} ч",
                $"Лучший месяц:       {d.Focus.MostProductiveMonth} · {d.Focus.MostProductiveMonthHours} ч",
                "Часы по месяцам:    " + MiniBar(d.Focus.MonthlyHours)
            }));

            // ── Привычки ──────────────────────────────────────────────
            col.Item().Element(Section("🔥 Привычки", new[]
            {
                $"Активных: {d.Habits.ActiveHabits}",
                $"Макс. серия: {d.Habits.LongestStreak} дн. — {d.Habits.LongestStreakHabit}",
                $"Средний %: {d.Habits.AvgCompletionPercent}%",
                "Стабильные: " + string.Join(", ", d.Habits.Top3Stable.Select(h => $"{h.Name} {Math.Round(h.Percent, 1)}%"))
            }));

            // ── Тренировки ────────────────────────────────────────────
            col.Item().Element(Section("🏋 Тренировки", new[]
            {
                $"Всего тренировок: {d.Workouts.TotalSessions}",
                $"Тоннаж: {d.Workouts.TotalTonnageTons} т",
                $"Часов в зале: {d.Workouts.TotalHours} ч",
                $"Любимое упражнение: {d.Workouts.FavoriteExercise}"
            }));

            // ── Настроение ────────────────────────────────────────────
            col.Item().Element(Section("😊 Настроение", new[]
            {
                $"Всего записей: {d.Mood.TotalEntries}",
                $"Самый «зелёный» месяц: {d.Mood.BestMonth} · {d.Mood.BestMonthGoodPct}%",
                "Распределение: " + string.Join("  ", d.Mood.Distribution.OrderBy(kv => kv.Key)
                    .Select(kv => $"L{kv.Key}:{kv.Value}"))
            }));

            // ── Заметки ───────────────────────────────────────────────
            col.Item().Element(Section("📝 Заметки", new[]
            {
                $"Всего записей: {d.Notes.TotalNotes}",
                $"Дней с записями: {d.Notes.DaysWithNotes}",
                "Топ теги: " + string.Join(", ", d.Notes.Top3Tags.Select(t => $"#{t.Tag} ({t.Count})"))
            }));

            // ── Медиа ─────────────────────────────────────────────────
            col.Item().Element(Section("🎬 Медиа", new[]
            {
                $"Фильмов: {d.Media.CompletedMovies}  Сериалов: {d.Media.CompletedSeries}  Аниме: {d.Media.CompletedAnime}  Книг: {d.Media.CompletedBooks}  Манги: {d.Media.CompletedManga}",
                $"Средняя оценка: {d.Media.AvgScore:F1}",
                "Топ: " + string.Join(", ", d.Media.Top3.Select(m => $"{m.Title} ({m.Score:F1})"))
            }));

            // ── События ───────────────────────────────────────────────
            col.Item().Element(Section("📅 События", new[]
            {
                $"Всего событий: {d.Events.TotalEvents}",
                $"Часов в событиях: {d.Events.TotalHours} ч"
            }));
        });

    private static Action<IContainer> Section(string title, string[] lines) => c =>
        c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
        {
            col.Item().Text(title).Bold().FontSize(12);
            col.Item().PaddingTop(4).Column(inner =>
            {
                foreach (var line in lines)
                    inner.Item().Text(line);
            });
        });

    private static string MiniBar(double[] values)
    {
        double max = values.Max();
        if (max <= 0) return "—";
        string[] months = { "Я","Ф","М","А","М","И","И","А","С","О","Н","Д" };
        return string.Join(" ", values.Select((v, i) =>
            $"{months[i]}:{v:F0}"));
    }
}
