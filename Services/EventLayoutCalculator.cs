using System;
using System.Collections.Generic;
using System.Linq;

namespace FocusFlowFinal.Services;

/// <summary>
/// Элемент для расчёта раскладки событий — содержит временной интервал
/// и вычисляемые после раскладки координаты (Left/Width).
/// </summary>
public class EventLayoutItem
{
    /// <summary>Произвольный идентификатор/ссылка, которую вызывающий код
    /// использует для сопоставления результата с исходным объектом.</summary>
    public object? Tag { get; set; }

    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    // Результат раскладки (заполняется CalculateLayout)
    public double Left { get; set; }
    public double Width { get; set; }
}

/// <summary>
/// ИСПРАВЛЕНО (Часть 1, п.1): реализует "кирпичную" укладку событий
/// в стиле Google Calendar — пересекающиеся по времени события
/// размещаются рядом друг с другом равной шириной, а не наслаиваются.
/// </summary>
public static class EventLayoutCalculator
{
    /// <summary>
    /// Рассчитывает Left/Width для каждого события так, чтобы пересекающиеся
    /// по времени события не перекрывали друг друга, а делили доступную
    /// ширину поровну между собой.
    /// </summary>
    /// <param name="events">Список событий (Start/End должны быть заполнены).</param>
    /// <param name="totalWidth">Общая доступная ширина области (px).</param>
    /// <param name="leftMargin">Отступ слева от начала области (px).</param>
    /// <param name="gap">Зазор между соседними колонками (px).</param>
    public static void CalculateLayout(
        List<EventLayoutItem> events,
        double totalWidth,
        double leftMargin = 4,
        double gap = 4)
    {
        if (events.Count == 0) return;

        // Защита от событий с нулевой/отрицательной длительностью —
        // иначе сортировка/группировка по кластерам сломается.
        foreach (var ev in events)
        {
            if (ev.End <= ev.Start)
                ev.End = ev.Start.AddMinutes(30);
        }

        var sorted = events.OrderBy(e => e.Start).ThenBy(e => e.End).ToList();

        // ── Шаг 1: группируем события в кластеры взаимно пересекающихся ──
        var clusters = new List<List<EventLayoutItem>>();
        List<EventLayoutItem>? current = null;
        DateTime clusterEnd = DateTime.MinValue;

        foreach (var ev in sorted)
        {
            if (current == null || ev.Start >= clusterEnd)
            {
                current = new List<EventLayoutItem>();
                clusters.Add(current);
                clusterEnd = ev.End;
            }
            else if (ev.End > clusterEnd)
            {
                clusterEnd = ev.End;
            }

            current.Add(ev);
        }

        // ── Шаг 2: внутри каждого кластера — жадное распределение по колонкам ──
        double available = Math.Max(totalWidth - leftMargin, 40);

        foreach (var cluster in clusters)
        {
            var columns = new List<List<EventLayoutItem>>();

            foreach (var ev in cluster)
            {
                bool placed = false;

                for (int c = 0; c < columns.Count; c++)
                {
                    // Событие можно поместить в колонку c, если оно начинается
                    // не раньше окончания последнего события в этой колонке.
                    var lastInColumn = columns[c][^1];
                    if (ev.Start >= lastInColumn.End)
                    {
                        columns[c].Add(ev);
                        ev.Left = c >= 0 ? c : 0; // временно храним индекс колонки
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    columns.Add(new List<EventLayoutItem> { ev });
                    ev.Left = columns.Count - 1;
                }
            }

            int totalColumns = columns.Count;
            double colWidth = (available - gap * (totalColumns - 1)) / totalColumns;
            if (colWidth < 24) colWidth = 24; // минимальная читаемая ширина

            foreach (var column in columns)
            {
                foreach (var ev in column)
                {
                    int columnIndex = (int)ev.Left; // индекс был временно записан выше
                    ev.Left  = leftMargin + columnIndex * (colWidth + gap);
                    ev.Width = colWidth;
                }
            }
        }
    }
}
