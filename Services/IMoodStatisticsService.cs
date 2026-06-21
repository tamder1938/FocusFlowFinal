using FocusFlowFinal.Models.Mood;
using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public interface IMoodStatisticsService
{
    double GetAverageMood(IEnumerable<MoodEntry> entries);
    List<MoodDistributionItem> GetDistribution(IEnumerable<MoodEntry> entries);
    List<MoodYearDotItem> GetYearGrid(int year, IEnumerable<MoodEntry> entries);
}

public class MoodDistributionItem
{
    public int    Level      { get; init; }
    public string LevelName  { get; init; } = string.Empty;
    public string Color      { get; init; } = string.Empty;
    public int    Count      { get; init; }
    public double Percentage { get; init; }
}

public class MoodYearDotItem
{
    public System.DateTime? Date  { get; init; }
    public int              Level { get; init; }   // 0 = нет записи
    public string Color =>
        Level switch { 1 => "#DC4F4F", 2 => "#E08A3C", 3 => "#8AB7D9", 4 => "#A8D88A", 5 => "#5FB87A", _ => "#E5E7EB" };
    public string Tooltip =>
        Date == null ? string.Empty :
        Level == 0   ? Date.Value.ToString("d MMM") :
                       $"{Date.Value:d MMM}: {MoodStatisticsService.LevelName(Level)}";
}
