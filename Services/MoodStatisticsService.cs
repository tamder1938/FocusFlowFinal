using FocusFlowFinal.Models.Mood;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FocusFlowFinal.Services;

public class MoodStatisticsService : IMoodStatisticsService
{
    private static readonly string[] Colors = ["#DC4F4F", "#E08A3C", "#8AB7D9", "#A8D88A", "#5FB87A"];
    private static readonly string[] Names  = ["ужасно",  "плохо",   "так себе", "хорошо",  "супер"];

    public static string LevelName(int level) =>
        level is >= 1 and <= 5 ? Names[level - 1] : "—";

    public double GetAverageMood(IEnumerable<MoodEntry> entries)
    {
        var list = entries.ToList();
        return list.Count == 0 ? 0 : list.Average(e => e.Level);
    }

    public List<MoodDistributionItem> GetDistribution(IEnumerable<MoodEntry> entries)
    {
        var list  = entries.ToList();
        int total = list.Count;
        return Enumerable.Range(1, 5).Select(lvl =>
        {
            int cnt = list.Count(e => e.Level == lvl);
            return new MoodDistributionItem
            {
                Level      = lvl,
                LevelName  = Names[lvl - 1],
                Color      = Colors[lvl - 1],
                Count      = cnt,
                Percentage = total == 0 ? 0 : Math.Round((double)cnt / total * 100, 1)
            };
        }).ToList();
    }

    public List<MoodYearDotItem> GetYearGrid(int year, IEnumerable<MoodEntry> entries)
    {
        var byDate = entries
            .Where(e => e.Date.Year == year)
            .GroupBy(e => e.Date.Date)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Date).First().Level);

        var result = new List<MoodYearDotItem>(31 * 12);
        for (int day = 1; day <= 31; day++)
        {
            for (int month = 1; month <= 12; month++)
            {
                DateTime? date = null;
                int level = 0;
                if (day <= DateTime.DaysInMonth(year, month))
                {
                    date  = new DateTime(year, month, day);
                    byDate.TryGetValue(date.Value, out level);
                }
                result.Add(new MoodYearDotItem { Date = date, Level = level });
            }
        }
        return result;
    }
}
